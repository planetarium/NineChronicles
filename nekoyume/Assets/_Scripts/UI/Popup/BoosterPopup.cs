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

        private static readonly Vector3 PlayerPosition = new Vector3(1999.8f, 1999.3f, 3f);
        private const int MaxBoostCount = 12;

        protected override void Awake()
        {
            base.Awake();

            cancelButton.OnClickAsObservable().Subscribe(_ => Close()).AddTo(gameObject);
            submitButton.OnClickAsObservable().Subscribe(_ => BoostQuest()).AddTo(gameObject);
            boostPlusButton.OnClickAsObservable().Subscribe(_ => apSlider.value++);
            boostMinusButton.OnClickAsObservable().Subscribe(_ => apSlider.value--);
        }

        public void Show(Stage stage, List<Costume> costumes, List<Equipment> equipments, List<Consumable> consumables)
        {
            _stage = stage;
            _costumes = costumes;
            _equipments = equipments;
            _consumables = consumables;
            _player = _stage.GetPlayer(PlayerPosition);

            ReactiveAvatarState.ActionPoint.Subscribe(value =>
            {
                var cost = GetCostOfStage();
                apSlider.maxValue = value / cost >= MaxBoostCount ? MaxBoostCount : value / cost;
                ownAPText.text = value.ToString();
            }).AddTo(gameObject);

            apSlider.onValueChanged.AddListener(value =>
            {
                var cost = GetCostOfStage();
                boostCountText.text = value.ToString();
                needAPText.text = (cost * value).ToString();
            });

            var cost = GetCostOfStage();
            var actionPoint = Game.Game.instance.States.CurrentAvatarState.actionPoint;
            ownAPText.text = actionPoint.ToString();
            // Call onValueChanged by Change value
            apSlider.value = 0;
            apSlider.value = apSlider.maxValue = actionPoint / cost >= MaxBoostCount ? MaxBoostCount : actionPoint / cost;
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
            var worldMap = Find<WorldMap>();
            var worldId = worldMap.SelectedWorldId;
            var stageId = worldMap.SelectedStageId;

            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);

            Find<WorldMap>().Close(true);
            Find<StageInformation>().Close(true);
            Find<LoadingScreen>().Show();
            Close();

            _stage.IsInStage = true;
            _stage.IsShowHud = true;

            Game.Game.instance.ActionManager
                .HackAndSlash(
                    _costumes,
                    _equipments,
                    _consumables,
                    worldId,
                    stageId,
                    (int) apSlider.value
                )
                .Subscribe(
                    _ =>
                    {
                        LocalLayerModifier.ModifyAvatarActionPoint(
                            States.Instance.CurrentAvatarState.address, GetCostOfStage());
                    }, e => ActionRenderHandler.BackToMain(false, e))
                .AddTo(this);
        }

        private static int GetCostOfStage()
        {
            return Game.Game.instance
                .TableSheets.StageSheet.Values.FirstOrDefault(i =>
                    i.Id == Find<WorldMap>().SelectedStageId).CostAP;
        }
    }
}
