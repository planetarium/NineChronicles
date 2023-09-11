using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;

namespace Nekoyume.UI.Module
{
    public class IAPShopDynamicGridLayoutView : MonoBehaviour
    {
        [SerializeField]
        private float space;

        [SerializeField]
        private IAPShopProductCellView cellViewOrigin;

        private List<IAPShopProductCellView> _childProductList = new List<IAPShopProductCellView>();
        private Dictionary<string, IAPShopProductCellView> _childProductDic = new Dictionary<string, IAPShopProductCellView>();

        public void OnEnable()
        {
            Refresh();
        }

        public void SetSortOrder()
        {

        }

        public void AddProduct(ProductSchema productData)
        {
            if(!_childProductDic.TryGetValue(productData.GoogleSku, out var product))
            {
                product = Instantiate(cellViewOrigin, transform);
                _childProductList.Add(product);
                _childProductDic.Add(productData.GoogleSku, product);
            }
            product.SetData(productData);
        }

        public void Refresh()
        {
            var rectTrans = GetComponent<RectTransform>();
            rectTrans.pivot = new Vector2(0, 1);

            var children = GetComponentsInChildren<RectTransform>(false).ToList();
            children.Remove(rectTrans);
            children.Sort(Compare);

            Vector2 lastPos = new Vector2(space, -space);
            float minHeight = 0;
            for (int i = 0; i < children.Count; i++)
            {
                children[i].pivot = new Vector2(0, 1);
                children[i].anchorMin = new Vector2(0, 1);
                children[i].anchorMax = new Vector2(0, 1);

                children[i].SetSiblingIndex(i);

                children[i].localPosition = lastPos;
                if (lastPos.x + children[i].rect.width + space > rectTrans.rect.width ||
                    lastPos.x + children[i].rect.width + space + children[Mathf.Min((i + 1), (children.Count - 1))].rect.width + space > rectTrans.rect.width)
                {
                    lastPos.x = space;
                    lastPos.y -= (children[i].rect.height + space);

                    for (int z = i; z >= 0; z--)
                    {
                        if (children[z].anchoredPosition.y - children[z].rect.height < lastPos.y)
                            lastPos.x += children[z].rect.width + space;
                    }
                }
                else
                {
                    lastPos.x += children[i].rect.width + space;
                }

                if ((lastPos.y - children[i].rect.height) < minHeight)
                {
                    minHeight = (lastPos.y - children[i].rect.height);
                }
            }

            rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, Mathf.Abs(minHeight) + space);
        }

        private static int Compare(RectTransform lhs, RectTransform rhs)
        {
            if (lhs == rhs) return 0;

            var rectLhs = lhs.rect;
            var rectRhs = rhs.rect;

            if (rectLhs.height < rectRhs.height) return 1;
            if (rectLhs.height > rectRhs.height) return -1;
            return 0;
        }
    }
}
