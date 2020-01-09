using System.Globalization;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class SubmitWithCostButton : SubmitButton
    {
        public GameObject costNCG;
        public TextMeshProUGUI costNCGText;
        public GameObject costAP;
        public TextMeshProUGUI costAPText;
        public GameObject rightSpacer;

        public void ShowNCG(decimal ncg, bool isEnough)
        {
            costNCG.SetActive(true);
            costNCGText.text = ncg.ToString(CultureInfo.InvariantCulture);
            costNCGText.color = isEnough ? Color.white : Color.red;
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
            costAPText.color = isEnough ? Color.white : Color.red;
            UpdateRightSpacer();
        }

        public void HideAP()
        {
            costAP.SetActive(false);
            UpdateRightSpacer();
        }

        private void UpdateRightSpacer()
        {
            rightSpacer.SetActive(!costNCG.activeSelf && !costAP.activeSelf);
        }
    }
}
