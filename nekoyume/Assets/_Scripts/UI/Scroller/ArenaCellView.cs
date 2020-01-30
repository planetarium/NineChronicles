using System;
using Libplanet;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Tween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Nekoyume.Game.Controller;

namespace Nekoyume.UI.Scroller
{
    public class ArenaCellView : MonoBehaviour
    {
        public Action<ArenaCellView> onClickChallenge;
        public Action<Address> onClickInfo;

        [SerializeField]
        private GameObject rankImageContainer = null;
        [SerializeField]
        private Image rankImage = null;
        [SerializeField]
        private GameObject rankTextContainer = null;
        [SerializeField]
        private TextMeshProUGUI rankText = null;
        [SerializeField]
        private Image portraitImage = null;
        [SerializeField]
        private TextMeshProUGUI levelText = null;
        [SerializeField]
        private TextMeshProUGUI nameText = null;
        [SerializeField]
        private TextMeshProUGUI cpText = null;
        [SerializeField]
        private TextMeshProUGUI scoreText = null;
        [SerializeField]
        private GameObject challengeCountTextContainer = null;
        [SerializeField]
        private TextMeshProUGUI challengeCountText = null;
        [SerializeField]
        private Button avatarInfoButton = null;
        [SerializeField]
        private SubmitButton challengeButton = null;

        [SerializeField]
        private DOTweenRectTransformMoveBy tweenMove = null;
        [SerializeField]
        private DOTweenGroupAlpha tweenAlpha = null;

        public ArenaInfo ArenaInfo { get; private set; }

        private void Awake()
        {
            challengeButton.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                onClickChallenge.Invoke(this);
            }).AddTo(gameObject);

            avatarInfoButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                onClickInfo.Invoke(ArenaInfo.AvatarAddress);
            }).AddTo(gameObject);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(int rank, ArenaInfo arenaInfo, bool canChallenge, bool isCurrentUser)
        {
            if (arenaInfo is null)
                throw new ArgumentNullException(nameof(arenaInfo));

            ArenaInfo = arenaInfo;

            UpdateRank(rank);
            portraitImage.overrideSprite = SpriteHelper.GetItemIcon(arenaInfo.ArmorId);
            levelText.text = arenaInfo.Level.ToString();
            nameText.text = arenaInfo.AvatarName;
            cpText.text = arenaInfo.CombatPoint.ToString();
            scoreText.text = arenaInfo.Score.ToString();
            challengeCountTextContainer.SetActive(isCurrentUser);
            challengeButton.gameObject.SetActive(!isCurrentUser);

            if (isCurrentUser)
            {
                rank = 1;
                challengeCountText.text = $"{arenaInfo.DailyChallengeCount}/{GameConfig.ArenaChallengeCountMax}";
            }
            else
            {
                challengeButton.SetSubmittable(canChallenge);
            }

            tweenMove.StartDelay = rank * 0.16f;
            tweenAlpha.StartDelay = rank * 0.16f;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdateRank(int rank)
        {
            switch (rank)
            {
                case 1:
                case 2:
                case 3:
                    rankImageContainer.SetActive(true);
                    rankTextContainer.SetActive(false);
                    rankImage.overrideSprite = SpriteHelper.GetRankIcon(rank); 
                    break;
                default:
                    rankImageContainer.SetActive(true);
                    rankTextContainer.SetActive(true);
                    rankText.text = rank.ToString();
                    break;
            }
        }
    }
}
