using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FancyScrollView;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Tween;
using TMPro;
using UniRx;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    public class NotificationCell : FancyCell<
        NotificationCell.ViewModel,
        NotificationScroll.DefaultContext>
    {
        public enum AnimationState
        {
            None,
            New,
            Add,
            Remove
        }

        public class ViewModel
        {
            public AnimationState animationState = AnimationState.New;
            public MailType mailType;
            public string message;
            public float addedTime;
        }

        [SerializeField]
        private BaseTweener[] addTweeners = null;

        [SerializeField]
        private BaseTweener[] removeTweeners = null;

        [SerializeField]
        private Image iconImage = null;

        [SerializeField]
        private TextMeshProUGUI messageText = null;

        private ViewModel _viewModel;

        private Coroutine _coCheckCompleteOfAnimation;

        private readonly List<IDisposable> _disposablesForSetContext = new List<IDisposable>();

        private void OnDisable()
        {
            _coCheckCompleteOfAnimation = null;

            KillTweeners();
        }

        public override void SetContext(NotificationScroll.DefaultContext context)
        {
            base.SetContext(context);

            _disposablesForSetContext.DisposeAllAndClear();
            context.OnCompleteOfAddAnimation
                .Merge(context.OnCompleteOfRemoveAnimation)
                .Where(cell => cell.Index == Index)
                .Subscribe(cell => cell._viewModel.animationState = AnimationState.None)
                .AddTo(_disposablesForSetContext);
            context.PlayRemoveAnimation
                .Where(tuple => tuple.index == Index)
                .Subscribe(tuple =>
                {
                    tuple.viewModel.animationState = AnimationState.Remove;
                    PlayAnimation(removeTweeners, Context.OnCompleteOfRemoveAnimation);
                })
                .AddTo(_disposablesForSetContext);
        }

        public override void UpdateContent(ViewModel itemData)
        {
            _viewModel = itemData;

            switch (_viewModel.animationState)
            {
                case AnimationState.None:
                    ResetTweeners();
                    break;
                case AnimationState.New:
                    _viewModel.animationState = AnimationState.Add;
                    PlayAnimation(addTweeners, Context.OnCompleteOfAddAnimation);
                    AudioController.instance.PlaySfx(AudioController.SfxCode.Notice);
                    break;
                case AnimationState.Add:
                case AnimationState.Remove:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            iconImage.overrideSprite = SpriteHelper.GetMailIcon(_viewModel.mailType);
            messageText.text = _viewModel.message;
        }

        public override void UpdatePosition(float position)
        {
            var scrollSize = Context.CalculateScrollSize();
            var start = 0.5f * scrollSize;
            var end = -start;
            var localPosition = math.lerp(start, end, position);
            transform.localPosition = new Vector2(0f, localPosition);
        }

        private void PlayAnimation(
            BaseTweener[] tweeners,
            IObserver<NotificationCell> completeSubject)
        {
            KillTweeners();

            foreach (var tweener in tweeners)
            {
                tweener.PlayTween();
            }

            if (!(_coCheckCompleteOfAnimation is null))
            {
                StopCoroutine(_coCheckCompleteOfAnimation);
                _coCheckCompleteOfAnimation = null;
            }

            _coCheckCompleteOfAnimation = StartCoroutine(CoCheckCompleteOfAnimation(
                tweeners,
                completeSubject));
        }

        private IEnumerator CoCheckCompleteOfAnimation(
            BaseTweener[] tweeners,
            IObserver<NotificationCell> completeSubject)
        {
            var playing = true;
            while (playing)
            {
                playing = tweeners.Any(tweener => tweener.IsPlaying);
                yield return null;
            }

            completeSubject.OnNext(this);
        }

        private void KillTweeners()
        {
            foreach (var tweener in addTweeners)
            {
                tweener.KillTween();
            }

            foreach (var tweener in removeTweeners)
            {
                tweener.KillTween();
            }
        }

        private void ResetTweeners()
        {
            foreach (var tweener in addTweeners)
            {
                tweener.ResetToOrigin();
            }

            foreach (var tweener in removeTweeners)
            {
                tweener.ResetToOrigin();
            }
        }
    }
}
