using System;
using System.IO;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Login : Widget
    {
        public bool ready = false;
        public GameObject[] slots;

        protected override void Awake()
        {
            base.Awake();

            if (slots.Length != GameConfig.SlotCount)
            {
                throw new Exception("Login widget's slots.Length is not equals GameConfig.SlotCount.");
            }
        }

        public void SlotClick(int index)
        {
            if (!ready)
                return;

            Game.Event.OnLoginDetail.Invoke(index);
            gameObject.SetActive(false);
            AudioController.PlayClick();
        }

        public void SlotDeleteClick(int index)
        {
            if (!ready)
                return;

            var confirm = Create<Confirm>();
            confirm.Show("UI_CONFIRM_DELETE_CHARACTER_TITLE", "UI_CONFIRM_DELETE_CHARACTER_CONTENT");
            confirm.CloseCallback = result =>
            {
                if (result != ConfirmResult.Yes)
                {
                    return;
                }
                
                Find<GrayLoadingScreen>().Show();

                ActionManager.instance.DeleteAvatar(index)
                    .Subscribe(eval =>
                    {
                        Game.Event.OnNestEnter.Invoke();
                        Show();
                        Find<GrayLoadingScreen>()?.Close();

                        // 강제로 레이아웃 정렬 (업데이트)
                        var layout = slots[index].GetComponentInChildren<HorizontalLayoutGroup>();
                        layout.CalculateLayoutInputHorizontal();
                        layout.SetLayoutHorizontal();
                    }, onError: e => Widget.Find<ActionFailPopup>().Show("Action timeout during DeleteAvatar."));
            };

            AudioController.PlayClick();
        }

        public override void Show()
        {
            base.Show();

            var stage = Game.Game.instance.stage;
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var playerSlot = slot.GetComponent<LoginPlayerSlot>();
                var targetPosition = stage.SelectPositionEnd(i);
                targetPosition.y = 0.0f;

                if (States.Instance.AvatarStates.TryGetValue(i, out var avatarState))
                {
                    playerSlot.LabelLevel.text = $"LV.{avatarState.level}";
                    playerSlot.LabelName.text = avatarState.NameWithHash;
                    playerSlot.CreateView.SetActive(false);
                    playerSlot.DeleteView.SetActive(true);
                    playerSlot.NameView.SetActive(true);
                }
                else
                {
                    playerSlot.CreateView.SetActive(true);
                    playerSlot.DeleteView.SetActive(false);
                    playerSlot.NameView.SetActive(false);
                }
            }

            AudioController.instance.PlayMusic(AudioController.MusicCode.SelectCharacter);
        }
    }
}
