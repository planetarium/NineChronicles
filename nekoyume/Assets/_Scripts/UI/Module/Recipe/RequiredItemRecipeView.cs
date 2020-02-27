using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class RequiredItemRecipeView : MonoBehaviour
    {
        [SerializeField]
        private RequiredItemView[] requiredItemViews = null;

        [SerializeField]
        private Image plusImage = null;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(int recipe)
        {
            Show();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
