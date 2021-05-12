using Nekoyume.Model.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class RankCell : RectCell<
        RankingModel,
        RankScroll.ContextModel>
    {
        [SerializeField]
        private GameObject imageContainer = null;

        [SerializeField]
        private GameObject textContainer = null;

        [SerializeField]
        private Image rankImage = null;

        [SerializeField]
        private TextMeshProUGUI rankText = null;

        [SerializeField]
        private DetailedCharacterView characterView = null;

        [SerializeField]
        private TextMeshProUGUI nicknameText = null;

        [SerializeField]
        private TextMeshProUGUI addressText = null;

        [SerializeField]
        private TextMeshProUGUI firstElementCpText = null;

        [SerializeField]
        private TextMeshProUGUI firstElementText = null;

        [SerializeField]
        private TextMeshProUGUI secondElement = null;

        [SerializeField]
        private Sprite firstPlaceSprite = null;

        [SerializeField]
        private Sprite secondPlaceSprite = null;

        [SerializeField]
        private Sprite thirdPlaceSprite = null;

        [SerializeField]
        private int addressStringCount = 6;

        private void Awake()
        {
            characterView.OnClickCharacterIcon
                .Subscribe(avatarState =>
                {
                    if (avatarState is null)
                    {
                        return;
                    }

                    Widget.Find<FriendInfoPopup>().Show(avatarState);
                })
                .AddTo(gameObject);
        }

        public override void UpdateContent(RankingModel viewModel)
        {
            if (viewModel is AbilityRankingModel abilityRankingModel)
            {
                SetDataAsAbility(abilityRankingModel);
            }
            else if (viewModel is StageRankingModel stageRankingModel)
            {
                SetDataAsStage(stageRankingModel);
            }
        }

        public void SetDataAsAbility(AbilityRankingModel rankingInfo)
        {
            nicknameText.text = rankingInfo.AvatarState.name;
            nicknameText.gameObject.SetActive(true);
            addressText.text = rankingInfo.AvatarState.address
                .ToString()
                .Remove(addressStringCount);

            firstElementCpText.text = rankingInfo.Cp.ToString();
            secondElement.text = rankingInfo.AvatarState.level.ToString();

            firstElementText.gameObject.SetActive(false);
            firstElementCpText.gameObject.SetActive(true);
            secondElement.gameObject.SetActive(true);

            var rank = rankingInfo.Rank;
            switch (rank)
            {
                case 0:
                    imageContainer.SetActive(false);
                    textContainer.SetActive(true);
                    rankText.text = "-";
                    break;
                case 1:
                    imageContainer.SetActive(true);
                    textContainer.SetActive(false);
                    rankImage.sprite = firstPlaceSprite;
                    break;
                case 2:
                    imageContainer.SetActive(true);
                    textContainer.SetActive(false);
                    rankImage.sprite = secondPlaceSprite;
                    break;
                case 3:
                    imageContainer.SetActive(true);
                    textContainer.SetActive(false);
                    rankImage.sprite = thirdPlaceSprite;
                    break;
                default:
                    imageContainer.SetActive(false);
                    textContainer.SetActive(true);
                    rankText.text = rank.ToString();
                    break;
            }

            characterView.SetByAvatarState(rankingInfo.AvatarState);
            gameObject.SetActive(true);
        }

        public void SetDataAsStage(StageRankingModel rankingInfo)
        {
            nicknameText.text = rankingInfo.AvatarState.name;
            nicknameText.gameObject.SetActive(true);
            addressText.text = rankingInfo.AvatarState.address
                .ToString()
                .Remove(addressStringCount);

            firstElementText.text = rankingInfo.Stage.ToString();
            firstElementText.gameObject.SetActive(true);
            firstElementCpText.gameObject.SetActive(false);
            secondElement.gameObject.SetActive(false);

            var rank = rankingInfo.Rank;
            switch (rank)
            {
                case 1:
                    imageContainer.SetActive(true);
                    textContainer.SetActive(false);
                    rankImage.sprite = firstPlaceSprite;
                    break;
                case 2:
                    imageContainer.SetActive(true);
                    textContainer.SetActive(false);
                    rankImage.sprite = secondPlaceSprite;
                    break;
                case 3:
                    imageContainer.SetActive(true);
                    textContainer.SetActive(false);
                    rankImage.sprite = thirdPlaceSprite;
                    break;
                default:
                    imageContainer.SetActive(false);
                    textContainer.SetActive(true);
                    rankText.text = rank.ToString();
                    break;
            }

            characterView.SetByAvatarState(rankingInfo.AvatarState);
            gameObject.SetActive(true);
        }

        public void SetEmpty(AvatarState avatarState)
        {
            rankText.text = "-";
            characterView.SetByAvatarState(avatarState);
            nicknameText.text = avatarState.name;
            addressText.text = avatarState.address.ToString();

            firstElementText.text = "-";
            firstElementText.gameObject.SetActive(true);
            firstElementCpText.gameObject.SetActive(false);
            secondElement.gameObject.SetActive(false);
        }
    }
}
