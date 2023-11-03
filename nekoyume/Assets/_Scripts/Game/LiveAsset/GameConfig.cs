using System;

namespace Nekoyume.Game.LiveAsset
{
    [Serializable]
    public class GameConfig
    {
        public int SecondsPerBlock { get; set; }

        public class RequiredStage
        {
            public const int WorkShop = 1;
            public const int CraftConsumable = 1;
            public const int Enhancement = 1;
            public const int Shop = 17;
            public const int Arena = 15;
            public const int Mimisbrunnr = 100;
            public const int WorldBoss = 50;
            public const int Adventure = 0;
            public const int ChargeAP = 23;
            public const int ShowPopupRoomEntering = 51;
        }
    }
}
