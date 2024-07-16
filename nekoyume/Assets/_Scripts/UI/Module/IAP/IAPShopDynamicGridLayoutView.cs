using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class IAPShopDynamicGridLayoutView : MonoBehaviour
    {
        [SerializeField]
        private float space;

        public void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            var rectTrans = GetComponent<RectTransform>();
            rectTrans.pivot = new Vector2(0, 1);

            var sortbyProductOrderList = new List<IAPShopProductCellView>();

            for (var i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject.activeSelf)
                {
                    sortbyProductOrderList.Add(transform.GetChild(i).GetComponent<IAPShopProductCellView>());
                }
            }

            sortbyProductOrderList.Sort(Compare);

            var children = new List<RectTransform>();
            foreach (var item in sortbyProductOrderList)
            {
                children.Add(item.GetComponent<RectTransform>());
            }

            var lastPos = new Vector2(space, -space);
            var minHeight = 0f;
            for (var i = 0; i < children.Count; i++)
            {
                children[i].pivot = new Vector2(0, 1);
                children[i].anchorMin = new Vector2(0, 1);
                children[i].anchorMax = new Vector2(0, 1);

                children[i].SetSiblingIndex(i);

                children[i].localPosition = lastPos;
                if (lastPos.x + children[i].rect.width + space > rectTrans.rect.width ||
                    lastPos.x + children[i].rect.width + space + children[Mathf.Min(i + 1, children.Count - 1)].rect.width + space > rectTrans.rect.width)
                {
                    lastPos.x = space;
                    lastPos.y -= children[i].rect.height + space;

                    for (var z = i; z >= 0; z--)
                    {
                        if (children[z].anchoredPosition.y - children[z].rect.height < lastPos.y)
                        {
                            lastPos.x += children[z].rect.width + space;
                        }
                    }
                }
                else
                {
                    lastPos.x += children[i].rect.width + space;
                }

                if (lastPos.y - children[i].rect.height < minHeight && i + 1 < children.Count)
                {
                    minHeight = lastPos.y - children[i].rect.height;
                }
            }

            rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, Mathf.Abs(minHeight) + space);
        }

        private static int Compare(IAPShopProductCellView lhs, IAPShopProductCellView rhs)
        {
            if (lhs == rhs)
            {
                return 0;
            }

            if (lhs.GetOrder() > rhs.GetOrder())
            {
                return 1;
            }

            if (lhs.GetOrder() < rhs.GetOrder())
            {
                return -1;
            }

            return 0;
        }
    }
}
