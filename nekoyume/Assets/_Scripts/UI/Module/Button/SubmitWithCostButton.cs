using System;
using Nekoyume.Game.Controller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class SubmitWithCostButton : MonoBehaviour
    {
        public Button button;
        public GameObject costNCG;
        public TextMeshProUGUI costNCGText;
        public GameObject costAP;
        public TextMeshProUGUI costAPText;
        public TextMeshProUGUI submitText;
        public GameObject rightSpacer;
        
        public readonly Subject<SubmitWithCostButton> OnSubmitClick = new Subject<SubmitWithCostButton>();

        private void Awake()
        {
            button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnSubmitClick.OnNext(this);
            }).AddTo(gameObject);
        }

        public void ShowNCG(decimal ncg, bool isEnough)
        {
            costNCG.SetActive(true);
            costNCGText.text = ncg.ToString();
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
