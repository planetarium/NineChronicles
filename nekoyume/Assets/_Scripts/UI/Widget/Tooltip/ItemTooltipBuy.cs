using Libplanet.Assets;
using Nekoyume.Game.Controller;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume
{
    using UniRx;

    public class ItemTooltipBuy : MonoBehaviour
    {
        [SerializeField]
        private BlockTimer timer;

        [SerializeField]
        private SubmitWithCostButton button;

        public void Set(long expiredBlockIndex, FungibleAssetValue ncg, bool isEnough,
            System.Action onSubmit)
        {
            button.OnSubmitClick.Dispose();
            button.OnSubmitClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                onSubmit?.Invoke();
            }).AddTo(gameObject);
            timer.UpdateTimer(expiredBlockIndex);
        }
    }
}
