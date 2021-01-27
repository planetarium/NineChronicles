using UnityEngine;

namespace Nekoyume.UI
{
    public class GuideDialog : MonoBehaviour
    {
        [SerializeField] private Transform topContainer;
        [SerializeField] private Transform bottomContainer;

        public void Play(GuideDialogData data)
        {
            transform.SetParent(data.TargetHeight > 0 ? topContainer : bottomContainer);
            data.Callback?.Invoke();
        }

        public void Stop()
        {
        }
    }
}
