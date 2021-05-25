using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class RankCellPanel : MonoBehaviour
    {
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
                    rankImage.gameObject.SetActive(false);
                    rankText.gameObject.SetActive(true);
                    rankText.text = "-";
                    break;
                case 1:
                    rankImage.gameObject.SetActive(true);
                    rankText.gameObject.SetActive(false);
                    rankImage.sprite = firstPlaceSprite;
                    break;
                case 2:
                    rankImage.gameObject.SetActive(true);
                    rankText.gameObject.SetActive(false);
                    rankImage.sprite = secondPlaceSprite;
                    break;
                case 3:
                    rankImage.gameObject.SetActive(true);
                    rankText.gameObject.SetActive(false);
                    rankImage.sprite = thirdPlaceSprite;
                    break;
                default:
                    rankImage.gameObject.SetActive(false);
                    rankText.gameObject.SetActive(true);
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

            firstElementText.text = rankingInfo.ClearedStageId.ToString();
            firstElementText.gameObject.SetActive(true);
            firstElementCpText.gameObject.SetActive(false);
            secondElement.gameObject.SetActive(false);

            var rank = rankingInfo.Rank;
            switch (rank)
            {
                case 1:
                    rankImage.gameObject.SetActive(true);
                    rankText.gameObject.SetActive(false);
                    rankImage.sprite = firstPlaceSprite;
                    break;
                case 2:
                    rankImage.gameObject.SetActive(true);
                    rankText.gameObject.SetActive(false);
                    rankImage.sprite = secondPlaceSprite;
                    break;
                case 3:
                    rankImage.gameObject.SetActive(true);
                    rankText.gameObject.SetActive(false);
                    rankImage.sprite = thirdPlaceSprite;
                    break;
                default:
                    rankImage.gameObject.SetActive(false);
                    rankText.gameObject.SetActive(true);
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
