using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(Button))]
    public class OpenUrlButton : MonoBehaviour
    {
        [SerializeField]
        private string url;

        private void Awake()
        {
            GetComponent<Button>().OnClickAsObservable().Subscribe(_ =>
            {
                if (string.IsNullOrEmpty(url))
                {
                    return;
                }

                Application.OpenURL(url);
            }).AddTo(gameObject);
        }
    }
}
