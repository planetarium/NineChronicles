using System.Globalization;
using System.Numerics;
using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class SubmitWithCostButton : SubmitButton
    {
        [SerializeField]
        private Image costBackgroundImage = null;

        [SerializeField]
        private Image costBackgroundImageForSubmittable = null;

        [SerializeField]
        private GameObject costs = null;

        [SerializeField]
        private GameObject costNCG = null;

        [SerializeField]
        private Image costNCGImage = null;

        [SerializeField]
        private Image costNCGImageForSubmittable = null;

        [SerializeField]
        private TextMeshProUGUI costNCGText = null;

        [SerializeField]
        private TextMeshProUGUI costNCGTextForSubmittable = null;

        [SerializeField]
        private GameObject costAP = null;

        [SerializeField]
        private Image costAPImage = null;

        [SerializeField]
        private Image costAPImageForSubmittable = null;

        [SerializeField]
        private TextMeshProUGUI costAPText = null;

        [SerializeField]
        private TextMeshProUGUI costAPTextForSubmittable = null;

        [SerializeField]
        private GameObject costHourglass = null;

        [SerializeField]
        private Image costHourglassImage = null;

        [SerializeField]
        private Image costHourGlassImageForSubmittable = null;

        [SerializeField]
        private TextMeshProUGUI costHourglassText = null;

        [SerializeField]
        private TextMeshProUGUI costHourglassTextForSubmittable = null;

        [SerializeField]
        private HorizontalLayoutGroup layoutGroup = null;

        public void ShowNCG(BigInteger ncg, bool isEnough)
        {
            costNCG.SetActive(true);
            SetText(costNCGText, costNCGTextForSubmittable, isEnough, ncg);
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

        private static void SetText(TextMeshProUGUI textField, TextMeshProUGUI submitField, bool isEnough, int cost) =>
            SetText(textField, submitField, isEnough, (BigInteger)cost);

        private static void SetText(TextMeshProUGUI textField, TextMeshProUGUI submitField, bool isEnough, BigInteger cost)
        {
            textField.text = cost.ToString(CultureInfo.InvariantCulture);
            submitField.text = textField.text;
            SetTextColor(textField, submitField, isEnough);
        }

        private static void SetText(TextMeshProUGUI textField, TextMeshProUGUI submitField, bool isEnough, int cost, int reserve) =>
            SetText(textField, submitField, isEnough, (BigInteger)cost, (BigInteger)reserve);

        private static void SetText(TextMeshProUGUI textField, TextMeshProUGUI submitField, bool isEnough, BigInteger cost, BigInteger reserve)
        {
            var reserveText = reserve.ToString(CultureInfo.InvariantCulture);
            var costText = cost.ToString(CultureInfo.InvariantCulture);

            textField.text = isEnough ?
                $"{costText}/{reserveText}" :
                $"<color=#ff00005a>{costText}</color>/{reserveText}";
            submitField.text = textField.text;
        }

        private static void SetTextColor(TextMeshProUGUI textField, TextMeshProUGUI submitField, bool isEnough)
        {
            textField.color = isEnough ? Palette.GetColor(0) : Palette.GetColor(3);
            submitField.color = textField.color;
        }
    }
}
