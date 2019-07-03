using System;
using System.IO;
using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine;

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

        /// <summary>
        /// ToDo. DeleteAvatar 액션을 통해서 삭제되도록.
        /// </summary>
        /// <param name="index"></param>
        public void SlotDeleteClick(int index)
        {
            if (!ready)
                return;

            var confirm = Create<Confirm>();
            confirm.Show("캐릭터 삭제", "정말 삭제하시겠습니까?", "삭제합니다", "아니오");
            confirm.CloseCallback = result =>
            {
                if (result != ConfirmResult.Yes)
                {
                    return;
                }
                
                Find<GrayLoadingScreen>()?.Show();

                ActionManager.instance.DeleteAvatar(index)
                    .Subscribe(eval =>
                    {
                        Game.Event.OnNestEnter.Invoke();
                        Show();
                        Find<GrayLoadingScreen>()?.Close();
                    });
            };

            AudioController.PlayClick();
        }

        public override void Show()
        {
            base.Show();

            var stage = Nekoyume.Game.Game.instance.stage;
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var playerSlot = slot.GetComponent<LoginPlayerSlot>();
                var slotRect = slot.GetComponent<RectTransform>();
                var targetPosition = stage.selectPositionEnd(i);
                targetPosition.y = 0.0f;
                slotRect.anchoredPosition = targetPosition.WorldToScreen(Game.ActionCamera.instance.Cam,
                    MainCanvas.instance.Canvas);

                if (States.Instance.avatarStates.TryGetValue(i, out var avatarState))
                {
                    playerSlot.LabelLevel.text = $"LV.{avatarState.level}";
                    playerSlot.LabelName.text = $"{avatarState.name}";
                    playerSlot.CreateView.SetActive(false);
                    playerSlot.DeleteView.SetActive(true);
                }
                else
                {
                    playerSlot.LabelLevel.text = "LV.1";
                    playerSlot.LabelName.text = "";
                    playerSlot.CreateView.SetActive(true);
                    playerSlot.DeleteView.SetActive(false);   
                }
                playerSlot.NameView.SetActive(true);
            }

            AudioController.instance.PlayMusic(AudioController.MusicCode.SelectCharacter);
        }
    }
}
