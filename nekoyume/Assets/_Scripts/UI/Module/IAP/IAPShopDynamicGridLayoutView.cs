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

        public void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            var rectTrans = GetComponent<RectTransform>();
            rectTrans.pivot = new Vector2(0, 1);

            
            List<RectTransform> children = new List<RectTransform>();

            for (int i = 0; i < transform.childCount; i++)
            {
                if(transform.GetChild(i).gameObject.activeSelf)
                    children.Add(transform.GetChild(i).GetComponent<RectTransform>());
            }
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

                if ((lastPos.y - children[i].rect.height) < minHeight && i + 1 < transform.childCount)
                {
                    minHeight = (lastPos.y - children[i].rect.height);
                }
            }

            rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, Mathf.Abs(minHeight) + 10);
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
