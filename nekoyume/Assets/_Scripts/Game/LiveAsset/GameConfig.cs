using System;
using UnityEngine;

namespace Nekoyume.Game.LiveAsset
{
    [Serializable]
    public class GameConfig
    {
        public int SecondsPerBlock { get; set; }

        public class RequiredStage
        {
            public const int WorkShop = 0;
            public const int CraftConsumable = 0;
            public const int Enhancement = 35;
            public const int Rune = 23;
            public const int Grind = 40;
            public const int Shop = 17;
            public const int Arena = 15;
            public const int Mimisbrunnr = 100;
            public const int WorldBoss = 49;
            public const int Adventure = 0;
            public const int ChargeAP = 23;
            public const int ShowPopupRoomEntering = 51;
            public const int Sweep = 23;
            public const int SeasonPass = 15;
            public const int TutorialEnd = 10;
        }

        public const string DiscordLink = "https://discord.com/invite/planetarium";

        public const string PackageNameForKorean = "com.planetariumlabs.ninechroniclesmobilek";
        public static bool IsKoreanBuild => Application.identifier switch
        {
            PackageNameForKorean => true,
            _ => false
        };
    }
}
