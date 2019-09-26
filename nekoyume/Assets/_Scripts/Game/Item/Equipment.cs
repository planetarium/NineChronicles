using System;
using Nekoyume.TableData;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Equipment : ItemUsable
    {
        public bool equipped = false;
        
        public new EquipmentItemSheet.Row Data { get; }

        public Equipment(EquipmentItemSheet.Row data, Guid id) : base(data, id)
        {
            Data = data;
            
//            //TODO 논의후 테이블에 제대로 설정되야함.
//            Stats.AddStatValue("turnSpeed", Data.turnSpeed);
//            //TODO 장비대신 스킬별 사거리를 사용해야함.
//            Stats.AddStatValue("attackRange", Data.attackRange);
        }

        public bool Equip()
        {
            equipped = true;
            return true;
        }

        public bool Unequip()
        {
            equipped = false;
            return true;
        }
    }
}
