using System;
using Nekoyume.Game;

namespace Nekoyume.Data.Table
{
    [Serializable]
    public class Elemental : Row
    {
        // 0 : Normal
        // 1 : Fire
        // 2 : Land
        // 3 : Water
        // 4 : Wind
        public enum ElementalType
        {
            Normal,
            Fire,
            Land,
            Water,
            Wind,
        }

        public ElementalType id;
        public ElementalType strong;
        public ElementalType weak;
        public float multiply = 0.0f;
    }
}
