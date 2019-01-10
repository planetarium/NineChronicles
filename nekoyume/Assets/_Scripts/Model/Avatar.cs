using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;

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
    }
}
