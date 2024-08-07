using System;
using System.Collections.Generic;
using Nekoyume.State;
using Nekoyume.TableData.CustomEquipmentCraft;

namespace Nekoyume.UI.Model
{
    using UniRx;
    public class CustomOutfit : IDisposable
    {
        public readonly ReactiveProperty<CustomEquipmentCraftIconSheet.Row> IconRow = new();
        public readonly ReactiveProperty<bool> Dimmed = new(false);
        public readonly ReactiveProperty<bool> Selected = new(false);
        public readonly ReactiveProperty<bool> RandomOnly = new(false);

        public readonly Subject<CustomOutfit> OnClick = new();
        private readonly List<IDisposable> _disposables = new();

        public CustomOutfit(CustomEquipmentCraftIconSheet.Row row)
        {
            IconRow.Value = row;
            if (row is not null)
            {
                RandomOnly.Value = row.RandomOnly;
                ReactiveAvatarState.ObservableRelationship.Subscribe(relationship =>
                {
                    Dimmed.Value = row.RequiredRelationship > relationship;
                }).AddTo(_disposables);
            }
            else
            {
                Selected.Value = false;
                RandomOnly.Value = false;
                Dimmed.Value = false;
            }
        }

        public virtual void Dispose()
        {
            Dimmed.Dispose();
            Selected.Dispose();
            RandomOnly.Dispose();
            OnClick.Dispose();
            _disposables.DisposeAllAndClear();
        }
    }
}
