using Cysharp.Threading.Tasks;
using DG.Tweening;
using Nekoyume.Helper;
using Nekoyume.L10n;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class AdventureBossStartNotificationPopup : PopupWidget
    {
        [SerializeField] private TextMeshProUGUI continueText;
        [SerializeField] private Image bossImage;

        private CancellationTokenSource cancellationTokenSource;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            if (Game.Game.instance.AdventureBossData.SeasonInfo.Value != null)
            {
                bossImage.sprite = SpriteHelper.GetBigCharacterIcon(Game.Game.instance.AdventureBossData.SeasonInfo.Value.BossId);
                bossImage.SetNativeSize();
            }
            else
            {
                bossImage.sprite = SpriteHelper.GetBigCharacterIcon(0);
                bossImage.SetNativeSize();
            }

            ShowCount(cancellationTokenSource.Token).Forget();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            //check showcount
            cancellationTokenSource?.Cancel();
        }

        public async UniTaskVoid ShowCount(CancellationToken cancellationToken)
        {
            try
            {
                for (var i = 3; i >= 0; i--)
                {
                    continueText.text = L10nManager.Localize("UI_ADVENTUREBOSS_START_NOTI_SUB_DESC", i);
                    continueText.DORewind();
                    continueText.alpha = 1;
                    continueText.DOFade(0, 1f).SetEase(Ease.InCubic);
                    await UniTask.Delay(1000, cancellationToken: cancellationToken);
                }

                Close();
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
