using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Material : ItemBase
    {
        public Material(Data.Table.Item data) : base(data)
        {
        }

        public override string ToItemInfo()
        {
            return $"{Data.elemental} 속성. {Data.stat} 을 최소 {Data.minStat} ~ 최대 {Data.maxStat} 까지 상승시켜준다.";
        }
    }
}
