using System.Collections;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BattleResult : Widget
    {
        public BattleLog.Result result;
        public Image header;
        public Text submitText;
        public Text title;
        public GameObject slotBase;
        public Transform grid;
        public Image image;
        private List<InventorySlot> _slots;

        private void Awake()
        {
            _slots = new List<InventorySlot>();
        }

        public void SubmitClick()
        {
            StartCoroutine(SubmitAsync());
        }

        private IEnumerator SubmitAsync()
        {
            var currentAvatar = ActionManager.Instance.Avatar;
            var player = FindObjectOfType<Game.Character.Player>();
            ActionManager.Instance.HackAndSlash(player.equipments);
            while (currentAvatar.Equals(ActionManager.Instance.Avatar))
            {
                yield return new WaitForSeconds(1.0f);
            }

            Game.Event.OnStageStart.Invoke();
            Close();

        }
        public void BackClick()
        {
            Game.Event.OnRoomEnter.Invoke();
            Close();
        }

        public void Show(BattleLog.Result battleResult)
        {
            result = battleResult;
            Sprite sprite;
            if (result == BattleLog.Result.Win)
            {
                submitText.text = "다음 퀘스트";
                sprite = Resources.Load<Sprite>($"ui/UI_01");
                title.text = "획득한 아이템";
                grid.gameObject.SetActive(true);
                image.gameObject.SetActive(false);
            }
            else
            {
                submitText.text = "재도전";
                sprite = Resources.Load<Sprite>($"ui/UI_02");
                title.text = "";
                grid.gameObject.SetActive(false);
                image.gameObject.SetActive(true);
            }
            header.sprite = sprite;
            base.Show();
        }

        public override void Close()
        {
            _slots.Clear();
            foreach (Transform child in grid)
            {
                Destroy(child.gameObject);
            }
            base.Close();
        }

        public void Add(ItemBase characterItem)
        {
            slotBase.SetActive(true);
            GameObject newSlot = Instantiate(slotBase, grid);
            InventorySlot slot = newSlot.GetComponent<InventorySlot>();
            slot.Set(characterItem, 1);
            _slots.Add(slot);
            slotBase.SetActive(false);
        }
    }
}
