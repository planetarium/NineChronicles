using System;

namespace Nekoyume.Model
{
    [Serializable]
    public class Avatar
    {
        public string Name;
        public int Level;
        public long EXP;
        public int HPMax;
        public int CurrentHP;
        public string Items;
        public int WorldStage;
        public bool Dead = false;

        public void Update(Player player)
        {
            Level = player.level;
            EXP = player.exp;
            HPMax = player.hpMax;
            CurrentHP = player.hp;
            Items = player.items;
            WorldStage = player.stage;
            Dead = player.isDead;
        }
    }
}
