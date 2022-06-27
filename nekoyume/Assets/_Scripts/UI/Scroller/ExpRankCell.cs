using System;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class ExpRankCell : RectCell<
        ExpRankCell.ViewModel,
        ExpRankScroll.ContextModel>
    {
        public class ViewModel
        {
            public int rank;
            public Nekoyume.Model.State.RankingInfo rankingInfo;
        }

        [SerializeField]
        private Image backgroundImage = null;

        [SerializeField]
        private Button avatarInfoButton = null;

        [SerializeField]
        private TextMeshProUGUI rankText = null;

        [SerializeField]
        private FramedCharacterView characterView = null;

        [SerializeField]
        private TextMeshProUGUI levelText = null;

        [SerializeField]
        private TextMeshProUGUI idText = null;

        [SerializeField]
        private TextMeshProUGUI stageText = null;

        private RectTransform _rectTransformCache;
        private bool _isCurrentUser;

        public RectTransform RectTransform => _rectTransformCache
            ? _rectTransformCache
            : _rectTransformCache = GetComponent<RectTransform>();

        public Nekoyume.Model.State.RankingInfo RankingInfo { get; private set; }

        private void Awake()
        {
            avatarInfoButton.OnClickAsObservable()
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Context.OnClick.OnNext(this);
                })
                .AddTo(gameObject);

            Game.Event.OnUpdatePlayerEquip
                .Where(_ => _isCurrentUser)
                .Subscribe(characterView.SetByPlayer)
                .AddTo(gameObject);
        }

        public override void UpdateContent(ViewModel itemData)
        {
            var rank = itemData.rank;
            var rankingInfo = itemData.rankingInfo;

            RankingInfo = rankingInfo ?? throw new ArgumentNullException(nameof(rankingInfo));
            _isCurrentUser = States.Instance.CurrentAvatarState?.address ==
                             RankingInfo.AvatarAddress;

            backgroundImage.enabled = Index % 2 == 1;
            rankText.text = rank.ToString();
            levelText.text = RankingInfo.Level.ToString();
            idText.text = RankingInfo.AvatarName;
            stageText.text = RankingInfo.Exp.ToString();

            if (_isCurrentUser)
            {
                var player = Game.Game.instance.Stage.SelectedPlayer;
                if (player is null)
                {
                    player = Game.Game.instance.Stage.GetPlayer();
                    characterView.SetByPlayer(player);
                    player.gameObject.SetActive(false);
                }
                else
                {
                    characterView.SetByPlayer(player);
                }
            }
            else
            {
                //FIXME 현재 코스튬대응이 안되있음 lib9c쪽과 함께 고쳐야함
                characterView.SetByFullCostumeOrArmorId(rankingInfo.ArmorId);
            }
        }
    }
}
