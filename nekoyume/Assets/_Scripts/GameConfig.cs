using System.Collections.Generic;
using Unity.Mathematics;

namespace Nekoyume
{
    public class GameConfig
    {
        public const int SlotCount = 3;
        
        #region action

        public const int DefaultAvatarCharacterId = 100010;
        public const int DefaultAvatarWeaponId = 10100000;
        public const int DefaultAvatarArmorId = 10200000;
        public const int DefaultAvatarBeltId = 10310000;
        
        public const float CombinationValueP1 = 30f;
        public const float CombinationValueP2 = 1.2f;
        public const float CombinationValueL1 = 10f;
        public const float CombinationValueL2 = 1f;
        public const float CombinationValueR1 = 2f;
        public const int CombinationDefaultFoodId = 200000;
        
        #endregion
        
        public static readonly int[] EquipmentMaterials =
        {
            303000,
            303001,
            303002,
            303100,
            303101,
            303102,
            303200,
            303201,
            303202,
            303300,
            303301,
            303302,
            303400,
            303401,
            303402
        };
        
        public static readonly int[] ConsumableMaterials =
        {
            302000,
            302001,
            302002,
            302003,
            302004,
            302005,
            302006,
            302007,
            302008,
            302009
        };
        
        public static readonly int[] PaintMaterials =
        {
            100000,
            301000,
            304000,
            304002,
            304001,
            304003,
            305000,
            305001,
            305002,
            305003,
            305004
        };
        
        public static readonly int2 ScreenSize = new int2(1136, 640);
    }
}
