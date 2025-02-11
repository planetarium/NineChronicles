﻿using Nekoyume.EnumType;
using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ColorSelectView : MonoBehaviour
    {
        [SerializeField] private Image plainColor;
        [SerializeField] private Image gradationColor;
        [SerializeField] private Image lightColor;
        [SerializeField] private Button button;
        [SerializeField] private GameObject selected;
        [SerializeField] private GameObject leopardPrint1;
        [SerializeField] private GameObject leopardPrint2;
        [SerializeField] private GameObject leopardPrint3;
        [SerializeField] private GameObject leopardPrint4;
        [SerializeField] private GameObject leopardPrint5;
        [SerializeField] private Image lightColor2;


        public bool Selected
        {
            set => selected.SetActive(value);
        }

        public void Set(ColorSelect itemData, System.Action onClick)
        {
            plainColor.gameObject.SetActive(false);
            gradationColor.gameObject.SetActive(false);
            lightColor.gameObject.SetActive(false);
            leopardPrint1.gameObject.SetActive(false);
            leopardPrint2.gameObject.SetActive(false);
            leopardPrint3.gameObject.SetActive(false);
            leopardPrint4.gameObject.SetActive(false);
            leopardPrint5.gameObject.SetActive(false);
            lightColor2.gameObject.SetActive(false);

            switch (itemData.colorSelectType)
            {
                case ColorSelectType.Plain:
                    plainColor.gameObject.SetActive(true);
                    plainColor.color = itemData.color;
                    break;
                case ColorSelectType.Gradation:
                    gradationColor.gameObject.SetActive(true);
                    gradationColor.color = itemData.color;
                    break;
                case ColorSelectType.Light:
                    lightColor.gameObject.SetActive(true);
                    lightColor.color = itemData.color;
                    break;
                case ColorSelectType.LeopardPrint1:
                    leopardPrint1.gameObject.SetActive(true);
                    break;
                case ColorSelectType.LeopardPrint2:
                    leopardPrint2.gameObject.SetActive(true);
                    break;
                case ColorSelectType.LeopardPrint3:
                    leopardPrint3.gameObject.SetActive(true);
                    break;
                case ColorSelectType.LeopardPrint4:
                    leopardPrint4.gameObject.SetActive(true);
                    break;
                case ColorSelectType.LeopardPrint5:
                    leopardPrint5.gameObject.SetActive(true);
                    break;
                case ColorSelectType.Light2:
                    lightColor2.gameObject.SetActive(true);
                    lightColor2.color = itemData.color;
                    break;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick.Invoke);
        }
    }
}
