using System;
using Nekoyume.Model;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemInformationStat : IDisposable
    {
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

        public void Dispose()
        {
            key.Dispose();
            value.Dispose();
        }
    }
}
