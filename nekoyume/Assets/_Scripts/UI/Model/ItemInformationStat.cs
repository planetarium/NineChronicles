

using Nekoyume.Model;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class ItemInformationStat
    {
        public readonly ReactiveProperty<Sprite> image = new ReactiveProperty<Sprite>();
        public readonly ReactiveProperty<string> key = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> value = new ReactiveProperty<string>();

        public ItemInformationStat(Data.Table.Item itemRow)
        {
            key.Value = itemRow.stat.ToStatString();
            value.Value = $"{itemRow.minStat} - {itemRow.maxStat}";
        }

        public ItemInformationStat(IStatMap statMap)
        {
            key.Value = statMap.Key;
            value.Value = $"{statMap.TotalValue}";
        }
    }
}
