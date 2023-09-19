using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.L10n;

namespace Nekoyume.UI.Module
{
    public class IAPCategoryTab : MonoBehaviour
    {
        [SerializeField]
        private Image[] icons;

        [SerializeField]
        private TextMeshProUGUI[] tabNames;

        private string _name;

        private void Awake()
        {
            L10nManager.OnLanguageChange.Subscribe(_ =>
            {
                RefreshLocalized();
            }).AddTo(gameObject);
        }

        public void SetData(string name, Sprite icon = null)
        {
            _name = name;
            RefreshLocalized();

            foreach (var item in icons)
            {
                item.sprite = icon;
            }
        }

        private void RefreshLocalized()
        {
            foreach (var item in tabNames)
            {
                item.text = L10nManager.Localize(_name);
            }
        }
    }
}
