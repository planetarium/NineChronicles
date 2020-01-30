using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.Model.State;
using Nekoyume.State;

namespace Nekoyume.TableData
{
    [Serializable]
    public class QuestSheet : Sheet<int, QuestSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>, IState
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public int Goal { get; private set; }
            public int QuestRewardId { get; private set; }
            
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                Goal = int.Parse(fields[1]);
                QuestRewardId = int.Parse(fields[2]);
            }

            public IValue Serialize() =>
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "Id"] = (Integer) Key,
                    [(Text) "Goal"] = (Integer) Goal,
                    [(Text) "QuestRewardId"] = (Integer) QuestRewardId,
                });

            public static Row Deserialize(Bencodex.Types.Dictionary serialized)
            {
                return new Row
                {
                    Id = (int) ((Integer)serialized["Id"]),
                    Goal = (int) ((Integer)serialized["Goal"]),
                    QuestRewardId = (int) ((Integer)serialized["QuestRewardId"]),
                };
            }
        }
        
        public QuestSheet() : base(nameof(QuestSheet))
        {
        }
    }
}
