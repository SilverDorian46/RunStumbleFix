using System.Runtime.CompilerServices;

namespace Celeste.Mod.RunStumbleFix;

public sealed class PlayerFields
{
    private static readonly ConditionalWeakTable<Player, PlayerFields> players = [];

    public static PlayerFields CreateFor(Player player)
    {
        PlayerFields fields;
        players.AddOrUpdate(player, fields = new());
        return fields;
    }

    public static PlayerFields GetOrCreateFor(Player player)
    {
        if (!players.TryGetValue(player, out PlayerFields? fields))
            players.Add(player, fields = new());
        return fields;
    }

    public static PlayerFields? GetFor(Player player)
    {
        players.TryGetValue(player, out PlayerFields? fields);
        return fields;
    }

    internal bool justLanded;
    internal float prevHighestAirY;

    private PlayerFields() { }
}
