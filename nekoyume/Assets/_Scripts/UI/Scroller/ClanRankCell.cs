using System.Globalization;
using Cysharp.Threading.Tasks;
using GeneratedApiNamespace.ArenaServiceClient;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class ClanRankCell : RectCell<
        ClanResponse,
        ClanRankScroll.ContextModel>
    {
        [SerializeField]
        private Image rankImage = null;

        [SerializeField]
        private TextMeshProUGUI clanName;

        [SerializeField]
        private TextMeshProUGUI clanScore;

        [SerializeField]
        private Image clanIcon;

        [SerializeField]
        private TextMeshProUGUI rankText = null;

        [SerializeField]
        private Sprite firstPlaceSprite = null;

        [SerializeField]
        private Sprite secondPlaceSprite = null;

        [SerializeField]
        private Sprite thirdPlaceSprite = null;

        [SerializeField]
        private GameObject emptyClanIcon;

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
            emptyClanIcon.SetActive(true);
            clanIcon.gameObject.SetActive(false);
            
            if (viewModel == null)
            {
                return;
            }
            UpdateRank(viewModel.Rank);

            clanName.text = viewModel.Name;
            clanScore.text = viewModel.Score.ToString("N0", CultureInfo.CurrentCulture);
            if (string.IsNullOrEmpty(viewModel.ImageURL))
            {
                emptyClanIcon.SetActive(true);
                clanIcon.gameObject.SetActive(false);
                return;
            }
            Util.DownloadTexture(viewModel.ImageURL).ContinueWith((result) =>
            {
                if (result != null)
                {
                    clanIcon.sprite = result;
                    emptyClanIcon.SetActive(false);
                    clanIcon.gameObject.SetActive(true);
                }
                else
                {
                    emptyClanIcon.SetActive(true);
                    clanIcon.gameObject.SetActive(false);
                }
            });
        }
    }
}
