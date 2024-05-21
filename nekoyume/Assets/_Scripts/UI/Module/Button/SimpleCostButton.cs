using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.State;
using System;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class SimpleCostButton : ConditionalButton
    {
        [Serializable]
        private struct CostObject
        {
            public GameObject parent;
            public Image icon;
            public TextMeshProUGUI text;
        }

        [SerializeField]
        private CostIconDataScriptableObject costIconData;

        [SerializeField]
        private List<CostObject> costObjects;

        [SerializeField]
        private GameObject loadingObject;

        [SerializeField]
        private Button loadingButton;

        private CostType _type;
        private long _cost;

        public bool Loading
        {
            set => loadingObject.SetActive(value);
        }

        public (CostType type, long cost) GetCostParam => (_type, _cost);

        protected override void Awake()
        {
            base.Awake();
            loadingButton.onClick.AddListener(() => OnClickDisabledSubject.OnNext(Unit.Default));
        }

        public void SetCost(CostType type, long cost)
        {
            if (cost > 0)
            {
                _type = type;
                _cost = cost;
            }
            UpdateObjects();
        }

        public override void UpdateObjects()
        {
            base.UpdateObjects();

            var exist = _cost > 0;
            foreach (var costObject in costObjects)
            {
                costObject.parent.SetActive(exist);
            }

            if (!exist)
            {
                return;
            }

            var costEnough = CheckCostOfType(_type, _cost);
            foreach (var costObject in costObjects)
            {
                costObject.icon.sprite = costIconData.GetIcon(_type);
                costObject.text.text = _cost.ToString();
                costObject.text.color = costEnough ?
                    Palette.GetColor(ColorType.ButtonEnabled) :
                    Palette.GetColor(ColorType.TextDenial);
            }
        }

        public static bool CheckCostOfType(CostType type, long cost)
        {
            Nekoyume.Model.Item.Inventory inventory;

            switch (type)
            {
                case CostType.NCG:
                    return States.Instance.GoldBalanceState.Gold.MajorUnit >= cost;
                case CostType.Crystal:
                    return States.Instance.CrystalBalance.MajorUnit >= cost;
                case CostType.ActionPoint:
                    return ReactiveAvatarState.ActionPoint >= cost;
                case CostType.Hourglass:
                    inventory = States.Instance.CurrentAvatarState.inventory;
                    var count = Util.GetHourglassCount(inventory, Game.Game.instance.Agent.BlockIndex);
                    return count >= cost;
                case CostType.ArenaTicket:
                    return RxProps.ArenaTicketsProgress.Value.currentTickets >= cost;
                case CostType.EventDungeonTicket:
                    return RxProps.EventDungeonTicketProgress.Value.currentTickets >= cost;
                // For material costs
                case CostType.SilverDust:
                case CostType.GoldDust:
                case CostType.RubyDust:
                    inventory = States.Instance.CurrentAvatarState.inventory;
                    var materialCount = inventory.GetMaterialCount((int)type);
                    return materialCount >= cost;
                default:
                    return true;
            }
        }

        protected override bool CheckCondition()
        {
            return CheckCostOfType(_type, _cost) && base.CheckCondition();
        }

        public override void SetState(State state)
        {
            base.SetState(state);
            disabledObject.SetActive(CurrentState.Value == State.Disabled);
        }

        // It works like SetCost() and UpdateObjects().
        // But, this method update *Only UI*.
        // and, Not recommend to use it.
        public void SetFakeUI(CostType type, long cost)
        {
            base.UpdateObjects();

            foreach (var costObject in costObjects)
            {
                costObject.parent.SetActive(true);
            }

            var costEnough = CheckCostOfType(type, cost);
            foreach (var costObject in costObjects)
            {
                costObject.icon.sprite = costIconData.GetIcon(type);
                costObject.text.text = cost.ToString();
                costObject.text.color = costEnough ?
                    Palette.GetColor(ColorType.ButtonEnabled) :
                    Palette.GetColor(ColorType.TextDenial);
            }
        }
    }
}
