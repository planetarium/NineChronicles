using System;
using System.Collections.Generic;
using Nekoyume.Manager;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BattleResult : Widget
    {
        private static readonly Vector3 VfxBattleWinOffset = new Vector3(-3.43f, -0.28f, 10f);
        private const float Timer = 5.0f;
        
        public BattleLog.Result result;
        public Text submitText;
        public Text header;
        public Text title;
        public Text timeText;
        public GameObject slotBase;
        public Transform grid;
        public GameObject submitPanel;
        public GameObject modal;
        public bool actionEnd;
        private List<InventorySlot> _slots;
        private Stage _stage;
        private bool _repeat;
        private float _timer = 0;

        private BattleWinVFX _battleWinVFX;
        private Image _image;

        protected override void Awake()
        {
            base.Awake();
            
            _slots = new List<InventorySlot>();
            _stage = GameObject.Find("Stage").GetComponent<Stage>();
            _image = GetComponent<Image>();
        }

        public void SubmitClick()
        {
            Submit();
            AudioController.PlayClick();
            
            if (result == BattleLog.Result.Win)
            {
                AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ClickBattleResultNext);
            }
            else
            {
                AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ClickBattleResultRetry);
            }
        }

        private void Submit()
        {
            var w = Find<StageLoadingScreen>();
            w.Show(this, _stage.zone);

            if (!ReferenceEquals(_battleWinVFX, null))
            {
                _battleWinVFX.Stop();
            }

            var player = _stage.ReadyPlayer();
            var stage = _stage.id;
            if (!_stage.repeatStage)
                stage++;
            actionEnd = false;
            IObservable<ActionBase.ActionEvaluation<HackAndSlash>> observable =
                ActionManager.instance.HackAndSlash(player.equipments, new List<Food>(), stage);

            Hide();

            observable.ObserveOnMainThread().Subscribe(_ =>
            {
                
                actionEnd = true;
                StartCoroutine(w.CoClose());
                
                Game.Event.OnStageStart.Invoke();
                Close();
            }).AddTo(this);
        }
        public void BackClick()
        {   
            if (!ReferenceEquals(_battleWinVFX, null))
            {
                _battleWinVFX.Stop();
            }

            _stage.repeatStage = false;
            
            Game.Event.OnRoomEnter.Invoke();
            Close();
            AudioController.PlayClick();
            AnalyticsManager.instance.BattleLeave();
        }

        public void Show(BattleLog.Result battleResult, bool repeat)
        {
            _repeat = repeat;
            _image.enabled = true;
            modal.SetActive(true);
            result = battleResult;

            if (result == BattleLog.Result.Win)
            {
                submitText.text = "다음 퀘스트";
                header.text = "승리";
                title.text = "획득한 아이템";
                grid.gameObject.SetActive(true);

                submitPanel.SetActive(!_repeat);
                timeText.gameObject.SetActive(_repeat);
                if (_repeat)
                    _timer = Timer;
                
                AudioController.instance.PlayMusic(AudioController.MusicCode.Win, 0.3f);
                _battleWinVFX = VFXController.instance.Create<BattleWinVFX>(ActionCamera.instance.transform, VfxBattleWinOffset);
                _battleWinVFX.Play(10f);
                AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ActionBattleWin);
            }
            else
            {
                submitText.text = "재도전";
                title.text = "재도전 하시겠습니까?";
                header.text = "실패";
                _repeat = false;
                timeText.gameObject.SetActive(_repeat);
                _stage.repeatStage = _repeat;

                AudioController.instance.PlayMusic(AudioController.MusicCode.Lose);
                AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ActionBattleLose);
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

        private void Update()
        {
            if (_repeat)
            {
                _timer -= Time.deltaTime;
                timeText.text = $"{_timer:0}초 후 다시 전투를 시작합니다.";
                if (_timer <= 0)
                {
                    _repeat = false;
                    Submit();
                    AnalyticsManager.instance.BattleContinueAutomatically();
                }
            }
        }

        private void Hide()
        {
            _image.enabled = false;
            modal.SetActive(false);
        }
    }
}
