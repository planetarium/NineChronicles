using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Character;
using Nekoyume.Model.Item;
using Nekoyume.State;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using ObservableExtensions = UniRx.ObservableExtensions;

namespace Nekoyume.UI
{
    public class BoosterPopup : PopupWidget
    {
        [SerializeField]
        private Button cancelButton;

        [SerializeField]
        private Button submitButton;

        [SerializeField]
        private Slider apSlider;

        [SerializeField]
        private TMP_Text boostCountText;

        [SerializeField]
        private TMP_Text needAPText;

        [SerializeField]
        private TMP_Text ownAPText;

        [SerializeField]
        private Button boostPlusButton;

        [SerializeField]
        private Button boostMinusButton;

        private Stage _stage;
        private Player _player;
        private List<Guid> _costumes;
        private List<Guid> _equipments;
        private List<Consumable> _consumables;
        private List<RuneSlotInfo> _runes;
        private int _worldId;
        private int _stageId;

        private static readonly Vector3 PlayerPosition = new Vector3(1999.8f, 1999.3f, 3f);

        protected override void Awake()
        {
            base.Awake();

            ObservableExtensions.Subscribe(cancelButton.OnClickAsObservable(), _ => Close()).AddTo(gameObject);
            ObservableExtensions.Subscribe(submitButton.OnClickAsObservable(), _ => BoostQuest()).AddTo(gameObject);
            ObservableExtensions.Subscribe(boostPlusButton.OnClickAsObservable(), _ => apSlider.value++);
            ObservableExtensions.Subscribe(boostMinusButton.OnClickAsObservable(), _ => apSlider.value--);
        }

        public void Show(
            Stage stage,
            List<Guid> costumes,
            List<Guid> equipments,
            List<Consumable> consumables,
            List<RuneSlotInfo> runes,
            int maxCount,
            int worldId,
            int stageId)
        {
            _stage = stage;
            _costumes = costumes;
            _equipments = equipments;
            _consumables = consumables;
            _runes = runes;
            _player = _stage.GetPlayer(PlayerPosition);
            _worldId = worldId;
            _stageId = stageId;

            ObservableExtensions.Subscribe(ReactiveAvatarState.ObservableActionPoint, value =>
            {
                var costOfStage = GetCostOfStage();
                apSlider.maxValue = value / costOfStage >= maxCount ? maxCount : value / costOfStage;
                ownAPText.text = value.ToString();
            }).AddTo(gameObject);

            apSlider.onValueChanged.AddListener(value =>
            {
                var costOfStage = GetCostOfStage();
                boostCountText.text = value.ToString(CultureInfo.InvariantCulture);
                needAPText.text = (costOfStage * value).ToString(CultureInfo.InvariantCulture);
            });

            var cost = GetCostOfStage();
            var actionPoint = ReactiveAvatarState.ActionPoint;
            ownAPText.text = actionPoint.ToString();
            // Call onValueChanged by Change value
            apSlider.value = 0;
            apSlider.value = apSlider.maxValue =
                actionPoint / cost >= maxCount ? maxCount : actionPoint / cost;
            boostCountText.text = apSlider.value.ToString(CultureInfo.InvariantCulture);
            needAPText.text = (cost * apSlider.value).ToString(CultureInfo.InvariantCulture);
            base.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            apSlider.onValueChanged.RemoveAllListeners();
        }

        private void BoostQuest()
        {
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);

            Find<WorldMap>().Close(true);
            Find<StageInformation>().Close(true);
            Find<LoadingScreen>().Show();
            Close();

            BattleRenderer.Instance.IsOnBattle = true;
            _stage.IsShowHud = true;

            ObservableExtensions.Subscribe(Game.Game.instance.ActionManager.HackAndSlash(
                _costumes,
                _equipments,
                _consumables,
                _runes,
                _worldId,
                _stageId
            ));
        }

        private int GetCostOfStage()
        {
            var selectedStageIdRow =
                Game.Game.instance.TableSheets.StageSheet.Values.FirstOrDefault(i =>
                    i.Id == _stageId);

            return selectedStageIdRow?.CostAP ?? int.MaxValue;
        }
    }
}
