using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Model.Quest
{
    public enum QuestType
    {
        Adventure,
        Obtain,
        Craft,
        Exchange
    }

    [Serializable]
    public abstract class Quest : IState
    {
        [NonSerialized]
        public bool isReceivable = false;

        protected int _current;

        public abstract QuestType QuestType { get; }

        private static readonly Dictionary<string, Func<Dictionary, Quest>> Deserializers =
            new Dictionary<string, Func<Dictionary, Quest>>
            {
                ["collectQuest"] = d => new CollectQuest(d),
                ["combinationQuest"] = d => new CombinationQuest(d),
                ["monsterQuest"] = d => new MonsterQuest(d),
                ["tradeQuest"] = d => new TradeQuest(d),
                ["worldQuest"] = d => new WorldQuest(d),
                ["itemEnhancementQuest"] = d => new ItemEnhancementQuest(d),
                ["generalQuest"] = d => new GeneralQuest(d),
                ["itemGradeQuest"] = d => new ItemGradeQuest(d),
                ["itemTypeCollectQuest"] = d => new ItemTypeCollectQuest(d),
                ["GoldQuest"] = d => new GoldQuest(d),
                ["combinationEquipmentQuest"] = d => new CombinationEquipmentQuest(d),
            };

        public bool Complete { get; protected set; }

        public int Goal { get; set; }

        public int Id { get; }

        public QuestReward Reward { get; }

        /// <summary>
        /// 이미 퀘스트 보상이 액션에서 지급되었는가?
        /// </summary>
        public bool IsPaidInAction { get; set; }

        public virtual float Progress => (float) _current / Goal;

        public const string GoalFormat = "({0}/{1})";

        protected Quest(QuestSheet.Row data, QuestReward reward)
        {
            Id = data.Id;
            Goal = data.Goal;
            Reward = reward;
        }

        public abstract void Check();
        protected abstract string TypeId { get; }

        protected Quest(Dictionary serialized)
        {
            Complete = ((Bencodex.Types.Boolean) serialized["complete"]).Value;
            Goal = (int) ((Integer) serialized["goal"]).Value;
            _current = (int) ((Integer) serialized["current"]).Value;
            Id = (int) ((Integer) serialized["id"]).Value;
            Reward = new QuestReward((Dictionary) serialized["reward"]);
            IsPaidInAction = serialized["isPaidInAction"].ToNullableBoolean() ?? false;
        }

        public abstract string GetProgressText();

        public virtual IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "typeId"] = (Text) TypeId,
                [(Text) "complete"] = new Bencodex.Types.Boolean(Complete),
                [(Text) "goal"] = (Integer) Goal,
                [(Text) "current"] = (Integer) _current,
                [(Text) "id"] = (Integer) Id,
                [(Text) "reward"] = Reward.Serialize(),
                [(Text) "isPaidInAction"] = new Bencodex.Types.Boolean(IsPaidInAction),
            });

        public static Quest Deserialize(Dictionary serialized)
        {
            string typeId = ((Text) serialized["typeId"]).Value;
            Func<Dictionary, Quest> deserializer;
            try
            {
                deserializer = Deserializers[typeId];
            }
            catch (KeyNotFoundException)
            {
                string typeIds = string.Join(
                    ", ",
                    Deserializers.Keys.OrderBy(k => k, StringComparer.InvariantCulture)
                );
                throw new ArgumentException(
                    $"Unregistered typeId: {typeId}; available typeIds: {typeIds}"
                );
            }

            try
            {
                return deserializer(serialized);
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "{TypeFullName} was raised during deserialize: {Serialized}",
                    e.GetType().FullName,
                    serialized);
                throw;
            }
        }

        public static Quest Deserialize(IValue arg)
        {
            return Deserialize((Dictionary) arg);
        }
    }
}
