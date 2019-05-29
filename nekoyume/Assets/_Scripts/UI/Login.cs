using System.IO;
using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using UnityEngine;

namespace Nekoyume.UI
{
    public class Login : Widget
    {
        public bool ready = false;
        public GameObject[] slots;

        public void SlotClick(int index)
        {
            if (!ready)
                return;

            Game.Event.OnLoginDetail.Invoke(index);
            gameObject.SetActive(false);
            AudioController.PlayClick();
        }

        /// <summary>
        /// ToDo. DeleteNovice 액션을 통해서 삭제되도록.
        /// </summary>
        /// <param name="index"></param>
        public void SlotDeleteClick(int index)
        {
            if (!ready)
                return;

            var confirm = Widget.Create<Confirm>();
            confirm.Show("캐릭터 삭제", "정말 삭제하시겠습니까?", "삭제합니다", "아니오");
            confirm.CloseCallback = (ConfirmResult result) =>
            {
                if (result == ConfirmResult.Yes)
                {
                    //Delete key, avatar
                    var prefsKey = string.Format(AvatarManager.PrivateKeyFormat, index);
                    string privateKey = PlayerPrefs.GetString(prefsKey, "");
                    PlayerPrefs.DeleteKey(prefsKey);
                    Debug.Log($"Delete {prefsKey}: {privateKey}");
                    var fileName = string.Format(AvatarManager.AvatarFileFormat, index);
                    string datPath = Path.Combine(Application.persistentDataPath, fileName);
                    if (File.Exists(datPath))
                        File.Delete(datPath);
                    PlayerPrefs.Save();

                    Game.Event.OnNestEnter.Invoke();
                    Show();
                }
            };

            AudioController.PlayClick();
        }

        public override void Show()
        {
            base.Show();

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var playerSlot = slot.GetComponent<LoginPlayerSlot>();
                var slotRect = slot.GetComponent<RectTransform>();
                var targetPosition = new Vector3(-2.2f + i * 2.22f, 0.0f, 0.0f);
                slotRect.anchoredPosition = targetPosition.ToCanvasPosition(Game.ActionCamera.instance.Cam,
                    MainCanvas.instance.Canvas);

                if (States.AvatarStates.TryGetValue(i, out var avatarState))
                {
                    playerSlot.LabelLevel.text = $"LV.{avatarState.level}";
                    playerSlot.LabelName.text = $"{avatarState.name}";
                    playerSlot.CreateView.SetActive(false);
                    playerSlot.NameView.SetActive(true);
                    playerSlot.DeleteView.SetActive(true);
                }
                else
                {
                    playerSlot.LabelLevel.text = "LV.1";
                    playerSlot.LabelName.text = "";
                    playerSlot.CreateView.SetActive(true);
                    playerSlot.NameView.SetActive(true);
                    playerSlot.DeleteView.SetActive(false);   
                }
            }

            AudioController.instance.PlayMusic(AudioController.MusicCode.SelectCharacter);
        }
    }
}
