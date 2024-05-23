using Cysharp.Threading.Tasks;
using DG.Tweening;
using Nekoyume.L10n;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class AdventureBossStartNotificationPopup : PopupWidget
    {
        [SerializeField] private TextMeshProUGUI continueText;

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
                for (int i = 3; i >= 0; i--)
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
