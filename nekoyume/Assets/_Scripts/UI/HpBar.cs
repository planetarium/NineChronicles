using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.UI.Module;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class HpBar : ProgressBar
    {
        public BuffLayout buffLayout;
        public Text levelText;
        public Slider additionalSlider;

        private HpBarVFX _hpVFX;

        public void SetBuffs(IReadOnlyDictionary<int, Buff> buffs)
        {
            buffLayout.SetBuff(buffs);

            if (buffLayout.IsBuffAdded(EnumType.StatType.HP))
            {
                _hpVFX?.Stop();
                var rectTransform = bar.rectTransform;
                _hpVFX = VFXController.instance.CreateAndChaseRectTransform<HpBarVFX>(rectTransform.position, rectTransform);
                _hpVFX.Play();
            }
            else if (!buffLayout.HasBuff(EnumType.StatType.HP))
            {
                _hpVFX?.Stop();
            }
        }

        public void SetLevel(int value)
        {
            levelText.text = value.ToString();
        }

        public void Set(int current, int additional, int max)
        {
            Set(current, max);

            bool isHPBoosted = additional > 0;
            additionalSlider.gameObject.SetActive(isHPBoosted);
            if (isHPBoosted)
                additionalSlider.value = (float) current / max;
        }
    }
}
