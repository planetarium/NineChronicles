using System.Globalization;
using System.Numerics;
using Libplanet.Assets;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class SubmitWithCostButton : SubmitButton
    {
        [SerializeField] private Image costBackgroundImage;

        [SerializeField] private Image costBackgroundImageForSubmittable;

        [SerializeField] private GameObject costs;

        [SerializeField] private GameObject costNCG;

        [SerializeField] private Image costNCGImage;

        [SerializeField] private Image costNCGImageForSubmittable;

        [SerializeField] private TextMeshProUGUI costNCGText;

        [SerializeField] private TextMeshProUGUI costNCGTextForSubmittable;

        [SerializeField] private GameObject costAP;

        [SerializeField] private Image costAPImage;

        [SerializeField] private Image costAPImageForSubmittable;

        [SerializeField] private TextMeshProUGUI costAPText;

        [SerializeField] private TextMeshProUGUI costAPTextForSubmittable;

        [SerializeField] private GameObject costHourglass;

        [SerializeField] private Image costHourglassImage;

        [SerializeField] private Image costHourGlassImageForSubmittable;

        [SerializeField] private TextMeshProUGUI costHourglassText;

        [SerializeField] private TextMeshProUGUI costHourglassTextForSubmittable;

        [SerializeField] private HorizontalLayoutGroup layoutGroup;

        public void ShowNCG(BigInteger ncg, bool isEnough)
        {
            costNCG.SetActive(true);
            SetText(costNCGText, costNCGTextForSubmittable, isEnough, ncg);
            UpdateSpace();
        }

        public void ShowNCG(FungibleAssetValue ncg, bool isEnough)
        {
            costNCG.SetActive(true);
            SetText(costNCGText, costNCGTextForSubmittable, isEnough, ncg.GetQuantityString());
            UpdateSpace();
        }

        public void HideNCG()
        {
            costNCG.SetActive(false);
            UpdateSpace();
        }

        public void ShowAP(int ap, bool isEnough)
        {
            costAP.SetActive(true);
            SetText(costAPText, costAPTextForSubmittable, isEnough, ap);
            UpdateSpace();
        }

        public void HideAP()
        {
            costAP.SetActive(false);
            UpdateSpace();
        }

        public void ShowHourglass(int count, bool isEnough)
        {
            costHourglass.SetActive(true);
            SetText(costHourglassText, costHourglassTextForSubmittable, isEnough, count);
            UpdateSpace();
        }

        public void ShowHourglass(int required, int reserve)
        {
            costHourglass.SetActive(true);
            SetText(costHourglassText, costHourglassTextForSubmittable, required <= reserve, reserve, required);
            UpdateSpace();
        }

        public void HideHourglass()
        {
            costHourglass.SetActive(false);
            UpdateSpace();
        }

        public override void SetSubmittable(bool submittable)
        {
            base.SetSubmittable(submittable);
            costNCGText.gameObject.SetActive(!submittable);
            costNCGTextForSubmittable.gameObject.SetActive(submittable);
            costAPText.gameObject.SetActive(!submittable);
            costAPTextForSubmittable.gameObject.SetActive(submittable);
            costHourglassText.gameObject.SetActive(!submittable);
            costHourglassTextForSubmittable.gameObject.SetActive(submittable);

            costBackgroundImage.enabled = !submittable;
            costAPImage.enabled = !submittable;
            costNCGImage.enabled = !submittable;
            costHourglassImage.enabled = !submittable;

            costBackgroundImageForSubmittable.enabled = submittable;
            costAPImageForSubmittable.enabled = submittable;
            costNCGImageForSubmittable.enabled = submittable;
            costHourGlassImageForSubmittable.enabled = submittable;

            UpdateSpace();
        }

        private void UpdateSpace()
        {
            var hasNoCost = !costAP.activeSelf && !costNCG.activeSelf && !costHourglass.activeSelf;

            costs.SetActive(!hasNoCost);
            layoutGroup.childAlignment = hasNoCost ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            layoutGroup.spacing = costAP.activeSelf ^ costNCG.activeSelf ^ costHourglass.activeSelf ? 15 : 5;
        }

        private static void SetText(
            TMP_Text textField,
            TMP_Text submitField,
            bool isEnough,
            int cost) => SetText(textField, submitField, isEnough, cost.ToString(CultureInfo.InvariantCulture));

        private static void SetText(
            TMP_Text textField,
            TMP_Text submitField,
            bool isEnough,
            BigInteger cost) => SetText(textField, submitField, isEnough, cost.ToString(CultureInfo.InvariantCulture));

        private static void SetText(
            TMP_Text textField,
            TMP_Text submitField,
            bool isEnough,
            string cost)
        {
            textField.text = cost;
            submitField.text = cost;
            SetTextColor(textField, submitField, isEnough);
        }

        private static void SetText(
            TMP_Text textField,
            TMP_Text submitField,
            bool isEnough,
            int cost,
            int reserve) => SetText(
            textField,
            submitField,
            isEnough,
            cost.ToString(CultureInfo.InvariantCulture),
            reserve.ToString(CultureInfo.InvariantCulture));

        private static void SetText(
            TMP_Text textField,
            TMP_Text submitField,
            bool isEnough,
            string cost,
            string reserve)
        {
            textField.text =
                isEnough ? $"{cost}/{reserve}" : $"<color=#ff00005a>{cost}</color>/{reserve}";
            submitField.text = textField.text;
        }

        private static void SetTextColor(Graphic textField, Graphic submitField, bool isEnough)
        {
            textField.color = isEnough ? Palette.GetColor(ColorType.ButtonEnabled) : Palette.GetColor(ColorType.ButtonDisabled);
            submitField.color = textField.color;
        }
    }
}
