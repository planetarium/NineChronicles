using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using System.Numerics;
using UnityEngine;

namespace Nekoyume.UI
{
    using Nekoyume.BlockChain;
    using UniRx;

    public class BuffBonusPopup : PopupWidget
    {
        [SerializeField]
        private ConditionalCostButton normalButton = null;

        [SerializeField]
        private ConditionalCostButton advancedButton = null;

        private BigInteger _normalCost;

        private BigInteger _advancedCost;

        private System.Action _onAttract;

        protected override void Awake()
        {
            _onAttract = () =>
            {
                Close(true);
                Find<Grind>().Show();
            };

            normalButton.OnClickDisabledSubject
                .Subscribe(OnInsufficientStar)
                .AddTo(gameObject);
            advancedButton.OnClickDisabledSubject
                .Subscribe(OnInsufficientStar)
                .AddTo(gameObject);
        }

        public void Show(int stageId, bool hasEnoughStars)
        {
            var sheet = Game.Game.instance.TableSheets.CrystalStageBuffGachaSheet;
            _normalCost = CrystalCalculator.CalculateBuffGachaCost(stageId, false, sheet).MajorUnit;
            _advancedCost = CrystalCalculator.CalculateBuffGachaCost(stageId, true, sheet).MajorUnit;
            normalButton.SetCost(CostType.Crystal, (long) _normalCost);
            advancedButton.SetCost(CostType.Crystal, (long) _advancedCost);
            normalButton.Interactable = hasEnoughStars;
            advancedButton.Interactable = hasEnoughStars;
            base.Show();
        }

        private bool CheckCost(BigInteger cost)
        {
            if (States.Instance.CrystalBalance.MajorUnit < cost)
            {
                var message = L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL");
                Find<PaymentPopup>().ShowAttract(_normalCost, message, _onAttract);
                return false;
            }
            return true;
        }

        private void OnClickNormalButton()
        {
            if (CheckCost(_normalCost))
            {
                var usageMessage = L10nManager.Localize("UI_NORMAL_BUFF_GACHA");
                var balance = States.Instance.CrystalBalance;
                var content = balance.GetPaymentFormatText(usageMessage, _normalCost);

                Find<PaymentPopup>().Show(
                    CostType.Crystal,
                    balance.MajorUnit,
                    _normalCost,
                    content,
                    L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL"),
                    () => PushAction(false),
                    _onAttract);
            }
        }

        private void OnClickAdvancedButton()
        {
            if (CheckCost(_advancedCost))
            {
                var usageMessage = L10nManager.Localize("UI_ADVANCED_BUFF_GACHA");
                var balance = States.Instance.CrystalBalance;
                var content = balance.GetPaymentFormatText(usageMessage, _advancedCost);

                Find<PaymentPopup>().Show(
                    CostType.Crystal,
                    balance.MajorUnit,
                    _advancedCost,
                    content,
                    L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL"),
                    () => PushAction(true),
                    _onAttract);
            }
        }

        private void OnInsufficientStar(Unit unit)
        {
            OneLineSystem.Push(
                MailType.System,
                L10nManager.Localize("UI_HAS_BUFF_NOT_ENOUGH_STAR"),
                NotificationCell.NotificationType.Alert);
        }

        private void PushAction(bool advanced)
        {
            Find<BuffBonusLoadingScreen>().Show();
            
            ActionManager.Instance.HackAndSlashRandomBuff(advanced);
        }
    }
}
