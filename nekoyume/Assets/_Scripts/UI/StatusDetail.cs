using Nekoyume.Data.Table;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StatusDetail : Widget
    {
        public Text textAtk;
        public Text textDef;

        public void Init(Stats stats)
        {
//            textAtk.text = stats.Attack.ToString();
//            textDef.text = stats.Defense.ToString();
        }

        public void CloseClick()
        {
            var status = Widget.Find<Status>();
            if (status)
            {
                status.BtnStatus.group.SetAllTogglesOff();
            }
        }
    }
}
