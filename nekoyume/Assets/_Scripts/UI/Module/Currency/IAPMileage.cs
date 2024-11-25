using Nekoyume.ApiClient;
using TMPro;
using System;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;
    public class IAPMileage : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI amountText;

        [SerializeField]
        private GameObject loadingObject;

        private void Awake()
        {
            ApiClients.Instance.IAPServiceManager.CurrentMileage.Subscribe(mileage =>
            {
                amountText.text = mileage.ToString("N0", System.Globalization.CultureInfo.CurrentCulture);
            });
        }

        public void ShowMaterialNavigationPopup()
        {
            Widget.Find<MaterialNavigationPopup>().ShowIAPMileage();
        }
    }
}
