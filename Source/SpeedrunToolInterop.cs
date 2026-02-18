//#pragma warning disable IDE0079 // Remove unnecessary suppression
//#pragma warning disable CA2211 // Non-constant fields should not be visible
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

using System;
using MonoMod.ModInterop;

namespace Celeste.Mod.RunStumbleFix;

public static class SpeedrunToolInterop
{
    [ModImportName("SpeedrunTool.SaveLoad")]
    private static class Imports
    {
        public delegate object RegisterStaticTypesFunc(Type type, params string[] memberNames);
        public delegate void UnregisterFunc(object obj);

        public static RegisterStaticTypesFunc? RegisterStaticTypes;

        public static UnregisterFunc? Unregister;
    }

    private static object? saveLoadAction;

    internal static void Load()
    {
        typeof(Imports).ModInterop();

        saveLoadAction = Imports.RegisterStaticTypes?.Invoke(typeof(PlayerFields), "players");
    }

    internal static void Unload()
    {
        if (saveLoadAction is not null)
            Imports.Unregister?.Invoke(saveLoadAction);
    }
}
