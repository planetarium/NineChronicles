using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.UI;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.State;
using Convert = System.Convert;

namespace Nekoyume
{
    public class TestbedEditor : Widget
    {
        public override WidgetType WidgetType => WidgetType.Development;

        [SerializeField]
        private GameObject content;

        [SerializeField]
        private InputField championshipId;
        [SerializeField]
        private InputField round;
        [SerializeField]
        private InputField accountCount;

        public void OnClickActive()
        {
            content.SetActive(!content.activeSelf);
            championshipId.interactable = true;
            round.interactable = true;
            accountCount.interactable = true;
        }

        public void OnClickCreateTestbed()
        {
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            Game.Game.instance.ActionManager.CreateTestbed().Subscribe();
#endif
        }

        public void OnClickCreateArenaDummy()
        {
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            var inventory = States.Instance.CurrentAvatarState.inventory;

            Game.Game.instance.ActionManager.CreateArenaDummy(
                inventory.Costumes
                    .Where(e => e.Equipped)
                    .Select(e => e.NonFungibleId)
                    .ToList(),
                inventory.Equipments
                    .Where(e => e.Equipped)
                    .Select(e => e.NonFungibleId)
                    .ToList(),
                Convert.ToInt16(championshipId.text),
                Convert.ToInt16(round.text),
                Convert.ToInt16(accountCount.text)
            ).Subscribe();
#endif
        }
    }
}
