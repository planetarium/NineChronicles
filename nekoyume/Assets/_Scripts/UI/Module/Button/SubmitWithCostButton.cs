using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class SubmitWithCostButton : SubmitButton
    {
        public Image costBackgroundImage;
        public Image costBackgroundImageForSubmittable;
        public GameObject costs;
        public GameObject costNCG;
        public Image costNCGImage;
        public Image costNCGImageForSubmittable;
        public TextMeshProUGUI costNCGText;
        public TextMeshProUGUI costNCGTextForSubmittable;
        public GameObject costAP;
        public Image costAPImage;
        public Image costAPImageForSubmittable;
        public TextMeshProUGUI costAPText;
        public TextMeshProUGUI costAPTextForSubmittable;
        public GameObject costHourglass;
        public Image costHourglassImage;
        public Image costHourGlassImageForSubmittable;
        public TextMeshProUGUI costHourglassText;
        public TextMeshProUGUI costHourglassTextForSubmittable;
        public HorizontalLayoutGroup layoutGroup;

        public void ShowNCG(decimal ncg, bool isEnough)
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
            bool hasNoCost = !costAP.activeSelf && !costNCG.activeSelf && !costHourglass.activeSelf;

            costs.SetActive(!hasNoCost);
            layoutGroup.childAlignment = hasNoCost ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            layoutGroup.spacing = costAP.activeSelf ^ costNCG.activeSelf ^ costHourglass.activeSelf ? 15 : 5;
        }

        private static void SetText(TextMeshProUGUI textField, TextMeshProUGUI submitField, bool isEnough, int cost)
        {
            textField.text = cost.ToString(CultureInfo.InvariantCulture);
            submitField.text = textField.text;
            SetTextColor(textField, submitField, isEnough);
        }

        private static void SetText(TextMeshProUGUI textField, TextMeshProUGUI submitField, bool isEnough, decimal cost)
        {
            textField.text = cost.ToString(CultureInfo.InvariantCulture);
            submitField.text = textField.text;
            SetTextColor(textField, submitField, isEnough);
        }

        private static void SetTextColor(TextMeshProUGUI textField, TextMeshProUGUI submitField, bool isEnough)
        {
            textField.color = isEnough ? Color.white : Color.red;
            submitField.color = textField.color;
        }
    }
}
