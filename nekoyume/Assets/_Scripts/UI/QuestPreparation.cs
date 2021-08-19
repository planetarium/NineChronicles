using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using mixpanel;
using Nekoyume.L10n;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    using UniRx;

    public class QuestPreparation : Widget
    {
        [SerializeField]
        private Module.Inventory inventory = null;

        [SerializeField]
        private TextMeshProUGUI consumableTitleText = null;

        // todo: `EquipmentSlot`을 사용하지 않든가, 이름을 바꿔야 하겠다. 또한 `EquipmentSlots`와 같이 `ConsumableSlots`를 만들어도 좋겠다.
        [SerializeField]
        private EquipmentSlot[] consumableSlots = null;

        [SerializeField]
        private DetailedStatView[] statusRows = null;

        [SerializeField]
        private TextMeshProUGUI equipmentTitleText = null;

        [SerializeField]
        private EquipmentSlots equipmentSlots = null;

        [SerializeField]
        private TextMeshProUGUI costumeTitleText = null;

        [SerializeField]
        private EquipmentSlots costumeSlots = null;

        [SerializeField]
        private Button questButton = null;

        [SerializeField]
        private GameObject equipSlotGlow = null;

        [SerializeField]
        private TextMeshProUGUI requiredPointText = null;

        [SerializeField]
        private ParticleSystem[] particles = null;

        [SerializeField]
        private TMP_InputField levelField = null;

        [SerializeField]
        private Button simulateButton = null;

        [Header("ItemMoveAnimation")]
        [SerializeField]
        private Image actionPointImage = null;

        [SerializeField]
        private Transform buttonStarImageTransform = null;

        [SerializeField]
        private Toggle repeatToggle;

        [SerializeField, Range(.5f, 3.0f)]
        private float animationTime = 1f;

        [SerializeField]
        private bool moveToLeft = false;

        [SerializeField, Range(0f, 10f),
         Tooltip("Gap between start position X and middle position X")]
        private float middleXGap = 1f;

        private Stage _stage;

        private Game.Character.Player _player;

        private EquipmentSlot _weaponSlot;

        private int _worldId;

        private readonly IntReactiveProperty _stageId = new IntReactiveProperty();

        private int _requiredCost;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private CharacterStats _tempStats;

        private bool _reset = true;

        // NOTE: questButton을 클릭한 후에 esc키를 눌러서 월드맵으로 벗어나는 것을 막는다.
        // 행동력이 _requiredCost 미만일 경우 퀘스트 버튼이 비활성화되므로 임시 방편으로 행동력도 비교함.
        public override bool CanHandleInputEvent =>
            base.CanHandleInputEvent &&
            (questButton.interactable || !EnoughToPlay);

        private bool EnoughToPlay =>
            States.Instance.CurrentAvatarState.actionPoint >= _requiredCost;

        #region override

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = null;
            simulateButton.gameObject.SetActive(GameConfig.IsEditor);
            levelField.gameObject.SetActive(GameConfig.IsEditor);
        }

        public override void Initialize()
        {
            base.Initialize();

            _weaponSlot = equipmentSlots.First(es => es.ItemSubType == ItemSubType.Weapon);

            inventory.SharedModel.DimmedFunc.Value = inventoryItem =>
                inventoryItem.ItemBase.Value.ItemType == ItemType.Material;
            inventory.SharedModel.SelectedItemView
                .Subscribe(SubscribeInventorySelectedItem)
                .AddTo(gameObject);
            inventory.SharedModel.State
                .Subscribe(inventoryState =>
                {
                    switch (inventoryState)
                    {
                        case ItemType.Consumable:
                            break;
                        case ItemType.Costume:
                            costumeSlots.gameObject.SetActive(true);
                            equipmentSlots.gameObject.SetActive(false);
                            break;
                        case ItemType.Equipment:
                            costumeSlots.gameObject.SetActive(false);
                            equipmentSlots.gameObject.SetActive(true);
                            break;
                        case ItemType.Material:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(inventoryState),
                                inventoryState, null);
                    }
                })
                .AddTo(gameObject);
            inventory.OnDoubleClickItemView
                .Subscribe(itemView =>
                {
                    if (itemView is null ||
                        itemView.Model is null ||
                        itemView.Model.Dimmed.Value)
                    {
                        return;
                    }

                    Equip(itemView.Model);
                })
                .AddTo(gameObject);
            inventory.OnResetItems.Subscribe(SubscribeInventoryResetItems).AddTo(gameObject);

            _stageId.Subscribe(SubscribeStage).AddTo(gameObject);

            questButton.OnClickAsObservable().Subscribe(_ => QuestClick(repeatToggle.isOn)).AddTo(gameObject);

            Game.Event.OnRoomEnter.AddListener(b => Close());

            foreach (var slot in equipmentSlots)
            {
                slot.ShowUnlockTooltip = true;
            }

            foreach (var slot in consumableSlots)
            {
                slot.ShowUnlockTooltip = true;
            }

            foreach (var slot in costumeSlots)
            {
                slot.ShowUnlockTooltip = true;
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            inventory.SharedModel.State.Value = ItemType.Equipment;

            consumableTitleText.text = L10nManager.Localize("UI_EQUIP_CONSUMABLES");
            costumeTitleText.text = equipmentTitleText.text = L10nManager.Localize("UI_EQUIP_EQUIPMENTS");

            Mixpanel.Track("Unity/Click Stage");
            _stage = Game.Game.instance.Stage;
            _stage.repeatStage = false;
            repeatToggle.isOn = false;
            repeatToggle.interactable = true;
            _stage.LoadBackground("dungeon_01");
            _player = _stage.GetPlayer(_stage.questPreparationPosition);
            if (_player is null)
            {
                throw new NotFoundComponentException<Game.Character.Player>();
            }

            if (_reset)
            {
                _reset = false;

                // stop run immediately.
                _player.gameObject.SetActive(false);
                _player.gameObject.SetActive(true);
                _player.SpineController.Appear();
                var currentAvatarState = Game.Game.instance.States.CurrentAvatarState;
                _player.Set(currentAvatarState);

                equipmentSlots.SetPlayerEquipments(_player.Model, ShowTooltip, Unequip);
                costumeSlots.SetPlayerCostumes(_player.Model, ShowTooltip, Unequip);
                // 인벤토리 아이템의 장착 여부를 `equipmentSlots`의 상태를 바탕으로 설정하기 때문에 `equipmentSlots.SetPlayer()`를 호출한 이후에 인벤토리 아이템의 장착 상태를 재설정한다.
                // 또한 인벤토리는 기본적으로 `OnEnable()` 단계에서 `OnResetItems` 이벤트를 일으키기 때문에 `equipmentSlots.SetPlayer()`와 호출 순서 커플링이 생기게 된다.
                // 따라서 강제로 상태를 설정한다.
                SubscribeInventoryResetItems(inventory);

                foreach (var consumableSlot in consumableSlots)
                {
                    consumableSlot.Set(_player.Level);
                }

                var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;
                _player.Model.SetCostumeStat(costumeStatSheet);
                var tuples = _player.Model.Stats.GetBaseAndAdditionalStats();
                var idx = 0;
                foreach (var (statType, value, additionalValue) in tuples)
                {
                    var info = statusRows[idx];
                    info.Show(statType, value, additionalValue);
                    ++idx;
                }
            }

            var worldMap = Find<WorldMap>();
            _worldId = worldMap.SelectedWorldId;
            _stageId.Value = worldMap.SelectedStageId;

            Find<BottomMenu>().Show(
                UINavigator.NavigationType.Back,
                SubscribeBackButtonClick,
                true,
                BottomMenu.ToggleableType.Mail,
                BottomMenu.ToggleableType.Quest,
                BottomMenu.ToggleableType.Chat,
                BottomMenu.ToggleableType.IllustratedBook);

            ReactiveAvatarState.ActionPoint
                .Subscribe(_ => ReadyToQuest(EnoughToPlay))
                .AddTo(_disposables);
            _tempStats = _player.Model.Stats.Clone() as CharacterStats;
            inventory.SharedModel.UpdateEquipmentNotification();
            questButton.gameObject.SetActive(true);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            _reset = true;
            Find<BottomMenu>().Close(ignoreCloseAnimation);

            foreach (var slot in consumableSlots)
            {
                slot.Clear();
            }

            equipmentSlots.Clear();
            costumeSlots.Clear();
            base.Close(ignoreCloseAnimation);
            _disposables.DisposeAllAndClear();
        }

        #endregion

        #region Tooltip

        private void ShowTooltip(InventoryItemView view)
        {
            var tooltip = Find<ItemInformationTooltip>();
            if (view is null ||
                view.RectTransform == tooltip.Target)
            {
                tooltip.Close();

                return;
            }

            tooltip.Show(
                view.RectTransform,
                view.Model,
                value => !view.Model.Dimmed.Value,
                view.Model.EquippedEnabled.Value
                    ? L10nManager.Localize("UI_UNEQUIP")
                    : L10nManager.Localize("UI_EQUIP"),
                _ => Equip(tooltip.itemInformation.Model.item.Value),
                _ =>
                {
                    equipSlotGlow.SetActive(false);
                    inventory.SharedModel.DeselectItemView();
                });
        }

        private void ShowTooltip(EquipmentSlot slot)
        {
            var tooltip = Find<ItemInformationTooltip>();
            if (slot is null ||
                slot.RectTransform == tooltip.Target)
            {
                tooltip.Close();

                return;
            }

            if (inventory.SharedModel.TryGetEquipment(slot.Item as Equipment, out var item) ||
                inventory.SharedModel.TryGetConsumable(slot.Item as Consumable, out item) ||
                inventory.SharedModel.TryGetCostume(slot.Item as Costume, out item))
            {
                tooltip.Show(
                    slot.RectTransform,
                    item,
                    _ => inventory.SharedModel.DeselectItemView());
            }
        }

        #endregion

        #region Subscribe

        private void SubscribeInventoryResetItems(Module.Inventory value)
        {
            if (_reset)
            {
                return;
            }

            inventory.SharedModel.EquippedEnabledFunc.SetValueAndForceNotify(inventoryItem =>
            {
                if (inventoryItem.ItemBase.Value.ItemType == ItemType.Costume &&
                    inventoryItem.ItemBase.Value is Costume costume)
                {
                    return costume.equipped;
                }

                return TryToFindSlotAlreadyEquip(inventoryItem.ItemBase.Value, out _);
            });

            inventory.SharedModel.UpdateEquipmentNotification();
        }

        private void SubscribeInventorySelectedItem(InventoryItemView view)
        {
            // Fix me. 이미 장착한 아이템일 경우 장착 버튼 비활성화 필요.
            // 현재는 왼쪽 부분인 인벤토리와 아이템 정보 부분만 뷰모델을 적용했는데, 오른쪽 까지 뷰모델이 확장되면 가능.
            if (view is null ||
                view.Model is null ||
                view.Model.Dimmed.Value ||
                !(view.Model.ItemBase.Value is ItemUsable))
            {
                HideGlowEquipSlot();
            }
            else
            {
                UpdateGlowEquipSlot((ItemUsable) view.Model.ItemBase.Value);
            }

            ShowTooltip(view);
        }

        private void SubscribeBackButtonClick(BottomMenu bottomMenu)
        {
            if (!CanClose)
            {
                return;
            }

            Find<WorldMap>().Show(_worldId, _stageId.Value, false);
            gameObject.SetActive(false);
        }

        private void ReadyToQuest(bool ready)
        {
            questButton.interactable = ready;
            requiredPointText.color = ready ? Color.white : Color.red;
            foreach (var particle in particles)
            {
                if (ready)
                {
                    particle.Play();
                }
                else
                {
                    particle.Stop();
                }
            }
        }

        private void SubscribeStage(int stageId)
        {
            var stage =
                Game.Game.instance.TableSheets.StageSheet.Values.FirstOrDefault(
                    i => i.Id == stageId);
            if (stage is null)
                return;
            _requiredCost = stage.CostAP;
            requiredPointText.text = _requiredCost.ToString();
        }

        #endregion

        private void QuestClick(bool repeat)
        {
            if (_stage.IsInStage)
            {
                questButton.interactable = false;
                return;
            }

            _stage.IsInStage = true;
            _stage.IsShowHud = true;
            StartCoroutine(CoQuestClick(repeat));
            questButton.interactable = false;
            repeatToggle.interactable = false;
        }

        private IEnumerator CoQuestClick(bool repeat)
        {
            var animation = ItemMoveAnimation.Show(actionPointImage.sprite,
                actionPointImage.transform.position,
                buttonStarImageTransform.position,
                Vector2.one,
                moveToLeft,
                true,
                animationTime,
                middleXGap);
            LocalLayerModifier.ModifyAvatarActionPoint(States.Instance.CurrentAvatarState.address,
                -_requiredCost);
            yield return new WaitWhile(() => animation.IsPlaying);
            Quest(repeat);
            AudioController.PlayClick();
        }

        #region slot

        private void Equip(CountableItem countableItem)
        {
            if (!(countableItem is InventoryItem inventoryItem))
            {
                return;
            }

            var itemBase = inventoryItem.ItemBase.Value;
            // 이미 장착중인 아이템이라면 해제한다.
            if (TryToFindSlotAlreadyEquip(itemBase, out var slot))
            {
                Unequip(slot);
                return;
            }

            // 아이템을 장착할 슬롯을 찾는다.
            if (!TryToFindSlotToEquip(itemBase, out slot))
            {
                return;
            }

            // 이미 슬롯에 아이템이 있다면 해제한다.
            if (!slot.IsEmpty)
            {
                if (inventory.SharedModel.TryGetEquipment(
                        slot.Item as Equipment,
                        out var inventoryItemToUnequip) ||
                    inventory.SharedModel.TryGetConsumable(
                        slot.Item as Consumable,
                        out inventoryItemToUnequip) ||
                    inventory.SharedModel.TryGetCostume(
                        slot.Item as Costume, out inventoryItemToUnequip))
                {
                    inventoryItemToUnequip.EquippedEnabled.Value = false;
                    LocalStateItemEquipModify(slot.Item, false);
                }
            }

            inventoryItem.EquippedEnabled.Value = true;
            slot.Set(itemBase, ShowTooltip, Unequip);
            LocalStateItemEquipModify(slot.Item, true);
            HideGlowEquipSlot();
            PostEquipOrUnequip(slot, slot.Item is Costume);
        }

        private void Unequip(EquipmentSlot slot)
        {
            if (_stage.IsInStage)
            {
                return;
            }

            if (slot.IsEmpty)
            {
                equipSlotGlow.SetActive(false);
                foreach (var item in inventory.SharedModel.Equipments)
                {
                    item.GlowEnabled.Value =
                        item.ItemBase.Value.ItemSubType == slot.ItemSubType;
                }

                return;
            }

            if (inventory.SharedModel.TryGetEquipment(
                    slot.Item as Equipment,
                    out var inventoryItem) ||
                inventory.SharedModel.TryGetConsumable(
                    slot.Item as Consumable,
                    out inventoryItem) ||
                inventory.SharedModel.TryGetCostume(
                    slot.Item as Costume,
                    out inventoryItem))
            {
                inventoryItem.EquippedEnabled.Value = false;
                LocalStateItemEquipModify(slot.Item, false);
            }

            if (slot.Item is Costume costume)
            {
                _player.UnequipCostume(costume);
            }
            slot.Clear();
            PostEquipOrUnequip(slot);
        }

        private static void LocalStateItemEquipModify(ItemBase itemBase, bool equip)
        {
            if (!(itemBase is INonFungibleItem nonFungibleItem))
            {
                return;
            }

            LocalLayerModifier.SetItemEquip(
                States.Instance.CurrentAvatarState.address,
                nonFungibleItem.NonFungibleId,
                equip);
        }

        private void PostEquipOrUnequip(EquipmentSlot slot, bool equipCostume = false)
        {
            UpdateStats();
            Find<ItemInformationTooltip>().Close();

            if (slot.ItemSubType == ItemSubType.Armor)
            {
                var armor = (Armor) slot.Item;
                var weapon = (Weapon) _weaponSlot.Item;
                _player.EquipEquipmentsAndUpdateCustomize(armor, weapon);
            }
            else if (slot.ItemSubType == ItemSubType.Weapon)
            {
                _player.EquipWeapon((Weapon) slot.Item);
            }
            else if (equipCostume)
            {
                _player.EquipCostume((Costume) slot.Item);
            }

            Game.Event.OnUpdatePlayerEquip.OnNext(_player);
            AudioController.instance.PlaySfx(slot.ItemSubType == ItemSubType.Food
                ? AudioController.SfxCode.ChainMail2
                : AudioController.SfxCode.Equipment);
            inventory.SharedModel.UpdateEquipmentNotification();
            Find<BottomMenu>().UpdateInventoryNotification();
        }

        private bool TryToFindSlotAlreadyEquip(ItemBase item, out EquipmentSlot slot)
        {
            switch (item.ItemType)
            {
                case ItemType.Consumable:
                    foreach (var consumableSlot in consumableSlots.Where(consumableSlot =>
                        !consumableSlot.IsLock && !consumableSlot.IsEmpty))
                    {
                        if (!consumableSlot.Item.Equals(item))
                            continue;

                        slot = consumableSlot;
                        return true;
                    }

                    slot = null;
                    return false;
                case ItemType.Equipment:
                    return equipmentSlots.TryGetAlreadyEquip(item, out slot);
                case ItemType.Costume:
                    return costumeSlots.TryGetAlreadyEquip(item, out slot);
                default:
                    slot = null;
                    return false;
            }
        }

        private bool TryToFindSlotToEquip(ItemBase item, out EquipmentSlot slot)
        {
            switch (item.ItemType)
            {
                case ItemType.Consumable:
                    slot = consumableSlots.FirstOrDefault(s => !s.IsLock && s.IsEmpty)
                           ?? consumableSlots[0];
                    return true;
                case ItemType.Equipment:
                    return equipmentSlots.TryGetToEquip((Equipment) item, out slot);
                case ItemType.Costume:
                    return costumeSlots.TryGetToEquip((Costume) item, out slot);
                default:
                    slot = null;
                    return false;
            }
        }

        private void UpdateGlowEquipSlot(ItemUsable itemUsable)
        {
            var itemType = itemUsable.ItemType;
            EquipmentSlot equipmentSlot;
            switch (itemType)
            {
                case ItemType.Consumable:
                case ItemType.Equipment:
                    TryToFindSlotToEquip(itemUsable, out equipmentSlot);
                    break;
                case ItemType.Material:
                    HideGlowEquipSlot();
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (equipmentSlot && equipmentSlot.transform.parent)
            {
                equipSlotGlow.transform.SetParent(equipmentSlot.transform);
                equipSlotGlow.transform.localPosition = Vector3.zero;
            }
            else
            {
                HideGlowEquipSlot();
            }
        }

        private void HideGlowEquipSlot()
        {
            equipSlotGlow.SetActive(false);
        }

        #endregion

        private void UpdateStats()
        {
            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item as Equipment)
                .Where(item => !(item is null))
                .ToList();
            var consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => slot.Item as Consumable)
                .Where(item => !(item is null))
                .ToList();

            var stats = _tempStats.SetAll(
                _tempStats.Level,
                equipments,
                consumables,
                Game.Game.instance.TableSheets.EquipmentItemSetEffectSheet
            );
            using (var enumerator = stats.GetBaseAndAdditionalStats().GetEnumerator())
            {
                foreach (var statView in statusRows)
                {
                    if (!enumerator.MoveNext())
                    {
                        break;
                    }

                    var (statType, baseValue, additionalValue) = enumerator.Current;
                    statView.Show(statType, baseValue, additionalValue);
                }
            }
        }

        private void Quest(bool repeat)
        {
            Find<BottomMenu>().Close(true);
            Find<LoadingScreen>().Show();

            questButton.gameObject.SetActive(false);
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);

            var costumes = _player.Costumes;

            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Equipment) slot.Item)
                .ToList();

            var consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Consumable) slot.Item)
                .ToList();

            _stage.isExitReserved = false;
            _stage.repeatStage = repeat;
            _stage.foodCount = consumables.Count;
            ActionRenderHandler.Instance.Pending = true;
            Game.Game.instance.ActionManager
                .HackAndSlash(
                    costumes,
                    equipments,
                    consumables,
                    _worldId,
                    _stageId.Value
                )
                .Subscribe(
                    _ =>
                    {
                        LocalLayerModifier.ModifyAvatarActionPoint(
                            States.Instance.CurrentAvatarState.address, _requiredCost);
                    }, e => ActionRenderHandler.BackToMain(false, e))
                .AddTo(this);
        }

        public void GoToStage(BattleLog battleLog)
        {
            Game.Event.OnStageStart.Invoke(battleLog);
            Find<LoadingScreen>().Close();
            Close(true);
        }

        public void SimulateBattle()
        {
            var level = States.Instance.CurrentAvatarState.level;
            if (!string.IsNullOrEmpty(levelField.text))
                level = int.Parse(levelField.text);
            // 레벨 범위가 넘어간 값이면 만렙으로 설정
            if (!Game.Game.instance.TableSheets.CharacterLevelSheet.ContainsKey(level))
            {
                level = Game.Game.instance.TableSheets.CharacterLevelSheet.Keys.Last();
            }

            Find<LoadingScreen>().Show();

            questButton.gameObject.SetActive(false);
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);

            var stageId = _stageId.Value;
            if (!Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(stageId,
                out var worldRow))
                throw new KeyNotFoundException(
                    $"WorldSheet.TryGetByStageId() {nameof(stageId)}({stageId})");

            var avatarState = new AvatarState(States.Instance.CurrentAvatarState) {level = level};
            List<Guid> consumables = consumableSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => ((Consumable) slot.Item).ItemId)
                .ToList();
            var equipments = equipmentSlots
                .Where(slot => !slot.IsLock && !slot.IsEmpty)
                .Select(slot => (Equipment) slot.Item)
                .ToList();
            var inventoryEquipments = avatarState.inventory.Items
                .Select(i => i.item)
                .OfType<Equipment>()
                .Where(i => i.equipped).ToList();

            foreach (var equipment in inventoryEquipments)
            {
                equipment.Unequip();
            }

            foreach (var equipment in equipments)
            {
                if (!avatarState.inventory.TryGetNonFungibleItem(equipment,
                    out ItemUsable outNonFungibleItem))
                {
                    continue;
                }

                ((Equipment) outNonFungibleItem).Equip();
            }

            var tableSheets = Game.Game.instance.TableSheets;
            var simulator = new StageSimulator(
                new Cheat.DebugRandom(),
                avatarState,
                consumables,
                worldRow.Id,
                stageId,
                tableSheets.GetStageSimulatorSheets(),
                tableSheets.CostumeStatSheet
            );
            simulator.Simulate();
            GoToStage(simulator.Log);
        }
    }
}
