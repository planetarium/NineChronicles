using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class IAPShopProductCellView : MonoBehaviour
    {
        private RectTransform _rect;
        private ProductSchema _data;

        public void SetData(ProductSchema data)
        {
            _data = data;
            _rect = GetComponent<RectTransform>();
        }
    }
}
