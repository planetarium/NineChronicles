using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Module;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class HpBar : ProgressBar
    {
        [SerializeField]
        private BuffLayout buffLayout = null;

        [SerializeField]
        private TextMeshProUGUI levelText = null;

        [SerializeField]
        private Slider additionalSlider = null;

        public HpBarVFX HpVFX { get; private set; }

        public void SetBuffs(IReadOnlyDictionary<int, Buff> buffs, TableSheets tableSheets)
        {
            buffLayout.SetBuff(buffs, tableSheets, true);

            if (buffLayout.IsBuffAdded(StatType.HP))
            {
                if (HpVFX)
                {
                    HpVFX.Stop();
                }

                var rectTransform = bar.rectTransform;
                HpVFX = VFXController.instance.CreateAndChaseRectTransform<HpBarVFX>(rectTransform);
                HpVFX.Play();
            }
            else if (!buffLayout.HasBuff(StatType.HP))
            {
                if (HpVFX)
                {
                    HpVFX.Stop();
                }
            }
        }

        public void SetLevel(int value)
        {
            levelText.text = value.ToString();
        }

        public void Set(long current, long additional, long max)
        {
            SetText($"{current} / {max}");
            SetValue((float)math.min(current, max - additional) / max);

            var isHPBoosted = additional > 0;
            additionalSlider.gameObject.SetActive(isHPBoosted);
            if (isHPBoosted)
            {
                additionalSlider.value = (float)current / max;
            }
        }

        protected override void OnDestroy()
        {
            if (HpVFX)
            {
                HpVFX.Stop();
            }

            base.OnDestroy();
        }
    }
}
