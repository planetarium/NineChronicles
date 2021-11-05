using System.Collections.Generic;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Model.Item;
using Nekoyume.State;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

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
        private List<Costume> _costumes;
        private List<Equipment> _equipments;
        private List<Consumable> _consumables;
        private int _worldId;
        private int _stageId;

        private static readonly Vector3 PlayerPosition = new Vector3(1999.8f, 1999.3f, 3f);

        protected override void Awake()
        {
            base.Awake();

            cancelButton.OnClickAsObservable().Subscribe(_ => Close()).AddTo(gameObject);
            submitButton.OnClickAsObservable().Subscribe(_ => BoostQuest()).AddTo(gameObject);
            boostPlusButton.OnClickAsObservable().Subscribe(_ => apSlider.value++);
            boostMinusButton.OnClickAsObservable().Subscribe(_ => apSlider.value--);
        }

        public void Show(
            Stage stage,
            List<Costume> costumes,
            List<Equipment> equipments,
            List<Consumable> consumables,
            int maxCount,
            int worldId,
            int stageId)
        {
            _stage = stage;
            _costumes = costumes;
            _equipments = equipments;
            _consumables = consumables;
            _player = _stage.GetPlayer(PlayerPosition);
            _worldId = worldId;
            _stageId = stageId;

            ReactiveAvatarState.ActionPoint.Subscribe(value =>
            {
                var costOfStage = GetCostOfStage();
                apSlider.maxValue = value / costOfStage >= maxCount ? maxCount : value / costOfStage;
                ownAPText.text = value.ToString();
            }).AddTo(gameObject);

            apSlider.onValueChanged.AddListener(value =>
            {
                var costOfStage = GetCostOfStage();
                boostCountText.text = value.ToString();
                needAPText.text = (costOfStage * value).ToString();
            });

            var cost = GetCostOfStage();
            var actionPoint = Game.Game.instance.States.CurrentAvatarState.actionPoint;
            ownAPText.text = actionPoint.ToString();
            // Call onValueChanged by Change value
            apSlider.value = 0;
            apSlider.value = apSlider.maxValue =
                actionPoint / cost >= maxCount ? maxCount : actionPoint / cost;
            boostCountText.text = apSlider.value.ToString();
            needAPText.text = (cost * apSlider.value).ToString();
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

            _stage.IsInStage = true;
            _stage.IsShowHud = true;

            if (_stageId >= GameConfig.MimisbrunnrStartStageId)
            {
                Game.Game.instance.ActionManager.MimisbrunnrBattle(
                        _costumes,
                        _equipments,
                        _consumables,
                        _worldId,
                        _stageId,
                        (int) apSlider.value
                    );
            }
            else
            {
                Game.Game.instance.ActionManager.HackAndSlash(
                    _costumes,
                    _equipments,
                    _consumables,
                    _worldId,
                    _stageId,
                    (int)apSlider.value
                );
            }
        }

        private int GetCostOfStage()
        {
            var selectedStageIdRow =
                Game.Game.instance.TableSheets.StageSheet.Values.FirstOrDefault(i =>
                    i.Id == _stageId);

            return selectedStageIdRow.CostAP;
        }
    }
}
