using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class FramedCharacterView : VanillaCharacterView
    {
        [SerializeField]
        private Button button = null;

        [SerializeField]
        private Image frameImage = null;

        public readonly Subject<AvatarState> OnClickCharacterIcon = new Subject<AvatarState>();

        private AvatarState _avatarStateToDisplay;

        private void Awake()
        {
            button.OnClickAsObservable()
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(_ =>
                {
                    OnClickCharacterIcon.OnNext(_avatarStateToDisplay);
                    AudioController.PlayClick();
                })
                .AddTo(gameObject);
        }

        public override void SetByAvatarState(AvatarState avatarState)
        {
            base.SetByAvatarState(avatarState);
            _avatarStateToDisplay = avatarState;
        }

        protected override void SetDim(bool isDim)
        {
            base.SetDim(isDim);
            var alpha = isDim ? .3f : 1f;
            frameImage.color = GetColor(frameImage.color, alpha);
        }
    }
}
