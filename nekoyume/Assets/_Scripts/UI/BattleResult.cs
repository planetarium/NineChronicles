using System;
using System.Collections;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Manager;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.VFX;
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
        public Text backText;
        public Text submitText;
        public Text header;
        public Text title;
        public Text timeText;
        public GameObject slotBase;
        public Transform grid;
        public GameObject modal;
        public bool actionEnd;

        private List<InventorySlot> _slots;
        private Stage _stage;
        private bool _repeat;
        private bool _autoNext;
        private float _timer = 0;
        private string _timeText;

        private BattleWinVFX _battleWinVFX;
        private Image _image;

        protected override void Awake()
        {
            base.Awake();

            _slots = new List<InventorySlot>();
            _stage = GameObject.Find("Stage").GetComponent<Stage>();
            _image = GetComponent<Image>();

            backText.text = LocalizationManager.Localize("UI_GO_OUT");
        }

        public void SubmitClick()
        {
            Submit();
            AudioController.PlayClick();

            if (result == BattleLog.Result.Win)
            {
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickBattleResultNext);
            }
            else
            {
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickBattleResultRetry);
            }
        }

        private void Submit()
        {
            _autoNext = false;
            var w = Find<StageLoadingScreen>();
            w.Show(this, _stage.zone);
            Find<Status>().Close();
            Find<Gold>().Close();

            if (!ReferenceEquals(_battleWinVFX, null))
            {
                _battleWinVFX.Stop();
            }

            var player = _stage.RunPlayer();
            player.DisableHUD();
            var stage = _stage.id;
            if (!_stage.repeatStage && result == BattleLog.Result.Win)
                stage++;
            actionEnd = false;
            ActionManager.instance.HackAndSlash(player.equipments, new List<Food>(), stage)
                .Subscribe(_ => { StartCoroutine(CoNextStage(w)); }).AddTo(this);

            Hide();
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
            AnalyticsManager.Instance.BattleLeave();
        }

        public void Show(BattleLog.Result battleResult, bool repeat)
        {
            _repeat = repeat;
            _image.enabled = true;
            modal.SetActive(true);
            result = battleResult;
            _autoNext = true;

            if (result == BattleLog.Result.Win)
            {
                string submit = LocalizationManager.Localize("UI_NEXT_STAGE");
                _timeText = LocalizationManager.Localize("UI_NEXT_STAGE_FORMAT");
                if (_repeat)
                {
                    submit = LocalizationManager.Localize("UI_BATTLE_AGAIN");
                    _timeText = LocalizationManager.Localize("UI_BATTLE_AGAIN_FORMAT");
                }

                submitText.text = submit;
                header.text = LocalizationManager.Localize("UI_BATTLE_WIN");
                title.text = LocalizationManager.Localize("UI_REWARDS");
                grid.gameObject.SetActive(true);
                _timer = Timer;

                AudioController.instance.PlayMusic(AudioController.MusicCode.Win, 0.3f);
                _battleWinVFX =
                    VFXController.instance.Create<BattleWinVFX>(ActionCamera.instance.transform, VfxBattleWinOffset);
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionBattleWin);
            }
            else
            {
                submitText.text = LocalizationManager.Localize("UI_BATTLE_RETRY");
                title.text = LocalizationManager.Localize("UI_BATTLE_RETRY_FORMAT");
                header.text = LocalizationManager.Localize("UI_BATTLE_LOSE");
                
                _repeat = false;
                _stage.repeatStage = _repeat;
                _autoNext = false;

                AudioController.instance.PlayMusic(AudioController.MusicCode.Lose);
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionBattleLose);
            }

            timeText.gameObject.SetActive(_autoNext);

            base.Show();

            StartCoroutine(ShowSlots());
        }

        private IEnumerator ShowSlots()
        {
            yield return new WaitForSeconds(0.5f);
            foreach (var slot in _slots)
            {
                var container = slot.gameObject.transform.GetChild(0);
                container.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.4f);
            }
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
            if (!_autoNext)
            {
                return;
            }

            _timer -= Time.deltaTime;
            var timerText = string.Format(LocalizationManager.Localize("UI_N_SECONDS_LATER"), _timer); 
            timeText.text = string.Format(_timeText, timerText);
            
            if (_timer > 0)
            {
                return;
            }

            _repeat = false;
            Submit();
            AnalyticsManager.Instance.BattleContinueAutomatically();
        }

        private void Hide()
        {
            _image.enabled = false;
            modal.SetActive(false);
        }

        private IEnumerator CoNextStage(StageLoadingScreen loadingScreen)
        {
            actionEnd = true;
            yield return StartCoroutine(loadingScreen.CoClose());
            Game.Event.OnStageStart.Invoke();
            Close();
        }
    }
}
