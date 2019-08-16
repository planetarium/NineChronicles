using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;

namespace Nekoyume.TableData
{
    [Serializable]
    public class StageSheet : Sheet<int, StageSheet.Row>
    {
        [Serializable]
        public struct MonsterData
        {
            public int CharacterId { get; }
            public int Level { get; }
            public string Visual { get; }
            public int Count { get; }

            public MonsterData(int characterId, int level, string visual, int count)
            {
                CharacterId = characterId;
                Level = level;
                Visual = visual;
                Count = count;
            }
        }

        [Serializable]
        public struct Row : ISheetRow<int>
        {
            public int Id { get; private set; }
            public int Stage { get; private set; }
            public int Wave { get; private set; }
            public List<MonsterData> Monsters { get; private set; }
            public bool IsBoss { get; private set; }
            public int Reward { get; private set; }
            public long Exp { get; private set; }

            public int Key => Id;
            
            public void Set(string[] fields)
            {
                Id = int.TryParse(fields[0], out var id) ? id : 0;
                Stage = int.TryParse(fields[1], out var stage) ? stage : 0;
                Wave = int.TryParse(fields[2], out var wave) ? wave : 0;
                Monsters = new List<MonsterData>();
                for (var i = 0; i < 4; i++)
                {
                    var offset = i * 4;
                    Monsters.Add(new MonsterData(
                        int.TryParse(fields[3 + offset], out var characterId) ? characterId : 0,
                        int.TryParse(fields[4 + offset], out var level) ? level : 0,
                        fields[5 + offset],
                        int.TryParse(fields[6 + offset], out var count) ? count : 0
                        ));
                }
                
                IsBoss = bool.TryParse(fields[19], out var isBoss) && isBoss;
                Reward = int.TryParse(fields[20], out var reward) ? reward : 0;
                Exp = int.TryParse(fields[21], out var exp) ? exp : 0;
            }
        }
    }
}
