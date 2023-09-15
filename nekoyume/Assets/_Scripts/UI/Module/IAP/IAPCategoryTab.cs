using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nekoyume.UI.Module
{
    public class IAPCategoryTab : MonoBehaviour
    {
        [SerializeField]
        private Image[] icons;

        [SerializeField]
        private TextMeshProUGUI[] tabNames;

        public void SetData(string name, Sprite icon = null)
        {
            foreach (var item in tabNames)
            {
                item.text = name;
            }

            foreach (var item in icons)
            {
                item.sprite = icon;
            }
        }
    }
}
