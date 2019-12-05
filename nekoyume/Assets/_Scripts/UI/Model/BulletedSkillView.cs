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
        public readonly ReactiveProperty<StatType> Key = new ReactiveProperty<StatType>();
        public readonly ReactiveProperty<int> Value = new ReactiveProperty<int>();
        public readonly ReactiveProperty<(int, int)> ValueRange = new ReactiveProperty<(int, int)>();
        public readonly ReactiveProperty<int> Additional = new ReactiveProperty<int>();

        public BulletedStatView(MaterialItemSheet.Row itemRow, bool isMainStat = false)
        {
            IsMainStat.Value = isMainStat;
            Key.Value = itemRow.StatType;
            ValueRange.Value = (itemRow.StatMin, itemRow.StatMax);
            Additional.Value = 0;
        }

        public BulletedStatView(StatMapEx statMapEx, bool isMainStat = false)
        {
            IsMainStat.Value = isMainStat;
            Key.Value = statMapEx.StatType;
            Value.Value = statMapEx.ValueAsInt;
            Additional.Value = statMapEx.AdditionalValueAsInt;
        }

        public void Dispose()
        {
            IsMainStat.Dispose();
            Key.Dispose();
            Value.Dispose();
            Additional.Dispose();
        }
    }
}
