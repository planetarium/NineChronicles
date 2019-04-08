using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BattleResult : Widget
    {
        public BattleLog.Result result;
        public Text submitText;
        public Text header;
        public Text title;
        public GameObject slotBase;
        public Transform grid;
        private List<InventorySlot> _slots;
        private Stage _stage;

        private void Awake()
        {
            _slots = new List<InventorySlot>();
            _stage = GameObject.Find("Stage").GetComponent<Stage>();
        }

        public void SubmitClick()
        {
            StartCoroutine(SubmitAsync());
        }

        private IEnumerator SubmitAsync()
        {
            var w = Find<LoadingScreen>();
            if (!ReferenceEquals(w, null))
            {
                w.Show();   
            }

            // Reset camera focus
            var cam = Camera.main.GetComponent<ActionCamera>();
            cam.target = null;

            var player = _stage.ReadyPlayer();
            var currentId = ActionManager.Instance.battleLog?.id;
            ActionManager.Instance.HackAndSlash(player.equipments);
            while (currentId == ActionManager.Instance.battleLog?.id)
            {
                yield return null;
            }

            Game.Event.OnStageStart.Invoke();
            Close();

            if (!ReferenceEquals(w, null))
            {
                w.Close();
            }
        }
        public void BackClick()
        {
            Game.Event.OnRoomEnter.Invoke();
            Close();
        }

        public void Show(BattleLog.Result battleResult)
        {
            result = battleResult;

            if (result == BattleLog.Result.Win)
            {
                submitText.text = "다음 퀘스트";
                header.text = "승리";
                title.text = "획득한 아이템";
                grid.gameObject.SetActive(true);
            }
            else
            {
                submitText.text = "재도전";
                title.text = "재도전 하시겠습니까?";
                header.text = "실패";
                grid.gameObject.SetActive(false);
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
            var i = _slots.FindIndex(
                a => a.Item.Data.id.Equals(characterItem.Data.id)
            );
            if (i < 0)
            {
                slotBase.SetActive(true);
                GameObject newSlot = Instantiate(slotBase, grid);
                InventorySlot slot = newSlot.GetComponent<InventorySlot>();
                slot.Set(characterItem, 1);
                _slots.Add(slot);
                slotBase.SetActive(false);
            }
            else
            {
                _slots[i].LabelCount.text = (Convert.ToInt32(_slots[i].LabelCount.text) + 1).ToString();
            }
        }
    }
}
