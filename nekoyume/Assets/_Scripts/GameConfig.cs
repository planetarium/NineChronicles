using System.Collections.Generic;
using Unity.Mathematics;

namespace Nekoyume
{
    public class GameConfig
    {
        public const int SlotCount = 3;
        
        #region action

        public const int DefaultAvatarCharacterId = 100010;
        public const int DefaultAvatarWeaponId = 10110000;
        public const int DefaultAvatarArmorId = 10200000;
        public const int DefaultAvatarBeltId = 10310000;
        
        public const float CombinationValueP1 = 30f;
        public const float CombinationValueP2 = 1.2f;
        public const float CombinationValueL1 = 10f;
        public const float CombinationValueL2 = 1f;
        public const float CombinationValueR1 = 2f;
        public const int CombinationDefaultFoodId = 200000;
        
        #endregion
        
        public static readonly List<int> EquipmentMaterials = new List<int>
        {
            303000, 303001, 303002, 303100, 303101, 303102, 303200, 303201, 303202
        };
        public static readonly int2 ScreenSize = new int2(1136, 640);
    }
}
