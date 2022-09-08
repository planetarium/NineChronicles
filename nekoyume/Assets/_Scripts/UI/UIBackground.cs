using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class UIBackground : MonoBehaviour
    {
        [SerializeField]
        private TouchHandler touchHandler;

        public System.Action OnClick { get; set; }

        private void Awake()
        {
            touchHandler.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnClick?.Invoke();
            }).AddTo(this);
        }
    }
}
