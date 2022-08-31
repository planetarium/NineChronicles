using Nekoyume.Game.Character;
using UnityEngine;

namespace Nekoyume.UI.Module.Common
{
    using UniRx;
    public class PositionTooltip : MonoBehaviour
    {
        [SerializeField]
        private TouchHandler touchHandler;
        private void Start()
        {
            touchHandler.OnClick
                .Subscribe(_ =>
                {
                    gameObject.SetActive(false);
                })
                .AddTo(gameObject);
        }
    }
}
