using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.TableData.Summon;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class SummonCostButton : SimpleCostButton
    {
        public void Subscribe(GameObject addTo)
        {
            OnClickDisabledSubject.Subscribe(_ =>
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("NOTIFICATION_SUMMONING"),
                    NotificationCell.NotificationType.Information)
            ).AddTo(gameObject);

            LoadingHelper.Summon.Subscribe(tuple =>
            {
                var summoning = tuple != null;
                Interactable = !summoning;

                var loading = false;
                if (summoning)
                {
                    // it will have to fix - rune has same material with aura
                    var (material, totalCost) = tuple;
                    var cost = GetCostParam;
                    loading = material == (int)cost.type && totalCost == cost.cost;
                }
                else
                {
                    UpdateObjects();
                }

                Loading = loading;
            }).AddTo(addTo);
        }

        public void Subscribe(
            SummonSheet.Row summonRow,
            int summonCount,
            System.Action goToMarget,
            List<IDisposable> disposables)
        {
            var costType = (CostType)summonRow.CostMaterial;
            var cost = summonRow.CostMaterialCount * summonCount;

            Interactable = LoadingHelper.Summon.Value == null;
            SetCost(costType, cost);
            OnClickSubject.Subscribe(state =>
            {
                switch (state)
                {
                    case State.Normal:
                        Widget.Find<Summon>().SummonAction(summonRow.GroupId, summonCount);
                        break;
                    case State.Conditional:
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
                        Widget.Find<PaymentPopup>().ShowAttract(
                            costType,
                            cost.ToString(),
                            L10nManager.Localize("UI_SUMMON_MATERIAL_NOT_ENOUGH"),
                            L10nManager.Localize("UI_SHOP"),
                            goToMarget);
#else
                        OneLineSystem.Push(
                            MailType.System,
                            L10nManager.Localize("NOTIFICATION_MATERIAL_NOT_ENOUGH"),
                            NotificationCell.NotificationType.Information);
#endif
                        break;
                }
            }).AddTo(disposables);
        }
    }
}
