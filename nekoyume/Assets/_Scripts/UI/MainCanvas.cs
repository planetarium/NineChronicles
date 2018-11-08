using System.Collections;
using UnityEngine;


namespace Nekoyume.UI
{
    public class MainCanvas : MonoBehaviour
    {
        private void Awake()
        {
            GameObject widgetContainer = new GameObject("Widget");
            widgetContainer.transform.parent = transform;
            GameObject popupContainer = new GameObject("Popup");
            popupContainer.transform.parent = transform;
        }

        private void Start()
        {
            Login loginWidget = Widget.Create<Login>();
            loginWidget.Show();
        }
    }
}
