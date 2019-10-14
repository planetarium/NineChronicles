using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.UI.Module;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class HpBar : ProgressBar
    {
        public BuffLayout buffLayout;
        public Text levelText;

        public void SetBuffs(IReadOnlyDictionary<int, Buff> value)
        {
            buffLayout.UpdateBuff(value.Values);
        }

        public void SetLevel(int value)
        {
            levelText.text = value.ToString();
        }
    }
}
