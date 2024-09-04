using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    /// <summary>
    /// 일단 로딩창이 나옴
    /// 그 다음엔 외형이랑 스펙이 나옴
    /// 스펙엔 이름, 베이스 스탯, 옵션으로 붙은 스탯이 포함됨
    /// cp가 몇인지도 보여줘야하고
    /// </summary>
    public class CustomCraftResultPopup : PopupWidget
    {
        [SerializeField]
        private VanillaItemView itemView;

        public void Show(Equipment resultEquipment)
        {
            // 일단 이러면 아이템뷰 세팅은 됨
            itemView.SetData(resultEquipment);
            base.Show();
        }
    }
}
