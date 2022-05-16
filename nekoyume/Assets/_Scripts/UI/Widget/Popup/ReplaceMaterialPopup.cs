using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Module;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class ReplaceMaterialPopup : TwoButtonSystem
    {
        [SerializeField]
        private List<CostItemView> itemViews = null;

        [SerializeField]
        private TextMeshProUGUI costText = null;

        public void Show(List<(int materialId, int count)> materials,
            System.Action confirmCallback)
        {
            for (int i = 0; i < itemViews.Count; ++i)
            {
                var itemView = itemViews[i];
                if (i >= materials.Count)
                {
                    itemView.gameObject.SetActive(false);
                    continue;
                }

                var (materialId, count) = materials[i];
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

            var cost = CrystalCalculator.CalculateMaterialCost(materials,
                Game.Game.instance.TableSheets.CrystalMaterialCostSheet).MajorUnit;
            costText.text = cost.ToString();

            var currencyText = L10nManager.Localize("UI_CRYSTAL");
            var usageText = L10nManager.Localize("UI_CRYSTAL_REPLACE_MATERIAL");
            var content = L10nManager.Localize("UI_CONFIRM_PAYMENT_CURRENCY_FORMAT",
                cost, currencyText, usageText);
            var yes = L10nManager.Localize("UI_YES");
            var no = L10nManager.Localize("UI_NO");
            confirmCallback = cost <= States.Instance.CrystalBalance.MajorUnit ?
                confirmCallback :
                () => OnInsufficientCost(cost);

            Show(content, yes, no, confirmCallback);
        }

        private void OnInsufficientCost(BigInteger cost)
        {
            var message = L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL");
            Find<PaymentPopup>().ShowAttract(cost, message, () =>
            {
                Find<Craft>().Close(true);
                Find<Grind>().Show();
            });
        }
    }
}
