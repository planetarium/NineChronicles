using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Model;
using Nekoyume.Model.Buff;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BuffIcon : MonoBehaviour
    {
        public Image image;
        public TextMeshProUGUI remainedDurationText;
        public Buff Data { get; set; }
        public CharacterBase character;

        public void Show(Buff buff, bool isAdded)
        {
            Data = buff;
            gameObject.SetActive(true);
            remainedDurationText.enabled = true;
            var sprite = Data.GetIcon();
            image.overrideSprite = sprite;
            UpdateStatus(Data);

            if (isAdded &&
                enabled)
            {
                VFXController.instance
                    .CreateAndChaseRectTransform<DropItemInventoryVFX>(image.rectTransform);
            }
        }

        public void UpdateStatus(Buff buff)
        {
            Data = buff;
            remainedDurationText.text = Data.RemainedDuration.ToString();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            remainedDurationText.enabled = false;
            image.overrideSprite = null;
        }
    }
}
