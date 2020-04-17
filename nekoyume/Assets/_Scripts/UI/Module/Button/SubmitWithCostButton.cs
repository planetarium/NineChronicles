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
        public HorizontalLayoutGroup layoutGroup;
        public Animator animator;

        public void ShowNCG(decimal ncg, bool isEnough)
        {
            costNCG.SetActive(true);
            costNCGText.text = ncg.ToString();
            costNCGTextForSubmittable.text = costNCGText.text;
            costNCGText.color = isEnough ? Color.white : Color.red;
            costNCGTextForSubmittable.color = costNCGText.color; 
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
            costAPText.text = ap.ToString();
            costAPTextForSubmittable.text = costAPText.text;
            costAPText.color = isEnough ? Color.white : Color.red;
            costAPTextForSubmittable.color = costAPText.color;
            UpdateSpace();
        }

        public void HideAP()
        {
            costAP.SetActive(false);
            UpdateSpace();
        }

        public override void SetSubmittable(bool submittable)
        {
            base.SetSubmittable(submittable);
            costNCGText.gameObject.SetActive(!submittable);
            costNCGTextForSubmittable.gameObject.SetActive(submittable);
            costAPText.gameObject.SetActive(!submittable);
            costAPTextForSubmittable.gameObject.SetActive(submittable);

            costBackgroundImage.enabled = !submittable;
            costAPImage.enabled = !submittable;
            costNCGImage.enabled = !submittable;

            costBackgroundImageForSubmittable.enabled = submittable;
            costAPImageForSubmittable.enabled = submittable;
            costNCGImageForSubmittable.enabled = submittable;

            UpdateSpace();
        }

        private void UpdateSpace()
        {
            bool hasNoCost = !costAP.activeSelf && !costNCG.activeSelf;

            costs.SetActive(!hasNoCost);
            layoutGroup.childAlignment = hasNoCost ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            layoutGroup.spacing = costAP.activeSelf ^ costNCG.activeSelf ? 15 : 5;
        }
    }
}
