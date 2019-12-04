using System;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.TableData;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class BulletedStatView : IDisposable
    {
        public readonly ReactiveProperty<bool> IsMainStat = new ReactiveProperty<bool>();
        public readonly ReactiveProperty<string> Key = new ReactiveProperty<string>();
        public readonly ReactiveProperty<string> Value = new ReactiveProperty<string>();

        public BulletedStatView(MaterialItemSheet.Row itemRow, bool isMainStat = false)
        {
            IsMainStat.Value = isMainStat;
            Key.Value = itemRow.StatType != StatType.NONE
                ? itemRow.StatType.GetLocalizedString()
                : $"{nameof(itemRow.StatType)} has not value";
            Value.Value = $"{itemRow.StatMin} - {itemRow.StatMax}";
        }

        public BulletedStatView(StatMapEx statMapEx, bool isMainStat = false)
        {
            IsMainStat.Value = isMainStat;
            Key.Value = statMapEx.StatType.GetLocalizedString();

            if (isMainStat && statMapEx.HasAdditionalValue)
            {
                Value.Value = $"{statMapEx.ValueAsInt} <color=green>(+{statMapEx.AdditionalValueAsInt})</color>";
            }
            else
            {
                Value.Value = $"{statMapEx.TotalValueAsInt}";
            }
        }

        public void Dispose()
        {
            Key.Dispose();
            Value.Dispose();
        }
    }
}
