using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.UI.Module;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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

        public void SetDataAsAbility(int rank, string nickname, Address avatarAddress, int cp, int level)
        {
            nicknameText.text = nickname;
            addressText.text = avatarAddress
                .ToString()
                .Remove(addressStringCount);

            firstElement.text = cp.ToString();
            secondElement.text = level.ToString();
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

            characterView.SetByAvatarAddress(avatarAddress);
            gameObject.SetActive(true);
        }
    }
}
