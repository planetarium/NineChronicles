using Nekoyume.Model.Item;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EquipmentCombinationPanel : MonoBehaviour
    {
        public EquipmentRecipeCellView recipeCellView;

        public Button cancelButton;
        public Button submitButton;

        public void Awake()
        {
            cancelButton.onClick.AddListener(SubscribeOnClickCancel);
            submitButton.onClick.AddListener(SubscribeOnClickSubmit);
        }

        public void SetData(Equipment equipment)
        {
            recipeCellView.Set(equipment);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SubscribeOnClickCancel()
        {
            Widget.Find<Combination>().State.SetValueAndForceNotify(Combination.StateType.CombineEquipment);
        }

        public void SubscribeOnClickSubmit()
        {
            Widget.Find<Combination>().State.SetValueAndForceNotify(Combination.StateType.CombineEquipment);
        }
    }
}
