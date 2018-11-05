using System.Collections;
using UnityEngine;


namespace Nekoyume.UI
{
    public class MainCanvas : MonoBehaviour
    {
        private IEnumerator Start()
        {
            Login loginWidget = Widget.Create<Login>();
            yield return loginWidget.WaitForShow();
        }
    }
}
