using System;
using System.Collections;
using System.IO;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
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
        }

        private void Start()
        {
            Show();
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

            var confirm = Widget.Create<Confirm>();
            confirm.Show("캐릭터 삭제", "정말 삭제하시겠습니까?", "삭제합니다", "아니오");
            confirm.CloseCallback = (ConfirmResult result) => {
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

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];

                var slotRect = slot.GetComponent<RectTransform>();
                var targetPosition = new Vector3(-2.2f + i * 2.22f, 0.0f, 0.0f);
                slotRect.anchoredPosition = targetPosition.ToCanvasPosition(Nekoyume.Game.ActionCamera.instance.Cam, MainCanvas.instance.Canvas);

                var playerSlot = slot.GetComponent<LoginPlayerSlot>();
                playerSlot.CreateView.SetActive(true);
                playerSlot.NameView.SetActive(true);
                playerSlot.DeleteView.SetActive(true);
                try
                {
                    var avatar = AvatarManager.Avatars[i];
                    playerSlot.LabelLevel.text = $"LV.{avatar.Level}";
                    playerSlot.LabelName.text = $"{avatar.Name}";
                    playerSlot.CreateView.SetActive(false);
                }
                catch (Exception e)
                {
                    playerSlot.NameView.SetActive(false);
                    playerSlot.DeleteView.SetActive(false);
                    if (e is ArgumentOutOfRangeException || e is NullReferenceException)
                    {
                        var tween = slot.GetComponentInChildren<UI.Tween.DOTweenImageAlpha>();
                        if (tween)
                            tween.gameObject.SetActive(false);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            StartCoroutine(CoPlayMusic());
        }

        private IEnumerator CoPlayMusic()
        {
            while (AudioController.instance.state != AudioController.State.Idle)
            {
                yield return null;
            }
            
            AudioController.instance.PlayMusic(AudioController.MusicCode.SelectCharacter);
        }
    }
}
