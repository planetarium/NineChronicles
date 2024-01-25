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

        [SerializeField]
        private GameObject[] notificationObjs;

        private string _nameKey;

        private void Awake()
        {
            L10nManager.OnLanguageChange.Subscribe(_ =>
            {
                RefreshLocalized();
            }).AddTo(gameObject);
        }

        public void SetData(string nameKey, Sprite icon = null)
        {
            _nameKey = nameKey;
            RefreshLocalized();

            foreach (var item in icons)
            {
                item.sprite = icon;
            }
        }

        public void SetNoti(bool isNoti)
        {
            foreach (var item in notificationObjs)
            {
                item.SetActive(isNoti);
            }
        }

        private void RefreshLocalized()
        {
            foreach (var item in tabNames)
            {
                item.text = L10nManager.Localize(_nameKey);
            }
        }
    }
}
