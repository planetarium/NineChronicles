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

        [SerializeField]
        private bool isTitleFrame = false;

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

            if (!isTitleFrame)
            {
                return;
            }

            var title = avatarState.inventory.Costumes.FirstOrDefault(costume =>
                costume.ItemSubType == ItemSubType.Title &&
                costume.equipped);
            if (title is null)
            {
                SetFrame(null);
            }
            else
            {
                var image = SpriteHelper.GetTitleFrame(title.Id);
                SetFrame(image);
            }
        }

        public override void SetByPlayer(Player player)
        {
            base.SetByPlayer(player);

            if (!isTitleFrame)
            {
                return;
            }

            var title = player.Costumes.FirstOrDefault(costume =>
                costume.ItemSubType == ItemSubType.Title &&
                costume.equipped);
            if (title is null)
            {
                SetFrame(null);
            }
            else
            {
                var image = SpriteHelper.GetTitleFrame(title.Id);
                SetFrame(image);
            }
        }

        public void Set(List<Equipment> equipments, List<Costume> costumes, int characterId)
        {
            var itemId = GameConfig.DefaultAvatarArmorId;
            var armor = equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
            if (armor != null)
            {
                itemId = armor.Id;
            }

            var fullCostume = costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.FullCostume);
            if (fullCostume != null)
            {
                itemId = fullCostume.Id;
            }

            base.Set(itemId, characterId);

            var title = costumes.FirstOrDefault(costume =>
                costume.ItemSubType == ItemSubType.Title &&
                costume.equipped);
            if (title is null)
            {
                SetFrame(null);
            }
            else
            {
                var image = SpriteHelper.GetTitleFrame(title.Id);
                SetFrame(image);
            }
        }

        protected override void SetDim(bool isDim)
        {
            base.SetDim(isDim);
            var alpha = isDim ? .3f : 1f;
            frameImage.color = GetColor(frameImage.color, alpha);
        }

        protected void SetFrame(Sprite image)
        {
            if (image is null)
            {
                frameImage.enabled = false;
                return;
            }

            frameImage.overrideSprite = image;
            frameImage.SetNativeSize();
            frameImage.enabled = true;
        }
    }
}
