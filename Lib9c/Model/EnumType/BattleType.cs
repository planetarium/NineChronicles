using System;

namespace Nekoyume.Model.EnumType
{
    public enum BattleType
    {
        Adventure = 1,
        Arena = 2,
        Raid = 3,
        End = 4,
    }

    public static class BattleTypeExtensions
    {
         public static bool IsEquippableRune(this BattleType battleType, RuneUsePlace runePlace)
        {
            switch (battleType)
            {
                case BattleType.Adventure:
                    switch (runePlace)
                    {
                        case RuneUsePlace.Adventure:
                        case RuneUsePlace.AdventureAndArena:
                        case RuneUsePlace.RaidAndAdventure:
                        case RuneUsePlace.All:
                            return true;
                        case RuneUsePlace.Arena:
                        case RuneUsePlace.Raid:
                        case RuneUsePlace.RaidAndArena:
                            return false;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(runePlace), runePlace, null);
                    }
                case BattleType.Arena:
                    switch (runePlace)
                    {
                        case RuneUsePlace.Arena:
                        case RuneUsePlace.AdventureAndArena:
                        case RuneUsePlace.RaidAndArena:
                        case RuneUsePlace.All:
                            return true;
                        case RuneUsePlace.Adventure:
                        case RuneUsePlace.Raid:
                        case RuneUsePlace.RaidAndAdventure:
                            return false;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(runePlace), runePlace, null);
                    }
                case BattleType.Raid:
                    switch (runePlace)
                    {
                        case RuneUsePlace.RaidAndAdventure:
                        case RuneUsePlace.Raid:
                        case RuneUsePlace.RaidAndArena:
                        case RuneUsePlace.All:
                            return true;
                        case RuneUsePlace.Arena:
                        case RuneUsePlace.Adventure:
                        case RuneUsePlace.AdventureAndArena:
                            return false;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(runePlace), runePlace, null);
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
