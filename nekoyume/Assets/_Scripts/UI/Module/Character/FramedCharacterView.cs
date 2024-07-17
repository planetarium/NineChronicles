using System;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
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

        public readonly Subject<AvatarState> OnClickCharacterIcon = new();

        private AvatarState _avatarStateToDisplay;

        private const string DccFrameIconName = "character_frame_dcc";
        private const string DefaultFrameIconName = "character_frame";

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

        public override void SetByCharacterId(int characterId)
        {
            base.SetByCharacterId(characterId);
            SetFrame(false);
        }

        public override void SetByFullCostumeOrArmorId(int armorOrFullCostumeId)
        {
            base.SetByFullCostumeOrArmorId(armorOrFullCostumeId);
            SetFrame(false);
        }

        public void SetByDccId(int dccId)
        {
            var sprite = SpriteHelper.GetDccProfileIcon(dccId);
            if (sprite is null)
            {
                throw new FailedToLoadResourceException<Sprite>(dccId.ToString());
            }

            SetIcon(sprite);
            SetFrame(true);
        }

        protected override void SetDim(bool isDim)
        {
            base.SetDim(isDim);
            var alpha = isDim ? .3f : 1f;
            frameImage.color = GetColor(frameImage.color, alpha);
        }

        private void SetFrame(bool isActiveDcc)
        {
            var frameName = isActiveDcc ? DccFrameIconName : DefaultFrameIconName;
            var frameSprite = SpriteHelper.GetProfileFrameIcon(frameName);
            if (frameSprite is null)
            {
                throw new FailedToLoadResourceException<Sprite>(frameName);
            }

            frameImage.sprite = frameSprite;
            frameImage.enabled = true;
        }
    }
}
