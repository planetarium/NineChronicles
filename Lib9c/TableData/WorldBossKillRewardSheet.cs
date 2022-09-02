using System;
using System.Collections.Generic;
using Libplanet.Action;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class WorldBossKillRewardSheet : Sheet<int, WorldBossKillRewardSheet.Row>, IWorldBossRewardSheet
    {
        public class Row : SheetRow<int>, IWorldBossRewardRow
        {
            private int _rune;
            private bool _initializedRune;
            public int Id;
            public int BossId { get; private set; }
            public int Rank { get; private set; }
            public int RuneMin;
            public int RuneMax;
            public int Crystal { get; private set; }
            public int Rune {
                get
                {
                    if (!_initializedRune)
                    {
                        throw new Exception();
                    }

                    return _rune;
                }
                private set => _rune = value;
            }
            public override int Key => Id;
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                BossId = ParseInt(fields[1]);
                Rank = ParseInt(fields[2]);
                RuneMin = ParseInt(fields[3]);
                RuneMax = ParseInt(fields[4]);
                Crystal = ParseInt(fields[5]);
            }

            public void SetRune(IRandom random)
            {
                _initializedRune = true;
                Rune = random.Next(RuneMin, RuneMax + 1);
            }
        }

        public WorldBossKillRewardSheet() : base(nameof(WorldBossKillRewardSheet))
        {
        }

        public IReadOnlyList<IWorldBossRewardRow> OrderedRows => OrderedList;
    }
}
