using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game.Buff;
using Nekoyume.TableData;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class HpBar : ProgressBar
    {
        public Image buffImage;
        public GameObject buffList;

        public void UpdateBuff(Dictionary<BuffCategory, Buff> modelBuffs)
        {
            ClearBuff();
            var buffs = modelBuffs.Values.OrderBy(r => r.data.id);
            buffImage.gameObject.SetActive(true);
            foreach (var buff in buffs)
            {
                var icon = buff.data.GetIcon();
                var go = Instantiate(buffImage, buffList.transform);
                go.sprite = icon;
            }
            buffImage.gameObject.SetActive(false);
        }

        private void ClearBuff()
        {
            foreach (Transform child in buffList.transform)
            {
                Destroy(child.gameObject);
            }

        }
    }
}
