using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Module;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using TMPro;
using UnityEngine;
using System.Collections.Immutable;
using System;

namespace Nekoyume.UI
{
    public class ReplaceMaterialPopup : TwoButtonSystem
    {
        [SerializeField] private List<CostItemView> itemViews = null;

        [SerializeField] private TextMeshProUGUI costText = null;

        public void Show(Dictionary<int, int> materials,
            System.Action confirmCallback)
        {
            foreach (var view in itemViews)
            {
                view.gameObject.SetActive(false);
            }

            foreach (var pair in materials)
            {
                var itemView = itemViews.FirstOrDefault(x => !x.gameObject.activeSelf);
                if (itemView == null)
                {
                    Debug.LogError("ItemView is already full.");
                    continue;
                }

                var materialId = pair.Key;
                var count = pair.Value;
                if (!Game.Game.instance.TableSheets.MaterialItemSheet.TryGetValue(materialId, out var itemRow))
                {
                    itemView.gameObject.SetActive(false);
                    continue;
                }

                if (!Game.Game.instance.TableSheets.CrystalMaterialCostSheet.TryGetValue(materialId, out var costRow))
                {
                    itemView.gameObject.SetActive(false);
                    continue;
                }

                itemView.SetData(itemRow, CostType.Crystal, count, costRow.CRYSTAL);
                itemView.gameObject.SetActive(true);
            }

            BigInteger cost = 0;
            var hasUnreplaceableMaterial = false;
            foreach (var pair in materials)
            {
                try
                {
                    cost += CrystalCalculator.CalculateMaterialCost(
                        pair.Key, pair.Value, Game.Game.instance.TableSheets.CrystalMaterialCostSheet).MajorUnit;
                }
                catch (ArgumentException)
                {
                    hasUnreplaceableMaterial = true;
                    continue;
                }
            }

            costText.text = cost.ToString();

            var currencyText = L10nManager.Localize("UI_CRYSTAL");
            var usageText = L10nManager.Localize("UI_CRYSTAL_REPLACE_MATERIAL");
            var content = L10nManager.Localize("UI_CONFIRM_PAYMENT_CURRENCY_FORMAT",
                cost, currencyText, usageText);
            var yes = L10nManager.Localize("UI_YES");
            var no = L10nManager.Localize("UI_NO");

            if (hasUnreplaceableMaterial)
            {
                confirmCallback = () => OnInsufficientCost(cost);
            }
            else
            {
                confirmCallback = cost <= States.Instance.CrystalBalance.MajorUnit
                    ? confirmCallback
                    : () => OnInsufficientCost(cost);
            }

            Show(content, yes, no, confirmCallback);
        }

        private void OnInsufficientCost(BigInteger cost)
        {
            var message = L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL");
            Find<PaymentPopup>().ShowAttract(
                CostType.Crystal,
                cost,
                message,
                L10nManager.Localize("UI_GO_GRINDING"),
                () =>
                {
                    Find<Craft>().Close(true);
                    Find<Grind>().Show();
                });
        }
    }
}
