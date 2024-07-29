using System;
using Libplanet.Types.Assets;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Item : IDisposable
    {
        public readonly ReactiveProperty<ItemBase> ItemBase = new();
        public readonly ReactiveProperty<FungibleAssetValue> FungibleAssetValue = new();
        public readonly ReactiveProperty<bool> GradeEnabled = new(true);
        public readonly ReactiveProperty<string> Enhancement = new();
        public readonly ReactiveProperty<bool> EnhancementEnabled = new(false);
        public readonly ReactiveProperty<bool> EnhancementEffectEnabled = new(false);
        public readonly ReactiveProperty<bool> Dimmed = new(false);
        public readonly ReactiveProperty<bool> Selected = new(false);
        public readonly ReactiveProperty<bool> ActiveSelf = new(true);

        public readonly Subject<Item> OnClick = new();
        public readonly Subject<Item> OnDoubleClick = new();

        public Item(ItemBase value)
        {
            ItemBase.Value = value;

            if (ItemBase.Value is Equipment equipment &&
                equipment.level > 0)
            {
                Enhancement.Value = $"+{equipment.level}";
                EnhancementEnabled.Value = true;
                EnhancementEffectEnabled.Value = equipment.level >= Util.VisibleEnhancementEffectLevel;
            }
            else
            {
                Enhancement.Value = string.Empty;
                EnhancementEnabled.Value = false;
                EnhancementEffectEnabled.Value = false;
            }
        }

        public Item(FungibleAssetValue value)
        {
            FungibleAssetValue.Value = value;
            EnhancementEnabled.Value = false;
            EnhancementEffectEnabled.Value = false;
        }

        public virtual void Dispose()
        {
            ItemBase.Dispose();
            GradeEnabled.Dispose();
            Enhancement.Dispose();
            EnhancementEnabled.Dispose();
            EnhancementEffectEnabled.Dispose();
            Dimmed.Dispose();
            ActiveSelf.Dispose();
            Selected.Dispose();

            OnClick.Dispose();
            OnDoubleClick.Dispose();
        }
    }
}
