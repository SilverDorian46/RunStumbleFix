using System;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.RunStumbleFix;

public sealed class RunStumbleFixModule : EverestModule
{
    public static RunStumbleFixModule Instance { get; private set; } = default!;

    private static ILHook? hook_Player_UpdateSprite;
    private static ILHook? hook_Player_OnCollideV;

    public RunStumbleFixModule()
    {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(RunStumbleFixModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(RunStumbleFixModule), LogLevel.Info);
#endif
    }

    public override void Load()
    {
        SpeedrunToolInterop.Load();

        Everest.Events.Player.OnRegisterStates += Player_OnRegisterStates;
        Everest.Events.Player.OnAfterUpdate += Player_OnAfterUpdate;

        hook_Player_UpdateSprite = new(
                typeof(Player).GetOrigMethod(nameof(Player.UpdateSprite), HookHelper.PrivateInstance)!,
                IL_Player_UpdateSprite
            );
        hook_Player_OnCollideV = new(
                typeof(Player).GetOrigMethod(nameof(Player.OnCollideV), HookHelper.PrivateInstance)!,
                IL_Player_OnCollideV
            );
    }

    public override void Unload()
    {
        SpeedrunToolInterop.Unload();

        Everest.Events.Player.OnRegisterStates -= Player_OnRegisterStates;
        Everest.Events.Player.OnAfterUpdate -= Player_OnAfterUpdate;

        HookHelper.DisposeAndSetNull(ref hook_Player_UpdateSprite);
        HookHelper.DisposeAndSetNull(ref hook_Player_OnCollideV);
    }

    // this is called on Player constructor
    private static void Player_OnRegisterStates(Player player)
        => PlayerFields.CreateFor(player);

    private static void Player_OnAfterUpdate(Player player)
    {
        var fields = PlayerFields.GetOrCreateFor(player);
        fields.justLanded = false;
        fields.prevHighestAirY = player.highestAirY;
    }

    private static void IL_Player_UpdateSprite(ILContext il)
    {
        Logger.Info(nameof(RunStumbleFixModule),
            "Patching IL for Player.(orig_)UpdateSprite to add a check " +
            "for whether the player has just landed");

        ILCursor cur = new(il);

        /*
                           || CheckJustLanded(this)
         [...]             vvvvvvvvvvvvvvvvvvvvvvvv
         else if (onGround                         )
         {
             [...]
             else if (!Sprite.Running || [...]) { [...] }
         }
         */
        cur.GotoNext(instr => instr.MatchCallvirt<PlayerSprite>($"get_{nameof(PlayerSprite.Running)}"));
        cur.GotoPrev(instr => instr.MatchLdfld<Player>(nameof(Player.onGround)));
        cur.GotoNext(MoveType.Before, instr => instr.MatchBrfalse(out _));

        ILLabel onGroundLabel = cur.DefineLabel();

        var clone = cur.Clone(); // kage bunshin
        clone.Index++;
        clone.MoveAfterLabels();
        clone.MarkLabel(onGroundLabel);

        cur.EmitBrtrue(onGroundLabel); // brtrue <=> brfalse
        cur.EmitLdarg0();
        cur.EmitDelegate(CheckJustLanded);

        static bool CheckJustLanded(Player player)
            => PlayerFields.GetFor(player)?.justLanded ?? false;
    }

    private static void IL_Player_OnCollideV(ILContext il)
    {
        Logger.Info(nameof(RunStumbleFixModule),
            "Patching IL for Player.(orig_)OnCollideV to set a new field on landing");

        VariableDefinition v_fields = new(il.Import(typeof(PlayerFields)));
        il.Body.Variables.Add(v_fields);

        ILCursor cur = new(il);

        /*
         if (Speed.Y > 0) // if the player is landing...
         {
             [...]
             if (StateMachine.State != 1) // ... and not in StClimb
             {
                  <-- var fields = SetJustLanded(this);
                 [...]
                     MinPrevHighestAirY(           , fields)
                     vvvvvvvvvvvvvvvvvvv           vvvvvvvvv
                 if (                   highestAirY          < Y - 50f
                     && Speed.Y >= 160f && Math.Abs(Speed.X) >= 90f)
                     Sprite.Play("runStumble");
                 [...]
             }
         }
         */
        cur.GotoNext(instr => instr.MatchLdstr("runStumble"));
        cur.GotoPrev(instr => instr.MatchCallvirt<StateMachine>($"get_{nameof(StateMachine.State)}"));
        cur.GotoNext(MoveType.After, instr => instr.MatchBeq(out _));
        cur.MoveAfterLabels();

        cur.EmitLdarg0();
        cur.EmitDelegate(SetJustLanded);
        cur.EmitStloc(v_fields);

        cur.GotoNext(MoveType.After, instr => instr.MatchLdfld<Player>(nameof(Player.highestAirY)));

        cur.EmitLdloc(v_fields);
        cur.EmitDelegate(MinPrevHighestAirY);

        static PlayerFields SetJustLanded(Player player)
        {
            var fields = PlayerFields.GetOrCreateFor(player);
            fields.justLanded = true;
            return fields;
        }

        static float MinPrevHighestAirY(float orig, PlayerFields fields)
            => Math.Min(orig, fields.prevHighestAirY);
    }
}