using System.Collections;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BattleResult : Widget
    {
        public BattleLog.Result result;
        public Image headerWin;
        public Image headerLose;
        public Text submitText;
        public Text title;
        public GameObject slotBase;
        public Transform grid;
        public Image imageLose;
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
            var player = FindObjectOfType<Game.Character.Player>();
            if (player == null)
            {
                var factory = GameObject.Find("Stage").GetComponent<PlayerFactory>();
                player = factory.Create().GetComponent<Game.Character.Player>();
            }
            var currentId = ActionManager.Instance.battleLog?.id;
            ActionManager.Instance.HackAndSlash(player.equipments);
            while (currentId == ActionManager.Instance.battleLog?.id)
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
            headerWin.gameObject.SetActive(false);
            headerLose.gameObject.SetActive(false);

            if (result == BattleLog.Result.Win)
            {
                submitText.text = "다음 퀘스트";
                headerWin.gameObject.SetActive(true);
                title.text = "획득한 아이템";
                grid.gameObject.SetActive(true);
                imageLose.gameObject.SetActive(false);
            }
            else
            {
                submitText.text = "재도전";
                headerLose.gameObject.SetActive(true);
                title.text = "";
                grid.gameObject.SetActive(false);
                imageLose.gameObject.SetActive(true);
            }
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
