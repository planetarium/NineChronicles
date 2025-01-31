using GeneratedApiNamespace.ArenaServiceClient;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class ClanRankCell : RectCell<
        ClanResponse,
        RankScroll.ContextModel>
    {
        [SerializeField]
        private Image rankImage = null;

        [SerializeField]
        private TextMeshProUGUI rankText = null;

        [SerializeField]
        private Sprite firstPlaceSprite = null;

        [SerializeField]
        private Sprite secondPlaceSprite = null;

        [SerializeField]
        private Sprite thirdPlaceSprite = null;

        private void UpdateRank(int? rank)
        {
            switch (rank)
            {
                case null:
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
        }

        public override void UpdateContent(ClanResponse viewModel)
        {
            UpdateRank(viewModel.Rank);
        }
    }
}
