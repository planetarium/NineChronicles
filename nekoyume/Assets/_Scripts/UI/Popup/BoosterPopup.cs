using System.Collections.Generic;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
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
        private List<Costume> _costumes;
        private List<Equipment> _equipments;
        private List<Consumable> _consumables;

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

            ReactiveAvatarState.ActionPoint.Subscribe(value =>
            {
                apSlider.maxValue = value / 5;
                ownAPText.text = value.ToString();
            }).AddTo(gameObject);

            apSlider.onValueChanged.AddListener(value =>
            {
                boostCountText.text = value.ToString();
                needAPText.text = (Game.Game.instance
                    .TableSheets.StageSheet.Values.FirstOrDefault(i =>
                        i.Id == Find<WorldMap>().SelectedStageId).CostAP * value).ToString();
            });

            var actionPoint = Game.Game.instance.States.CurrentAvatarState.actionPoint;
            ownAPText.text = actionPoint.ToString();
            apSlider.value = apSlider.maxValue = actionPoint / 5;
            Show();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
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
                            States.Instance.CurrentAvatarState.address, Game.Game.instance
                                .TableSheets.StageSheet.Values.FirstOrDefault(
                                    i => i.Id == stageId).CostAP);
                    }, e => ActionRenderHandler.BackToMain(false, e))
                .AddTo(this);
        }
    }
}
