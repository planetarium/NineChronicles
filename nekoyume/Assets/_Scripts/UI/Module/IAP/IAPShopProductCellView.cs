using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class IAPShopProductCellView : MonoBehaviour
    {
        public RectTransform recT;

        private ProductSchema _data;

        public void SetData(ProductSchema data)
        {
            _data = data;
            recT = GetComponent<RectTransform>();
        }
    }
}
