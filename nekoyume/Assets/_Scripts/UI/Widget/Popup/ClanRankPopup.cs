using Cysharp.Threading.Tasks;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class ClanRankPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton = null;

        [SerializeField]
        private ClanRankScroll clanRankScroll = null;

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
            //clanRankScroll.Show(rankingInfos, true);
        }
    }
}
