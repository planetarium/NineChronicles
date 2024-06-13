using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace Nekoyume.UI
{
    using Nekoyume.ActionExtensions;
    using UniRx;
    public class AdventureBossFullBountyStatusPopup : PopupWidget
    {
        [SerializeField] private BountyScrollView scrollView;
        [SerializeField] private BountyCell myBountyCell;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            Game.Game.instance.AdventureBossData.BountyBoard.Subscribe(bountyBoard =>
            {
                if (bountyBoard == null)
                {
                    scrollView.ClearData();
                    return;
                }
                scrollView.UpdateData(bountyBoard.Investors.Select((x, i) =>
                {
                    var data = new BountyItemData
                    {
                        Rank = i + 1,
                        Name = x.Name,
                        Count = x.Count,
                        Ncg = x.Price.MajorUnit,
                        Bonus = i == 0 ? 1.5f : 0
                    };

                    if (x.AvatarAddress == Game.Game.instance.States.CurrentAvatarState.address)
                    {
                        myBountyCell.UpdateContent(data);
                    }

                    return data;
                }));
            }).AddTo(_disposables);
        }
        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            _disposables.DisposeAllAndClear();
        }
    }
}
