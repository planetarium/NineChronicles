using System;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.Data.Table
{
    [Serializable]
    public class SetEffect : Row
    {
        public int id = 0;
        public int setId = 0;
        public int setCount = 0;
        public string ability = "";
        public float value = 0f;
        
        public IStatMap ToSetEffectMap()
        {
            return new StatMap(ability, value);
        }
    }
}
