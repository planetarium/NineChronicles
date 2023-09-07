using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using System;
using System.Collections.Generic;
using TMPro;
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
        private bool showCostAlert = true;

        [SerializeField]
        private List<CostObject> costObjects;

        [SerializeField]
        private GameObject loadingObject;

        private CostType _type;
        private long _cost;

        public bool Loading
        {
            set => loadingObject.SetActive(value);
        }

        public bool TryGetCost(CostType type, out long cost)
        {
            if(_type != type)
            {
                cost = 0;
                return false;
            }

            cost = _cost;
            return true;
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

            foreach (var costObject in costObjects)
            {
                costObject.icon.sprite = costIconData.GetIcon(_type);
                costObject.text.text = _cost.ToString();
                costObject.text.color = CheckCostOfType(_type, _cost) ?
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
                    return States.Instance.GoldBalanceState.Gold.MajorUnit >= cost;
                case CostType.Crystal:
                    return States.Instance.CrystalBalance.MajorUnit >= cost;
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

        protected override void OnClickButton()
        {
            base.OnClickButton();

            if (!showCostAlert)
            {
                return;
            }

            if (CheckCostOfType(_type, _cost)) return;

            var messageKey = _type switch
            {
                CostType.NCG => "UI_NOT_ENOUGH_NCG",
                CostType.Crystal => "UI_NOT_ENOUGH_CRYSTAL",
                CostType.ActionPoint => "ERROR_ACTION_POINT",
                CostType.Hourglass => "UI_NOT_ENOUGH_HOURGLASS",
                _ => string.Empty,
            };

            OneLineSystem.Push(
                MailType.System,
                L10nManager.Localize(messageKey),
                NotificationCell.NotificationType.Alert);
        }
    }
}
