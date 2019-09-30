using System;
using Nekoyume.EnumType;
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
            key.Value = itemRow.StatType.HasValue
                ? itemRow.StatType.Value.GetLocalizedString()
                : $"{nameof(itemRow.StatType)} has not value";
            value.Value = $"{itemRow.StatMin} - {itemRow.StatMax}";
        }

        public ItemInformationStat(StatMap statMap)
        {
            key.Value = statMap.StatType.GetLocalizedString();
            value.Value = $"{statMap.TotalValueAsInt}";
        }

        public void Dispose()
        {
            key.Dispose();
            value.Dispose();
        }
    }
}
