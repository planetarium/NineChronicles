using Cysharp.Threading.Tasks;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    using Nekoyume.Model.Item;
    using UniRx;

    public class ClanRankPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton = null;

        [SerializeField]
        private RankScroll rankScroll = null;

        [SerializeField]
        private RankCellPanel myInfoCell = null;

        [SerializeField]
        private GameObject preloadingObject = null;

        [SerializeField]
        private GameObject missingObject = null;

        [SerializeField]
        private TextMeshProUGUI missingText = null;

        [SerializeField]
        private GameObject refreshObject = null;

        [SerializeField]
        private Button refreshButton = null;

        public const int RankingBoardDisplayCount = 100;

        public override void Initialize()
        {
            base.Initialize();
            closeButton.onClick.AsObservable()
                .Subscribe(_ =>
                {
                    Close();
                    AudioController.PlayClick();
                })
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
        }

        private void SetScroll<T>(
            IReadOnlyDictionary<int, T> myRecordMap,
            IEnumerable<T> rankingInfos)
            where T : RankingModel
        {
            if (rankingInfos is null)
            {
                Find<Alert>().Show("UI_ERROR", "UI_RANKING_CATEGORY_ERROR");
                rankingInfos = new List<T>();
            }

            var states = States.Instance;
            if (myRecordMap.TryGetValue(states.CurrentAvatarKey, out var rankingInfo))
            {
                myInfoCell.SetData(rankingInfo);
            }
            else
            {
                myInfoCell.SetEmpty(states.CurrentAvatarState);
            }

            rankScroll.Show(rankingInfos, true);
        }
    }
}
