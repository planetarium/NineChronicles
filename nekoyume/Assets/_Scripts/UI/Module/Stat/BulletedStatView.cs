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

        public void Show(StatMapEx statMapEx, bool isMainStat)
        {
            if (statMapEx is null)
            {
                Hide();
                return;
            }

            bulletMainImage.enabled = isMainStat;
            bulletSubImage.enabled = !isMainStat;

            if (isMainStat)
                Show(statMapEx.StatType, statMapEx.ValueAsInt, statMapEx.AdditionalValueAsInt);
            else
                Show(statMapEx.StatType, statMapEx.TotalValueAsInt, 0);
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
