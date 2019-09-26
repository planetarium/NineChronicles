using System;
using Nekoyume.Model;
using Nekoyume.TableData;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemInformationStat : IDisposable
    {
        public readonly ReactiveProperty<string> key = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> value = new ReactiveProperty<string>();

        public ItemInformationStat(MaterialItemSheet.Row itemRow)
        {
            key.Value = itemRow.StatType.ToStatString();
            value.Value = $"{itemRow.StatMin} - {itemRow.StatMax}";
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
