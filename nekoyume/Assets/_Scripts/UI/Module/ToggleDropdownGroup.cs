using System.Linq;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class ToggleDropdownGroup : MonoBehaviour
    {
        private void OnEnable()
        {
            var children = transform.GetComponentsInChildren<ToggleDropdown>();
            float childHeight = children.First().GetComponent<RectTransform>().sizeDelta.y;

            int itemCount = 0;
            float itemHeight = 0;
            foreach (var child in children)
            {
                if (child.items.Count > 0)
                {
                    itemHeight = child.items.First().GetComponent<RectTransform>().sizeDelta.y;
                }
                itemCount = Mathf.Max(child.items.Count, itemCount);
            }

            GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x,
                (childHeight * children.Length) + (itemHeight * itemCount));

            children.First().isOn = true; // <-- 재수정해야됨
        }
    }
}
