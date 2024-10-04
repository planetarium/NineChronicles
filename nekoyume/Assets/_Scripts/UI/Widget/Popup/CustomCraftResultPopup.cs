using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using UnityEngine;

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
        private CustomCraftResultView view;

        private readonly int _randomShowHash = Animator.StringToHash("RandomShow");
        private readonly int _normalShowHash = Animator.StringToHash("NormalShow");

        public void Show(Equipment resultEquipment)
        {
            view.Show(resultEquipment);
            base.Show();
            Animator.Play(resultEquipment.CraftWithRandom ? _randomShowHash : _normalShowHash);
        }

        public void PlayIconJudgementSfx()
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.CustomCraftJudge1);
        }

        public void PlayStatPercentSfx()
        {
            AudioController.instance.PlaySfx(AudioController.SfxCode.CustomCraftJudge3);
        }
    }
}
