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
            var inventory = States.Instance.CurrentAvatarState.inventory;

            switch (type)
            {
                case CostType.NCG:
                    return States.Instance.AgentNCG.MajorUnit >= cost;
                case CostType.Crystal:
                    return States.Instance.AgentCrystal.MajorUnit >= cost;
                case CostType.ActionPoint:
                    return States.Instance.CurrentAvatarState.actionPoint >= cost;
                case CostType.Hourglass:
                    var count = Util.GetHourglassCount(inventory, Game.Game.instance.Agent.BlockIndex);
                    return count >= cost;
                case CostType.ArenaTicket:
                    return RxProps.ArenaTicketsProgress.Value.currentTickets >= cost;
                case CostType.EventDungeonTicket:
                    return RxProps.EventDungeonTicketProgress.Value.currentTickets >= cost;
                // For material costs
                case CostType.SilverDust:
                case CostType.GoldDust:
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
    }
}
