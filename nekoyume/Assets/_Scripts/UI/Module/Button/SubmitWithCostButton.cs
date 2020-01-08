using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class SubmitWithCostButton : SubmitButton
    {
        public GameObject costNCG;
        public TextMeshProUGUI costNCGText;
        public TextMeshProUGUI costNCGTextForSubmittable;
        public GameObject costAP;
        public TextMeshProUGUI costAPText;
        public TextMeshProUGUI costAPTextForSubmittable;
        public GameObject rightSpacer;

        public void ShowNCG(decimal ncg, bool isEnough)
        {
            costNCG.SetActive(true);
            costNCGText.text = ncg.ToString();
            costNCGTextForSubmittable.text = costNCGText.text;
            costNCGText.color = isEnough ? Color.white : Color.red;
            costNCGTextForSubmittable.color = costNCGText.color; 
            UpdateRightSpacer();
        }

        public void HideNCG()
        {
            costNCG.SetActive(false);
            UpdateRightSpacer();
        }
        
        public void ShowAP(int ap, bool isEnough)
        {
            costAP.SetActive(true);
            costAPText.text = ap.ToString();
            costAPTextForSubmittable.text = costAPText.text;
            costAPText.color = isEnough ? Color.white : Color.red;
            costAPTextForSubmittable.color = costAPText.color;
            UpdateRightSpacer();
        }

        public void HideAP()
        {
            costAP.SetActive(false);
            UpdateRightSpacer();
        }

        public override void SetSubmittable(bool submittable)
        {
            base.SetSubmittable(submittable);
            costNCGText.gameObject.SetActive(!submittable);
            costNCGTextForSubmittable.gameObject.SetActive(submittable);
            costAPText.gameObject.SetActive(!submittable);
            costAPTextForSubmittable.gameObject.SetActive(submittable);
        }

        private void UpdateRightSpacer()
        {
            rightSpacer.SetActive(!costNCG.activeSelf && !costAP.activeSelf);
        }
    }
}
