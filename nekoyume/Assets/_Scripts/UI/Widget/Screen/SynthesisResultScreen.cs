#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    /// <summary>
    /// 합성의 결과물을 표시합니다
    /// 컨셉은 ScreenWidget이지만, TooltipLayer보다 아래 렌더링 되어야 하여 팝업으로 구현됩니다.
    /// </summary>
    public class SynthesisResultScreen : PopupWidget
    {
        private static readonly int AnimatorHashHide = Animator.StringToHash("Hide");
        private static readonly int AnimatorHashShow = Animator.StringToHash("Show");
        private static readonly int AnimatorHashShowButton = Animator.StringToHash("ShowButton");

        private const float ItemViewAnimInterval = 0.1f;
        private const float DefaultAnimInterval = 1f;

        [SerializeField] private Button closeButton = null!;
        [SerializeField] private Animator animator = null!;

        [SerializeField] private RectTransform scrollView = null!; // TODO: many items
        [SerializeField] private SynthesisResultItemView itemViewPrefab = null!;
        [SerializeField] private Transform itemViewParent = null!;

        private readonly List<SynthesisResultItemView> _cachedItemViews = new ();

        protected override void Awake()
        {
            foreach (Transform itemView in itemViewParent)
            {
                if (itemView.TryGetComponent(out SynthesisResultItemView view))
                {
                    _cachedItemViews.Add(view);
                }
                else
                {
                    Destroy(itemView.gameObject);
                }
            }

            base.Awake();

            closeButton.onClick.AddListener(() => Close(true));
            CloseWidget = closeButton.onClick.Invoke;
        }

        public void Show(List<SynthesizeResult> resultList, bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            animator.SetTrigger(AnimatorHashHide);

            if (_cachedItemViews.Count < resultList.Count)
            {
                for (var i = _cachedItemViews.Count; i < resultList.Count; i++)
                {
                    var itemView = Instantiate(itemViewPrefab, itemViewParent);
                    _cachedItemViews.Add(itemView);
                }
            }

            // 성공했으면 뒤쪽으로 정렬
            resultList.Sort((a, b) => a.IsSuccess.CompareTo(b.IsSuccess));

            for (var i = 0; i < _cachedItemViews.Count; i++)
            {
                var view = _cachedItemViews[i];
                if (i < resultList.Count)
                {
                    view.SetData(resultList[i].ItemBase, true);
                    view.Show();
                    view.SetSuccess(resultList[i].IsSuccess);
                }
                else
                {
                    view.Hide();
                }
            }

            PlayResultAnimation().Forget();
        }

        private async UniTask PlayResultAnimation()
        {
            var audioController = AudioController.instance;

            await UniTask.Yield();
            audioController.PlaySfx(AudioController.SfxCode.Win);
            animator.SetTrigger(AnimatorHashShow);

            await UniTask.Delay(TimeSpan.FromSeconds(DefaultAnimInterval));
            audioController.PlaySfx(AudioController.SfxCode.Success);

            var activeItemCount = _cachedItemViews.Count(v => v.gameObject.activeInHierarchy);
            foreach (var itemView in _cachedItemViews)
            {
                if (!itemView.gameObject.activeInHierarchy)
                {
                    continue;
                }

                itemView.ShowWithAnimation();
                AudioController.PlaySelect();
                await UniTask.Delay(TimeSpan.FromSeconds(activeItemCount != 1 ?
                    DefaultAnimInterval/_cachedItemViews.Count :
                    ItemViewAnimInterval));
                // TODO: Success
            }

            await UniTask.Delay(TimeSpan.FromSeconds(DefaultAnimInterval));

            await UniTask.Yield();
            animator.SetTrigger(AnimatorHashShowButton);
        }
    }
}
