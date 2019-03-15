using System;
using Nekoyume.Action;
using Nekoyume.Data.Table;

namespace Nekoyume.Game
{
    [Serializable]
    public class Elemental
    {
        public Data.Table.Elemental Data { get; }
        public Elemental(Data.Table.Elemental data)
        {
            Data = data;
        }

        public static Elemental Create(Data.Table.Elemental.ElementalType type)
        {
            var table = ActionManager.Instance.tables.Elemental;
            Data.Table.Elemental data;
            if (table.TryGetValue((int)type, out data))
            {
                return new Elemental(data);
            }
            throw new InvalidElementalException();
        }

        private class InvalidElementalException : Exception
        {}

        public int Calculate(int i, Elemental t)
        {
            var multiplier = 1.0f;
            if (Data.strong == t.Data.id)
            {
                multiplier += Data.multiply;
            }
            else if (Data.weak == t.Data.id)
            {
                multiplier -= Data.multiply;
            }

            return Convert.ToInt32(i * multiplier);
        }
    }
}
