using Libplanet;
using Nekoyume.Game.Controller;
using Nekoyume.Model.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class RankCell : MonoBehaviour
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
        private TextMeshProUGUI firstElement = null;

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

        public void SetDataAsAbility(AbilityRankingModel rankingInfo)
        {
            nicknameText.text = rankingInfo.AvatarState.name;
            addressText.text = rankingInfo.AvatarState.address
                .ToString()
                .Remove(addressStringCount);

            firstElement.text = rankingInfo.Cp.ToString();
            secondElement.text = rankingInfo.AvatarState.level.ToString();
            firstElement.gameObject.SetActive(true);
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
            addressText.text = rankingInfo.AvatarState.address
                .ToString()
                .Remove(addressStringCount);

            firstElement.text = rankingInfo.Stage.ToString();
            firstElement.gameObject.SetActive(true);
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
    }
}
