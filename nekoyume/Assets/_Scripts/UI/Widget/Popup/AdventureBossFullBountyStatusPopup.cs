using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace Nekoyume.UI
{
    using ActionExtensions;
    using Helper;
    using UniRx;

    public class AdventureBossFullBountyStatusPopup : PopupWidget
    {
        [SerializeField] private BountyViewScroll scrollView;
        [SerializeField] private BountyCell myBountyCell;

        private readonly List<IDisposable> _disposables = new();

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

                scrollView.UpdateData(bountyBoard.Investors.OrderByDescending(investor => investor.Price).Select((x, i) =>
                {
                    var data = new BountyItemData
                    {
                        Rank = i + 1,
                        Name = x.Name,
                        Count = x.Count,
                        Ncg = x.Price.MajorUnit,
                        Bonus = i == 0 ? (float)AdventureBossHelper.TotalRewardMultiplier : 0
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
