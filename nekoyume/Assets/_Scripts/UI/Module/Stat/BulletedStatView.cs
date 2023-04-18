using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class BulletedStatView : DetailedStatView
    {
        public Image bulletMainImage;
        public Image bulletSubImage;

        public bool IsShow => gameObject.activeSelf;

        public void Show(DecimalStat decimalStat, bool isMainStat)
        {
            if (decimalStat is null)
            {
                Hide();
                return;
            }

            bulletMainImage.enabled = isMainStat;
            bulletSubImage.enabled = !isMainStat;

            if (isMainStat)
                Show(decimalStat.StatType, decimalStat.BaseValue, decimalStat.AdditionalValue);
            else
                Show(decimalStat.StatType, decimalStat.TotalValueAsInt, 0);
        }

        public override void Show()
        {
            gameObject.SetActive(true);
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
