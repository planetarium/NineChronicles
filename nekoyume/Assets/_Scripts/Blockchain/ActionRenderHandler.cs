using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.Model.State;
using Nekoyume.Model.Quest;
using Nekoyume.State.Modifiers;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Lib9c;
using Lib9c.Renderers;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using mixpanel;
using Nekoyume.Action.Garages;
using Nekoyume.Arena;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Market;
using Nekoyume.UI.Module.WorldBoss;
using Skill = Nekoyume.Model.Skill.Skill;

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions.Action;
#endif

namespace Nekoyume.Blockchain
{
    using Model;
    using UI.Scroller;
    using UniRx;

    /// <summary>
    /// 현상태 : 각 액션의 랜더 단계에서 즉시 게임 정보에 반영시킴. 아바타를 선택하지 않은 상태에서 이전에 성공시키지 못한 액션을 재수행하고
    ///       이를 핸들링하면, 즉시 게임 정보에 반영시길 수 없기 때문에 에러가 발생함.
    /// 참고 : 이후 언랜더 처리를 고려한 해법이 필요함.
    /// 해법 1: 랜더 단계에서 얻는 `eval` 자체 혹은 변경점을 queue에 넣고, 게임의 상태에 따라 꺼내 쓰도록.
    /// </summary>
    public class ActionRenderHandler : ActionHandler
    {
        private static class Singleton
        {
            internal static readonly ActionRenderHandler Value = new ActionRenderHandler();
        }

        public static ActionRenderHandler Instance => Singleton.Value;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private IDisposable _disposableForBattleEnd;

        private ActionRenderer _actionRenderer;

        // approximately 4h == 1200 block count
        private const int WorkshopNotifiedBlockCount = 0;

        private ActionRenderHandler()
        {
        }

        public override void Start(ActionRenderer renderer)
        {
            _actionRenderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

            Stop();
            _actionRenderer.BlockEndSubject.ObserveOnMainThread().Subscribe(_ =>
            {
                Debug.Log($"[{nameof(BlockRenderHandler)}] Render actions end");
            }).AddTo(_disposables);
            _actionRenderer.ActionRenderSubject.ObserveOnMainThread().Subscribe(eval =>
            {
                if (eval.Action is not GameAction gameAction)
                {
                    return;
                }

                if (ActionManager.Instance.TryPopActionEnqueuedDateTime(
                        gameAction.Id,
                        out var enqueuedDateTime))
                {
                    var actionType = gameAction.GetActionTypeAttribute();
                    var elapsed = (DateTime.Now - enqueuedDateTime).TotalSeconds;
                    var agentState = States.Instance.AgentState;
                    var currentAvatarState = States.Instance.CurrentAvatarState;
                    if (currentAvatarState is not null)
                    {
                        Analyzer.Instance.Track(
                            "Unity/ActionRender",
                            new Dictionary<string, Value>
                            {
                                ["ActionType"] = actionType.TypeIdentifier.Inspect(false),
                                ["Elapsed"] = elapsed,
                                ["AvatarAddress"] = currentAvatarState.address.ToString(),
                                ["AgentAddress"] = agentState.address.ToString(),
                            });
                    }
                }
            }).AddTo(_disposables);

            RewardGold();
            GameConfig();
            CreateAvatar();
            TransferAsset();
            Stake();

            // MeadPledge
            RequestPledge();
            ApprovePledge();

            // Battle
            HackAndSlash();
            MimisbrunnrBattle();
            HackAndSlashSweep();
            HackAndSlashRandomBuff();
            EventDungeonBattle();

            // Craft
            CombinationConsumable();
            CombinationEquipment();
            ItemEnhancement();
            RapidCombination();
            Grinding();
            EventConsumableItemCrafts();
            EventMaterialItemCrafts();

            // Market
            RegisterProduct();
            CancelProductRegistration();
            ReRegisterProduct();
            BuyProduct();

            // Consume
            DailyReward();
            RedeemCode();
            ChargeActionPoint();
            ClaimMonsterCollectionReward();
            ClaimStakeReward();

            // Crystal Unlocks
            UnlockEquipmentRecipe();
            UnlockWorld();

            // Arena
            InitializeArenaActions();

            // World Boss
            Raid();
            ClaimRaidReward();

            // Rune
            RuneEnhancement();
            UnlockRuneSlot();

            PetEnhancement();

            // GARAGE
            UnloadFromMyGarages();

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            Testbed();
            ManipulateState();
#endif
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private void RewardGold()
        {
            // FIXME RewardGold의 결과(ActionEvaluation)에서 다른 갱신 주소가 같이 나오고 있는데 더 조사해봐야 합니다.
            // 우선은 HasUpdatedAssetsForCurrentAgent로 다르게 검사해서 우회합니다.
            _actionRenderer.EveryRender<RewardGold>()
                .Where(HasUpdatedAssetsForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(eval => UpdateAgentStateAsync(eval).Forget())
                .AddTo(_disposables);
        }

        private void CreateAvatar()
        {
            _actionRenderer.EveryRender<CreateAvatar>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseCreateAvatar)
                .AddTo(_disposables);
        }

        private void HackAndSlash()
        {
            _actionRenderer.EveryRender<HackAndSlash>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseHackAndSlashAsync)
                .AddTo(_disposables);
        }

        private void MimisbrunnrBattle()
        {
            _actionRenderer.EveryRender<MimisbrunnrBattle>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseMimisbrunnrAsync)
                .AddTo(_disposables);
        }

        private void EventDungeonBattle()
        {
            _actionRenderer.EveryRender<EventDungeonBattle>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseEventDungeonBattleAsync)
                .AddTo(_disposables);
        }

        private void CombinationConsumable()
        {
            _actionRenderer.EveryRender<CombinationConsumable>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombinationConsumable)
                .AddTo(_disposables);
        }

        private void RegisterProduct()
        {
            _actionRenderer.EveryRender<RegisterProduct>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseRegisterProductAsync)
                .AddTo(_disposables);
        }

        private void CancelProductRegistration()
        {
            _actionRenderer.EveryRender<CancelProductRegistration>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseCancelProductRegistrationAsync)
                .AddTo(_disposables);
        }

        private void ReRegisterProduct()
        {
            _actionRenderer.EveryRender<ReRegisterProduct>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseReRegisterProduct)
                .AddTo(_disposables);
        }

        private void BuyProduct()
        {
            _actionRenderer.EveryRender<BuyProduct>()
                .ObserveOnMainThread()
                .Subscribe(ResponseBuyProduct)
                .AddTo(_disposables);
        }

        private void ItemEnhancement()
        {
            _actionRenderer.EveryRender<ItemEnhancement>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseItemEnhancement)
                .AddTo(_disposables);
        }

        private void DailyReward()
        {
            _actionRenderer.EveryRender<DailyReward>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseDailyRewardAsync)
                .AddTo(_disposables);
        }

        private void CombinationEquipment()
        {
            _actionRenderer.EveryRender<CombinationEquipment>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombinationEquipment)
                .AddTo(_disposables);
        }

        private void Grinding()
        {
            _actionRenderer.EveryRender<Grinding>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseGrinding)
                .AddTo(_disposables);
        }

        private void UnlockEquipmentRecipe()
        {
            _actionRenderer.EveryRender<UnlockEquipmentRecipe>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseUnlockEquipmentRecipeAsync)
                .AddTo(_disposables);
        }

        private void UnlockWorld()
        {
            _actionRenderer.EveryRender<UnlockWorld>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseUnlockWorld)
                .AddTo(_disposables);
        }

        private void HackAndSlashRandomBuff()
        {
            _actionRenderer.EveryRender<HackAndSlashRandomBuff>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseHackAndSlashRandomBuff)
                .AddTo(_disposables);
        }

        private void RapidCombination()
        {
            _actionRenderer.EveryRender<RapidCombination>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseRapidCombination)
                .AddTo(_disposables);
        }

        private void GameConfig()
        {
            _actionRenderer.EveryRender(GameConfigState.Address)
                .ObserveOnMainThread()
                .Subscribe(UpdateGameConfigState)
                .AddTo(_disposables);
        }

        private void RedeemCode()
        {
            _actionRenderer.EveryRender<Action.RedeemCode>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseRedeemCode)
                .AddTo(_disposables);
        }

        private void ChargeActionPoint()
        {
            _actionRenderer.EveryRender<ChargeActionPoint>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseChargeActionPoint)
                .AddTo(_disposables);
        }

        private void ClaimMonsterCollectionReward()
        {
            _actionRenderer.EveryRender<ClaimMonsterCollectionReward>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseClaimMonsterCollectionReward)
                .AddTo(_disposables);
        }

        private void TransferAsset()
        {
            _actionRenderer.EveryRender<TransferAsset>()
                .Where(eval =>
                    HasUpdatedAssetsForCurrentAgent(eval) || HasUpdatedAssetsForCurrentAvatar(eval))
                .ObserveOnMainThread()
                .Subscribe(ResponseTransferAsset)
                .AddTo(_disposables);
        }

        private void HackAndSlashSweep()
        {
            _actionRenderer.EveryRender<HackAndSlashSweep>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseHackAndSlashSweepAsync)
                .AddTo(_disposables);
        }

        private void Stake()
        {
            _actionRenderer.EveryRender<Stake>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseStake)
                .AddTo(_disposables);
        }

        private void ClaimStakeReward()
        {
            _actionRenderer.ActionRenderSubject
                .Where(ValidateEvaluationForCurrentAvatarState)
                .Where(eval => eval.Action is IClaimStakeReward)
                .ObserveOnMainThread()
                .Subscribe(ResponseClaimStakeReward)
                .AddTo(_disposables);
        }

        private void InitializeArenaActions()
        {
            _actionRenderer.EveryRender<JoinArena>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseJoinArenaAsync)
                .AddTo(_disposables);

            _actionRenderer.EveryRender<BattleArena>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseBattleArenaAsync)
                .AddTo(_disposables);
        }

        private void EventConsumableItemCrafts()
        {
            _actionRenderer.EveryRender<EventConsumableItemCrafts>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseEventConsumableItemCrafts)
                .AddTo(_disposables);
        }

        private void EventMaterialItemCrafts()
        {
            _actionRenderer.EveryRender<EventMaterialItemCrafts>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseEventMaterialItemCrafts)
                .AddTo(_disposables);
        }

        private void Raid()
        {
            _actionRenderer.EveryRender<Raid>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseRaidAsync)
                .AddTo(_disposables);
        }

        private void ClaimRaidReward()
        {
            _actionRenderer.EveryRender<ClaimRaidReward>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseClaimRaidRewardAsync)
                .AddTo(_disposables);
        }

        private void RuneEnhancement()
        {
            _actionRenderer.EveryRender<RuneEnhancement>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseRuneEnhancement)
                .AddTo(_disposables);
        }

        private void UnlockRuneSlot()
        {
            _actionRenderer.EveryRender<UnlockRuneSlot>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseUnlockRuneSlot)
                .AddTo(_disposables);
        }

        private void PetEnhancement()
        {
            _actionRenderer.EveryRender<PetEnhancement>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponsePetEnhancement)
                .AddTo(_disposables);
        }

        private void RequestPledge()
        {
            _actionRenderer.EveryRender<RequestPledge>()
                .Where(eval =>
                    HasUpdatedAssetsForCurrentAgent(eval) || HasUpdatedAssetsForCurrentAvatar(eval))
                .ObserveOnMainThread()
                .Subscribe(ResponseRequestPledge)
                .AddTo(_disposables);
        }

        private void ApprovePledge()
        {
            _actionRenderer.EveryRender<ApprovePledge>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseApprovePledge)
                .AddTo(_disposables);
        }

        private void UnloadFromMyGarages()
        {
            _actionRenderer.EveryRender<UnloadFromMyGarages>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    if (eval.Exception is not null)
                    {
                        Debug.Log(eval.Exception.Message);
                        return;
                    }

                    var gameStates = Game.Game.instance.States;
                    var agentAddr = gameStates.AgentState.address;
                    var avatarAddr = gameStates.CurrentAvatarState.address;
                    var states = eval.OutputState;
                    var action = eval.Action;
                    if (action.FungibleAssetValues is not null)
                    {
                        foreach (var (balanceAddr, value) in action.FungibleAssetValues)
                        {
                            var balance = states.GetBalance(
                                balanceAddr,
                                value.Currency);
                            if (balanceAddr.Equals(agentAddr))
                            {
                                if (value.Currency.Ticker == "NCG")
                                {
                                    AgentStateSubject.OnNextGold(value);
                                }
                                else if (value.Currency.Equals(Currencies.Crystal))
                                {
                                    AgentStateSubject.OnNextCrystal(value);
                                }
                                else if (value.Currency.Equals(Currencies.Garage))
                                {
                                    AgentStateSubject.OnNextGarage(value);
                                }
                            }
                            else if (balanceAddr.Equals(avatarAddr))
                            {
                                gameStates.SetCurrentAvatarBalance(balance);
                            }
                        }
                    }

                    if (action.FungibleIdAndCounts is not null)
                    {
                        var inventoryAddr = avatarAddr.Derive(SerializeKeys.LegacyInventoryKey);
                        var inventory = states.GetInventory(inventoryAddr);
                        gameStates.CurrentAvatarState.inventory = inventory;
                        ReactiveAvatarState.UpdateInventory(inventory);
                    }

                    var avatarValue = states.GetState(avatarAddr);
                    if (avatarValue is not Dictionary avatarDict)
                    {
                        Debug.LogError($"Failed to get avatar state: {avatarAddr}, {avatarValue}");
                        return;
                    }

                    if (!avatarDict.ContainsKey(SerializeKeys.MailBoxKey) ||
                        avatarDict[SerializeKeys.MailBoxKey] is not List mailBoxList)
                    {
                        Debug.LogError($"Failed to get mail box: {avatarAddr}");
                        return;
                    }

                    var mailBox = new MailBox(mailBoxList);
                    var mail = mailBox.OfType<UnloadFromMyGaragesRecipientMail>()
                        .FirstOrDefault(mail => mail.blockIndex == eval.BlockIndex);
                    if (mail is not null)
                    {
                        mail.New = true;
                        gameStates.CurrentAvatarState.mailBox = mailBox;
                        LocalLayerModifier.AddNewMail(avatarAddr, mail.id);
                    }
                    else
                    {
                        Debug.LogWarning($"Not found UnloadFromMyGaragesRecipientMail from " +
                                         $"the render context of UnloadFromMyGarages action.\n" +
                                         $"tx id: {eval.TxId}, action id: {eval.Action.Id}");
                    }
                })
                .AddTo(_disposables);
        }

        private async UniTaskVoid ResponseCreateAvatar(
            ActionEvaluation<CreateAvatar> eval)
        {
            if (eval.Exception != null)
            {
                return;
            }

            await UpdateAgentStateAsync(eval);
            await UpdateAvatarState(eval, eval.Action.index);
            var avatarState = await States.Instance.SelectAvatarAsync(eval.Action.index);
            await States.Instance.InitRuneStoneBalance();
            await States.Instance.InitSoulStoneBalance();
            await States.Instance.InitRuneStates();
            await States.Instance.InitItemSlotStates();
            await States.Instance.InitRuneSlotStates();

            RenderQuest(
                avatarState.address,
                avatarState.questList.completedQuestIds);

            var agentAddr = States.Instance.AgentState.address;
            var avatarAddr = Addresses.GetAvatarAddress(agentAddr, eval.Action.index);
            DialogPopup.DeleteDialogPlayerPrefs(avatarAddr);

            var loginDetail = Widget.Find<LoginDetail>();
            if (loginDetail && loginDetail.IsActive())
            {
                loginDetail.OnRenderCreateAvatar(eval);
            }
        }

        private void ResponseRapidCombination(ActionEvaluation<RapidCombination> eval)
        {
            if (eval.Exception is null)
            {
                var avatarAddress = eval.Action.avatarAddress;
                var slotIndex = eval.Action.slotIndex;
                var slotState = eval.OutputState.GetCombinationSlotState(avatarAddress, slotIndex);
                var result = (RapidCombination5.ResultModel)slotState.Result;
                foreach (var pair in result.cost)
                {
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value);
                }

                string formatKey;
                var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
                var avatarState = States.Instance.AvatarStates.Values
                    .FirstOrDefault(x => x.address == avatarAddress);
                if (avatarState is null)
                {
                    return;
                }

                var combinationSlotState = States.Instance.GetCombinationSlotState(
                    avatarState,
                    currentBlockIndex);
                var stateResult = combinationSlotState[slotIndex]?.Result;
                switch (stateResult)
                {
                    case CombinationConsumable5.ResultModel combineResultModel:
                    {
                        LocalLayerModifier.AddNewResultAttachmentMail(
                            avatarAddress,
                            combineResultModel.id,
                            currentBlockIndex);
                        if (combineResultModel.itemUsable is Equipment equipment)
                        {
                            var sheet = TableSheets.Instance.EquipmentItemSubRecipeSheetV2;
                            if (combineResultModel.subRecipeId.HasValue &&
                                sheet.TryGetValue(
                                    combineResultModel.subRecipeId.Value,
                                    out var subRecipeRow))
                            {
                                formatKey = equipment.optionCountFromCombination ==
                                            subRecipeRow.Options.Count
                                    ? "NOTIFICATION_COMBINATION_COMPLETE_GREATER"
                                    : "NOTIFICATION_COMBINATION_COMPLETE";
                            }
                            else
                            {
                                formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                            }
                        }
                        else
                        {
                            formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                        }

                        break;
                    }
                    case ItemEnhancement.ResultModel enhancementResultModel:
                    {
                        LocalLayerModifier.AddNewResultAttachmentMail(
                            avatarAddress,
                            enhancementResultModel.id,
                            currentBlockIndex);

                        switch (enhancementResultModel.enhancementResult)
                        {
                            case Action.ItemEnhancement.EnhancementResult.GreatSuccess:
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_GREATER";
                                break;
                            case Action.ItemEnhancement.EnhancementResult.Success:
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                                break;
                            case Action.ItemEnhancement.EnhancementResult.Fail:
                                Analyzer.Instance.Track("Unity/ItemEnhancement Failed",
                                    new Dictionary<string, Value>
                                    {
                                        ["GainedCrystal"] =
                                            (long)enhancementResultModel.CRYSTAL.MajorUnit,
                                        ["BurntNCG"] = (long)enhancementResultModel.gold,
                                        ["AvatarAddress"] =
                                            States.Instance.CurrentAvatarState.address.ToString(),
                                        ["AgentAddress"] =
                                            States.Instance.AgentState.address.ToString(),
                                    });
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_FAIL";
                                break;
                            default:
                                Debug.LogError(
                                    $"Unexpected result.enhancementResult: {enhancementResultModel.enhancementResult}");
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                                break;
                        }

                        break;
                    }
                    default:
                        Debug.LogError(
                            $"Unexpected state.Result: {stateResult}");
                        formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                        break;
                }

                var format = L10nManager.Localize(formatKey);
                NotificationSystem.CancelReserve(result.itemUsable.ItemId);
                NotificationSystem.Push(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    NotificationCell.NotificationType.Notification);

                UpdateCombinationSlotState(avatarAddress, slotIndex, slotState);
                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarStateAsync(eval).Forget();
                if (slotState.PetId.HasValue)
                {
                    UpdatePetState(avatarAddress, eval.OutputState, slotState.PetId.Value);
                }

                Widget.Find<CombinationSlotsPopup>().SetCaching(
                    avatarAddress,
                    eval.Action.slotIndex,
                    false);
            }
        }

        private void ResponseCombinationEquipment(
            ActionEvaluation<CombinationEquipment> eval)
        {
            if (eval.Action.payByCrystal)
            {
                Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(false);
            }

            if (eval.Exception is null)
            {
                var agentAddress = eval.Signer;
                var avatarAddress = eval.Action.avatarAddress;
                var slotIndex = eval.Action.slotIndex;
                var slot = eval.OutputState.GetCombinationSlotState(avatarAddress, slotIndex);
                var result = (CombinationConsumable5.ResultModel)slot.Result;

                if (!eval.OutputState.TryGetAvatarStateV2(
                        agentAddress,
                        avatarAddress,
                        out var avatarState,
                        out _))
                {
                    return;
                }

                LocalLayerModifier.ModifyAgentGold(agentAddress, result.gold);
                LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
                foreach (var pair in result.materials)
                {
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value);
                }

                LocalLayerModifier.RemoveItem(
                    avatarAddress,
                    result.itemUsable.ItemId,
                    result.itemUsable.RequiredBlockIndex,
                    1);
                LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

                var tableSheets = Game.Game.instance.TableSheets;
                var nextQuest = avatarState.questList?
                    .OfType<CombinationEquipmentQuest>()
                    .Where(x => !x.Complete)
                    .OrderBy(x => x.StageId)
                    .FirstOrDefault(x => tableSheets.EquipmentItemRecipeSheet.TryGetValue(
                        x.RecipeId,
                        out _));
                var hammerPointStateAddress =
                    Addresses.GetHammerPointStateAddress(avatarAddress, result.recipeId);
                var hammerPointState = new HammerPointState(hammerPointStateAddress,
                    eval.OutputState.GetState(hammerPointStateAddress) as List);

                UpdateCombinationSlotState(avatarAddress, slotIndex, slot);
                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarStateAsync(eval).Forget();
                RenderQuest(avatarAddress, avatarState.questList?.completedQuestIds);
                States.Instance.UpdateHammerPointStates(result.recipeId, hammerPointState);
                var action = eval.Action;
                if (action.petId.HasValue)
                {
                    UpdatePetState(avatarAddress, eval.OutputState, action.petId.Value);
                }

                if (nextQuest is not null)
                {
                    var isRecipeMatch = nextQuest.RecipeId == eval.Action.recipeId;
                    if (isRecipeMatch)
                    {
                        var celebratesPopup = Widget.Find<CelebratesPopup>();
                        celebratesPopup.Show(nextQuest);
                        celebratesPopup.OnDisableObservable
                            .First()
                            .Subscribe(_ =>
                            {
                                var menu = Widget.Find<Menu>();
                                if (menu.isActiveAndEnabled)
                                {
                                    menu.UpdateGuideQuest(avatarState);
                                }
                            });
                    }
                }

                // Notify
                string formatKey;
                if (result.itemUsable is Equipment equipment)
                {
                    if (eval.Action.subRecipeId.HasValue &&
                        TableSheets.Instance.EquipmentItemSubRecipeSheetV2.TryGetValue(
                            eval.Action.subRecipeId.Value,
                            out var row))
                    {
                        formatKey = equipment.optionCountFromCombination == row.Options.Count
                            ? "NOTIFICATION_COMBINATION_COMPLETE_GREATER"
                            : "NOTIFICATION_COMBINATION_COMPLETE";
                    }
                    else
                    {
                        formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                    }
                }
                else
                {
                    Debug.LogError(
                        $"[{nameof(ResponseCombinationEquipment)}] result.itemUsable is not Equipment");
                    formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                }

                var format = L10nManager.Localize(formatKey);
                NotificationSystem.Reserve(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    slot.UnlockBlockIndex,
                    result.itemUsable.ItemId);

                var blockCount = slot.UnlockBlockIndex - Game.Game.instance.Agent.BlockIndex;
                if (blockCount >= WorkshopNotifiedBlockCount)
                {
                    var expectedNotifiedTime =
                        BlockIndexExtensions.BlockToTimeSpan(Mathf.RoundToInt(blockCount * 1.15f));
                    var notificationText = L10nManager.Localize(
                        "PUSH_WORKSHOP_CRAFT_COMPLETE_CONTENT",
                        result.itemUsable.GetLocalizedNonColoredName(false));
                    PushNotifier.Push(
                        notificationText,
                        expectedNotifiedTime,
                        PushNotifier.PushType.Workshop);
                }
                // ~Notify

                Widget.Find<CombinationSlotsPopup>()
                    .SetCaching(avatarAddress, eval.Action.slotIndex, false);
            }
        }

        private void ResponseCombinationConsumable(
            ActionEvaluation<CombinationConsumable> eval)
        {
            if (eval.Exception is null)
            {
                var agentAddress = eval.Signer;
                var avatarAddress = eval.Action.avatarAddress;
                var slotIndex = eval.Action.slotIndex;
                var slot = eval.OutputState.GetCombinationSlotState(avatarAddress, slotIndex);
                var result = (CombinationConsumable5.ResultModel)slot.Result;
                var itemUsable = result.itemUsable;
                if (!eval.OutputState.TryGetAvatarStateV2(
                        agentAddress,
                        avatarAddress,
                        out var avatarState,
                        out _))
                {
                    return;
                }

                LocalLayerModifier.ModifyAgentGold(agentAddress, result.gold);
                LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
                foreach (var pair in result.materials)
                {
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value);
                }

                LocalLayerModifier.RemoveItem(
                    avatarAddress,
                    itemUsable.ItemId,
                    itemUsable.RequiredBlockIndex,
                    1);
                LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

                UpdateCombinationSlotState(avatarAddress, slotIndex, slot);
                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarStateAsync(eval).Forget();
                RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);

                // Notify
                var format = L10nManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
                NotificationSystem.Reserve(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    slot.UnlockBlockIndex,
                    result.itemUsable.ItemId);
                // ~Notify

                Widget.Find<CombinationSlotsPopup>()
                    .SetCaching(avatarAddress, eval.Action.slotIndex, false);
            }
        }

        private void ResponseEventConsumableItemCrafts(
            ActionEvaluation<EventConsumableItemCrafts> eval)
        {
            if (eval.Exception is not null)
            {
                return;
            }

            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.AvatarAddress;
            var slotIndex = eval.Action.SlotIndex;
            var slot = eval.OutputState.GetCombinationSlotState(avatarAddress, slotIndex);
            var result = (CombinationConsumable5.ResultModel)slot.Result;
            var itemUsable = result.itemUsable;

            LocalLayerModifier.ModifyAgentGold(agentAddress, result.gold);
            LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, result.actionPoint);
            foreach (var pair in result.materials)
            {
                LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value);
            }

            LocalLayerModifier.RemoveItem(
                avatarAddress,
                itemUsable.ItemId,
                itemUsable.RequiredBlockIndex,
                1);
            LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

            UpdateCombinationSlotState(avatarAddress, slotIndex, slot);
            UpdateAgentStateAsync(eval).Forget();
            UpdateCurrentAvatarStateAsync(eval).Forget();

            // Notify
            var format = L10nManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
            NotificationSystem.Reserve(
                MailType.Workshop,
                string.Format(format, result.itemUsable.GetLocalizedName()),
                slot.UnlockBlockIndex,
                result.itemUsable.ItemId);
            // ~Notify

            Widget.Find<CombinationSlotsPopup>()
                .SetCaching(avatarAddress, eval.Action.SlotIndex, false);
        }

        private void ResponseEventMaterialItemCrafts(
            ActionEvaluation<EventMaterialItemCrafts> eval)
        {
            if (eval.Exception is not null)
            {
                return;
            }

            var avatarAddress = eval.Action.AvatarAddress;
            var materialsToUse = eval.Action.MaterialsToUse;
            var recipe = TableSheets.Instance.EventMaterialItemRecipeSheet[
                eval.Action.EventMaterialItemRecipeId];
            var resultItem = ItemFactory.CreateMaterial(
                TableSheets.Instance.MaterialItemSheet,
                recipe.ResultMaterialItemId);

            foreach (var material in materialsToUse)
            {
                var id = TableSheets.Instance.MaterialItemSheet[material.Key].ItemId;
                LocalLayerModifier.AddItem(avatarAddress, id, material.Value);
            }

            UpdateAgentStateAsync(eval).Forget();
            UpdateCurrentAvatarStateAsync(eval).Forget();

            // Notify
            var format = L10nManager.Localize(
                "NOTIFICATION_COMBINATION_COMPLETE",
                resultItem.GetLocalizedName(false));
            NotificationSystem.Reserve(MailType.Workshop, format, 1, Guid.Empty);
            // ~Notify
        }

        private void ResponseItemEnhancement(ActionEvaluation<ItemEnhancement> eval)
        {
            Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(false);
            if (eval.Exception is null)
            {
                var agentAddress = eval.Signer;
                var avatarAddress = eval.Action.avatarAddress;
                var slotIndex = eval.Action.slotIndex;
                var slot = eval.OutputState.GetCombinationSlotState(avatarAddress, slotIndex);
                var result = (ItemEnhancement.ResultModel)slot.Result;
                var itemUsable = result.itemUsable;
                if (!eval.OutputState.TryGetAvatarStateV2(
                        agentAddress,
                        avatarAddress,
                        out var avatarState,
                        out _))
                {
                    return;
                }

                LocalLayerModifier.ModifyAgentGold(agentAddress, result.gold);
                LocalLayerModifier.ModifyAgentCrystalAsync(agentAddress, -result.CRYSTAL.MajorUnit)
                    .Forget();
                LocalLayerModifier.AddItem(
                    avatarAddress,
                    itemUsable.ItemId,
                    itemUsable.RequiredBlockIndex,
                    1);
                foreach (var tradableId in result.materialItemIdList)
                {
                    if (avatarState.inventory.TryGetNonFungibleItem(
                            tradableId,
                            out ItemUsable materialItem))
                    {
                        LocalLayerModifier.AddItem(
                            avatarAddress,
                            tradableId,
                            materialItem.RequiredBlockIndex,
                            1);
                    }
                }

                LocalLayerModifier.RemoveItem(
                    avatarAddress,
                    itemUsable.ItemId,
                    itemUsable.RequiredBlockIndex,
                    1);
                LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

                UpdateCombinationSlotState(avatarAddress, slotIndex, slot);
                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarStateAsync(eval).Forget();
                RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);

                // Notify
                string formatKey;
                switch (result.enhancementResult)
                {
                    case Action.ItemEnhancement.EnhancementResult.GreatSuccess:
                        formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_GREATER";
                        break;
                    case Action.ItemEnhancement.EnhancementResult.Success:
                        formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                        break;
                    case Action.ItemEnhancement.EnhancementResult.Fail:
                        Analyzer.Instance.Track("Unity/ItemEnhancement Failed",
                            new Dictionary<string, Value>
                            {
                                ["GainedCrystal"] = (long)result.CRYSTAL.MajorUnit,
                                ["BurntNCG"] = (long)result.gold,
                                ["AvatarAddress"] =
                                    States.Instance.CurrentAvatarState.address.ToString(),
                                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                            });
                        formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_FAIL";
                        break;
                    default:
                        Debug.LogError(
                            $"Unexpected result.enhancementResult: {result.enhancementResult}");
                        formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                        break;
                }

                var format = L10nManager.Localize(formatKey);
                NotificationSystem.Reserve(
                    MailType.Workshop,
                    string.Format(format, result.itemUsable.GetLocalizedName()),
                    slot.UnlockBlockIndex,
                    result.itemUsable.ItemId);

                var blockCount = slot.UnlockBlockIndex - Game.Game.instance.Agent.BlockIndex;
                if (blockCount >= WorkshopNotifiedBlockCount)
                {
                    var expectedNotifiedTime =
                        BlockIndexExtensions.BlockToTimeSpan(Mathf.RoundToInt(blockCount * 1.15f));
                    var notificationText = L10nManager.Localize(
                        "PUSH_WORKSHOP_UPGRADE_COMPLETE_CONTENT",
                        result.itemUsable.GetLocalizedNonColoredName(false));
                    PushNotifier.Push(
                        notificationText,
                        expectedNotifiedTime,
                        PushNotifier.PushType.Workshop);
                }
                // ~Notify

                var avatarSlotIndex = States.Instance.AvatarStates
                    .FirstOrDefault(x => x.Value.address == eval.Action.avatarAddress).Key;
                var itemSlotStates = States.Instance.ItemSlotStates[avatarSlotIndex];

                for (var i = 1; i < (int)BattleType.End; i++)
                {
                    var battleType = (BattleType)i;
                    var currentItemSlotState = States.Instance.CurrentItemSlotStates[battleType];
                    currentItemSlotState.Costumes.Remove(eval.Action.itemId);
                    currentItemSlotState.Equipments.Remove(eval.Action.itemId);

                    var itemSlotState = itemSlotStates[battleType];
                    itemSlotState.Costumes.Remove(eval.Action.itemId);
                    itemSlotState.Equipments.Remove(eval.Action.itemId);
                }

                Widget.Find<CombinationSlotsPopup>()
                    .SetCaching(avatarAddress, eval.Action.slotIndex, false);
            }
        }

        private async void ResponseRegisterProductAsync(ActionEvaluation<RegisterProduct> eval)
        {
            if (eval.Exception is not null)
            {
                var asset = eval.Action.RegisterInfos.FirstOrDefault();
                if (asset is not AssetInfo assetInfo)
                {
                    return;
                }

                await States.Instance.SetBalanceAsync(assetInfo.Asset.Currency.Ticker);
                var shopSell = Widget.Find<ShopSell>();
                if (shopSell.isActiveAndEnabled)
                {
                    shopSell.UpdateInventory();
                }

                return;
            }

            if (eval.Action.ChargeAp)
            {
                var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                    .First(r => r.ItemSubType == ItemSubType.ApStone);
                LocalLayerModifier.AddItem(eval.Action.AvatarAddress, row.ItemId);
            }

            if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.AvatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(eval.Action.AvatarAddress);
            }

            var info = eval.Action.RegisterInfos.FirstOrDefault();
            if (info is null)
            {
                return;
            }

            var count = 1;
            var itemName = string.Empty;
            switch (info)
            {
                case RegisterInfo registerInfo:
                    count = registerInfo.ItemCount;
                    var rand = new LocalRandom(eval.RandomSeed);
                    var productId = rand.GenerateRandomGuid();
                    var deriveAddress = Product.DeriveAddress(productId);
                    eval.OutputState.TryGetState(deriveAddress, out List rawState);
                    var product = ProductFactory.DeserializeProduct(rawState);
                    if (product is not ItemProduct itemProduct)
                    {
                        return;
                    }

                    if (itemProduct.TradableItem is not ItemBase item)
                    {
                        return;
                    }

                    itemName = item.GetLocalizedName();
                    var slotIndex = States.Instance.AvatarStates
                        .FirstOrDefault(x => x.Value.address == registerInfo.AvatarAddress).Key;
                    var itemSlotStates = States.Instance.ItemSlotStates[slotIndex];

                    for (var i = 1; i < (int)BattleType.End; i++)
                    {
                        var battleType = (BattleType)i;
                        var currentItemSlotState =
                            States.Instance.CurrentItemSlotStates[battleType];
                        currentItemSlotState.Costumes.Remove(registerInfo.TradableId);
                        currentItemSlotState.Equipments.Remove(registerInfo.TradableId);

                        var itemSlotState = itemSlotStates[battleType];
                        itemSlotState.Costumes.Remove(registerInfo.TradableId);
                        itemSlotState.Equipments.Remove(registerInfo.TradableId);
                    }

                    break;
                case AssetInfo assetInfo:
                    await States.Instance.SetBalanceAsync(assetInfo.Asset.Currency.Ticker);
                    itemName = assetInfo.Asset.GetLocalizedName();
                    count = Convert.ToInt32(assetInfo.Asset.GetQuantityString());
                    break;
            }

            UpdateCurrentAvatarStateAsync(eval).Forget();
            await ReactiveShopState.RequestSellProductsAsync();

            string message;
            if (count > 1)
            {
                message = string.Format(
                    L10nManager.Localize("NOTIFICATION_MULTIPLE_SELL_COMPLETE"),
                    itemName,
                    count);
            }
            else
            {
                message = string.Format(L10nManager.Localize("NOTIFICATION_SELL_COMPLETE"),
                    itemName);
            }

            OneLineSystem.Push(
                MailType.Auction,
                message,
                NotificationCell.NotificationType.Information);
        }

        private async void ResponseCancelProductRegistrationAsync(
            ActionEvaluation<CancelProductRegistration> eval)
        {
            if (eval.Exception is not null)
            {
                return;
            }

            if (eval.Action.ChargeAp)
            {
                var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                    .First(r => r.ItemSubType == ItemSubType.ApStone);
                LocalLayerModifier.AddItem(eval.Action.AvatarAddress, row.ItemId);
            }

            if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.AvatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(eval.Action.AvatarAddress);
            }

            string message;
            var productInfos = eval.Action.ProductInfos;
            if (productInfos.Count > 1)
            {
                message = L10nManager.Localize("NOTIFICATION_CANCELREGISTER_ALL_COMPLETE");
            }
            else
            {
                var productInfo = productInfos.FirstOrDefault();
                var (itemName, itemProduct, favProduct) =
                    await Game.Game.instance.MarketServiceClient.GetProductInfo(
                        productInfo.ProductId);
                var count = 0;
                if (itemProduct is not null)
                {
                    count = (int)itemProduct.Quantity;
                }

                if (favProduct is not null)
                {
                    count = (int)favProduct.Quantity;
                    await States.Instance.SetBalanceAsync(favProduct.Ticker);
                }

                LocalLayerModifier.AddNewMail(eval.Action.AvatarAddress, productInfo.ProductId);
                if (count > 1)
                {
                    message = string.Format(
                        L10nManager.Localize("NOTIFICATION_MULTIPLE_SELL_CANCEL_COMPLETE"),
                        itemName,
                        count);
                }
                else
                {
                    message = string.Format(
                        L10nManager.Localize("NOTIFICATION_SELL_CANCEL_COMPLETE"),
                        itemName);
                }
            }

            OneLineSystem.Push(
                MailType.Auction,
                message,
                NotificationCell.NotificationType.Information);
            UpdateCurrentAvatarStateAsync(eval).Forget();
            await ReactiveShopState.RequestSellProductsAsync();
        }

        private async void ResponseReRegisterProduct(ActionEvaluation<ReRegisterProduct> eval)
        {
            if (eval.Exception is not null)
            {
                return;
            }

            if (eval.Action.ChargeAp)
            {
                var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                    .First(r => r.ItemSubType == ItemSubType.ApStone);
                LocalLayerModifier.AddItem(eval.Action.AvatarAddress, row.ItemId);
            }

            if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.AvatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(eval.Action.AvatarAddress);
            }

            string message;
            if (eval.Action.ReRegisterInfos.Count() > 1)
            {
                message = L10nManager.Localize("NOTIFICATION_REREGISTER_ALL_COMPLETE");
            }
            else
            {
                var (productInfo, _) = eval.Action.ReRegisterInfos.FirstOrDefault();
                var (itemName, itemProduct, favProduct) =
                    await Game.Game.instance.MarketServiceClient.GetProductInfo(
                        productInfo.ProductId);
                var count = 0;
                if (itemProduct is not null)
                {
                    count = (int)itemProduct.Quantity;
                }

                if (favProduct is not null)
                {
                    count = (int)favProduct.Quantity;
                }

                if (count > 1)
                {
                    message = string.Format(
                        L10nManager.Localize("NOTIFICATION_MULTIPLE_REREGISTER_COMPLETE"),
                        itemName, count);
                }
                else
                {
                    message = string.Format(
                        L10nManager.Localize("NOTIFICATION_REREGISTER_COMPLETE"), itemName);
                }
            }

            OneLineSystem.Push(
                MailType.Auction,
                message,
                NotificationCell.NotificationType.Information);
            UpdateCurrentAvatarStateAsync(eval).Forget();
            await ReactiveShopState.RequestSellProductsAsync();
        }

        private async void ResponseBuyProduct(ActionEvaluation<BuyProduct> eval)
        {
            if (eval.Exception is not null)
            {
                return;
            }

            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var productInfos = eval.Action.ProductInfos;
            if (eval.Action.AvatarAddress == avatarAddress) // buyer
            {
                foreach (var info in productInfos)
                {
                    var (itemName, itemProduct, favProduct) =
                        await Game.Game.instance.MarketServiceClient.GetProductInfo(info.ProductId);
                    var count = 0;
                    if (itemProduct is not null)
                    {
                        count = (int)itemProduct.Quantity;
                    }

                    if (favProduct is not null)
                    {
                        count = (int)favProduct.Quantity;
                        await States.Instance.SetBalanceAsync(favProduct.Ticker);
                    }

                    var price = info.Price;
                    LocalLayerModifier.ModifyAgentGoldAsync(agentAddress, price).Forget();
                    LocalLayerModifier.AddNewMail(avatarAddress, info.ProductId);

                    string message;
                    if (count > 1)
                    {
                        message = string.Format(
                            L10nManager.Localize("NOTIFICATION_MULTIPLE_BUY_BUYER_COMPLETE"),
                            itemName, price, count);
                    }
                    else
                    {
                        message = string.Format(
                            L10nManager.Localize("NOTIFICATION_BUY_BUYER_COMPLETE"),
                            itemName, price);
                    }

                    OneLineSystem.Push(
                        MailType.Auction,
                        message,
                        NotificationCell.NotificationType.Notification);
                }
            }
            else // seller
            {
                foreach (var info in productInfos)
                {
                    var buyerNameWithHash = $"#{eval.Action.AvatarAddress.ToHex()[..4]}";
                    var (itemName, itemProduct, favProduct) =
                        await Game.Game.instance.MarketServiceClient.GetProductInfo(info.ProductId);
                    var count = 0;
                    if (itemProduct is not null)
                    {
                        count = (int)itemProduct.Quantity;
                    }

                    if (favProduct is not null)
                    {
                        count = (int)favProduct.Quantity;
                        await States.Instance.SetBalanceAsync(favProduct.Ticker);
                    }

                    var taxedPrice = info.Price.DivRem(100, out _) * Buy.TaxRate;
                    LocalLayerModifier.ModifyAgentGoldAsync(agentAddress, -taxedPrice).Forget();
                    LocalLayerModifier.AddNewMail(avatarAddress, info.ProductId);

                    string message;
                    if (count > 1)
                    {
                        message = string.Format(
                            L10nManager.Localize("NOTIFICATION_MULTIPLE_BUY_SELLER_COMPLETE"),
                            buyerNameWithHash,
                            itemName,
                            count);
                    }
                    else
                    {
                        message = string.Format(
                            L10nManager.Localize("NOTIFICATION_BUY_SELLER_COMPLETE"),
                            buyerNameWithHash,
                            itemName);
                    }

                    OneLineSystem.Push(MailType.Auction, message,
                        NotificationCell.NotificationType.Notification);
                }
            }

            UpdateAgentStateAsync(eval).Forget();
            UpdateCurrentAvatarStateAsync(eval).Forget();
            RenderQuest(avatarAddress,
                States.Instance.CurrentAvatarState.questList.completedQuestIds);
        }

        private async void ResponseDailyRewardAsync(ActionEvaluation<DailyReward> eval)
        {
            if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.avatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(eval.Action.avatarAddress);
            }

            if (eval.Exception is null &&
                eval.Action.avatarAddress == States.Instance.CurrentAvatarState.address)
            {
                await States.Instance.InitRuneStoneBalance();
                LocalLayer.Instance.ClearAvatarModifiers<AvatarDailyRewardReceivedIndexModifier>(
                    eval.Action.avatarAddress);
                UpdateCurrentAvatarStateAsync(eval).Forget();
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_RECEIVED_DAILY_REWARD"),
                    NotificationCell.NotificationType.Notification);
                var expectedNotifiedTime = BlockIndexExtensions.BlockToTimeSpan(Mathf.RoundToInt(
                    States.Instance.GameConfigState.DailyRewardInterval * 1.15f));
                var notificationText = L10nManager.Localize("PUSH_PROSPERITY_METER_CONTENT");
                PushNotifier.Push(
                    notificationText,
                    expectedNotifiedTime,
                    PushNotifier.PushType.Reward);

                if (!RuneFrontHelper.TryGetRuneData(
                        RuneHelper.DailyRewardRune.Ticker,
                        out var data))
                {
                    return;
                }

                var runeName = L10nManager.Localize($"RUNE_NAME_{data.id}");
                var amount = States.Instance.GameConfigState.DailyRuneRewardAmount;
                NotificationSystem.Push(
                    MailType.System,
                    $" {L10nManager.Localize("OBTAIN")} : {runeName} x {amount}",
                    NotificationCell.NotificationType.RuneAcquisition);
            }
        }

        private async void ResponseHackAndSlashAsync(ActionEvaluation<HackAndSlash> eval)
        {
            if (eval.Exception is null)
            {
                await Task.WhenAll(
                    States.Instance.UpdateItemSlotStates(BattleType.Adventure),
                    States.Instance.UpdateRuneSlotStates(BattleType.Adventure));

                if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
                {
                    return;
                }

                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd =
                    Game.Game.instance.Stage.onEnterToStageEnd
                        .First()
                        .Subscribe(_ =>
                        {
                            var task = UniTask.Run(async () =>
                            {
                                await UpdateCurrentAvatarStateAsync(eval);
                                UpdateCrystalRandomSkillState(eval);
                                var avatarState = States.Instance.CurrentAvatarState;
                                RenderQuest(
                                    eval.Action.AvatarAddress,
                                    avatarState.questList.completedQuestIds);
                                _disposableForBattleEnd = null;
                                Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                            });
                            task.ToObservable()
                                .First()
                                // ReSharper disable once ConvertClosureToMethodGroup
                                .DoOnError(e => Debug.LogException(e));
                        });

                var tableSheets = TableSheets.Instance;
                var skillsOnWaveStart = new List<Skill>();
                if (eval.Action.StageBuffId.HasValue)
                {
                    var skill = CrystalRandomSkillState.GetSkill(
                        eval.Action.StageBuffId.Value,
                        tableSheets.CrystalRandomBuffSheet,
                        tableSheets.SkillSheet);
                    skillsOnWaveStart.Add(skill);
                }

                var tempPlayer =
                    new AvatarState((Dictionary)States.Instance.CurrentAvatarState.Serialize());
                var resultModel = eval.GetHackAndSlashReward(
                    tempPlayer,
                    States.Instance.GetEquippedRuneStates(BattleType.Adventure),
                    skillsOnWaveStart,
                    tableSheets,
                    out var simulator,
                    out var temporaryAvatar);
                var log = simulator.Log;
                Game.Game.instance.Stage.PlayCount = eval.Action.TotalPlayCount;
                Game.Game.instance.Stage.StageType = StageType.HackAndSlash;
                if (eval.Action.TotalPlayCount > 1)
                {
                    Widget.Find<BattleResultPopup>().ModelForMultiHackAndSlash = resultModel;
                    if (log.IsClear)
                    {
                        var currentAvatar = States.Instance.CurrentAvatarState;
                        currentAvatar.exp = temporaryAvatar.exp;
                        currentAvatar.level = temporaryAvatar.level;
                        currentAvatar.inventory = temporaryAvatar.inventory;
                        currentAvatar.monsterMap = temporaryAvatar.monsterMap;
                        currentAvatar.eventMap = temporaryAvatar.eventMap;
                    }
                }

                if (eval.Action.StageBuffId.HasValue)
                {
                    Analyzer.Instance.Track("Unity/Use Crystal Bonus Skill",
                        new Dictionary<string, Value>
                        {
                            ["RandomSkillId"] = eval.Action.StageBuffId,
                            ["IsCleared"] = simulator.Log.IsClear,
                            ["AvatarAddress"] =
                                States.Instance.CurrentAvatarState.address.ToString(),
                            ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                        });
                }

                if (Widget.Find<LoadingScreen>().IsActive())
                {
                    if (Widget.Find<BattlePreparation>().IsActive())
                    {
                        Widget.Find<BattlePreparation>().GoToStage(log);
                    }
                    else if (Widget.Find<Menu>().IsActive())
                    {
                        Widget.Find<Menu>().GoToStage(log);
                    }
                }
                else if (Widget.Find<StageLoadingEffect>().IsActive() &&
                         Widget.Find<BattleResultPopup>().IsActive())
                {
                    Widget.Find<BattleResultPopup>().NextStage(log);
                }
            }
            else
            {
                var showLoadingScreen = false;
                if (Widget.Find<StageLoadingEffect>().IsActive())
                {
                    Widget.Find<StageLoadingEffect>().Close();
                }

                if (Widget.Find<BattleResultPopup>().IsActive())
                {
                    showLoadingScreen = true;
                    Widget.Find<BattleResultPopup>().Close();
                }

                Game.Game.BackToMainAsync(eval.Exception.InnerException, showLoadingScreen)
                    .Forget();
            }
        }

        private async void ResponseHackAndSlashSweepAsync(
            ActionEvaluation<HackAndSlashSweep> eval)
        {
            if (eval.Exception is null)
            {
                Widget.Find<SweepResultPopup>().OnActionRender(new LocalRandom(eval.RandomSeed));
                if (eval.Action.apStoneCount > 0)
                {
                    var avatarAddress = eval.Action.avatarAddress;
                    LocalLayerModifier.ModifyAvatarActionPoint(
                        avatarAddress,
                        eval.Action.actionPoint);
                    var row = TableSheets.Instance.MaterialItemSheet.Values.First(r =>
                        r.ItemSubType == ItemSubType.ApStone);
                    LocalLayerModifier.AddItem(avatarAddress, row.ItemId, eval.Action.apStoneCount);
                }

                await UpdateCurrentAvatarStateAsync();
                await Task.WhenAll(
                    States.Instance.UpdateItemSlotStates(BattleType.Adventure),
                    States.Instance.UpdateRuneSlotStates(BattleType.Adventure));
                Widget.Find<BattlePreparation>().UpdateInventoryView();
            }
            else
            {
                Widget.Find<SweepResultPopup>().Close();
                Game.Game.BackToMainAsync(eval.Exception.InnerException).Forget();
            }
        }

        private async void ResponseMimisbrunnrAsync(
            ActionEvaluation<MimisbrunnrBattle> eval)
        {
            if (eval.Exception is null)
            {
                if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
                {
                    return;
                }

                await Task.WhenAll(
                    States.Instance.UpdateItemSlotStates(BattleType.Adventure),
                    States.Instance.UpdateRuneSlotStates(BattleType.Adventure));

                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd =
                    Game.Game.instance.Stage.onEnterToStageEnd
                        .First()
                        .Subscribe(_ =>
                        {
                            var task = UniTask.Run(() =>
                            {
                                UpdateCurrentAvatarStateAsync(eval).Forget();
                                var avatarState = States.Instance.CurrentAvatarState;
                                RenderQuest(
                                    eval.Action.AvatarAddress,
                                    avatarState.questList.completedQuestIds);
                                _disposableForBattleEnd = null;
                                Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                            });
                            task.ToObservable()
                                .First()
                                // ReSharper disable once ConvertClosureToMethodGroup
                                .DoOnError(e => Debug.LogException(e));
                        });

                var sheets = TableSheets.Instance;
                var stageRow = sheets.StageSheet[eval.Action.StageId];
                var avatarState = States.Instance.CurrentAvatarState;
                var runeStates = States.Instance.GetEquippedRuneStates(BattleType.Adventure);
                var localRandom = new LocalRandom(eval.RandomSeed);
                var simulator = new StageSimulator(
                    localRandom,
                    avatarState,
                    eval.Action.Foods,
                    runeStates,
                    new List<Skill>(),
                    eval.Action.WorldId,
                    eval.Action.StageId,
                    stageRow,
                    sheets.StageWaveSheet[eval.Action.StageId],
                    avatarState.worldInformation.IsStageCleared(eval.Action.StageId),
                    0,
                    sheets.GetStageSimulatorSheets(),
                    sheets.EnemySkillSheet,
                    sheets.CostumeStatSheet,
                    StageSimulatorV2.GetWaveRewards(
                        localRandom,
                        stageRow,
                        sheets.MaterialItemSheet,
                        eval.Action.PlayCount)
                );
                simulator.Simulate();
                BattleLog log = simulator.Log;
                Game.Game.instance.Stage.PlayCount = eval.Action.PlayCount;
                Game.Game.instance.Stage.StageType = StageType.Mimisbrunnr;

                if (Widget.Find<LoadingScreen>().IsActive())
                {
                    if (Widget.Find<BattlePreparation>().IsActive())
                    {
                        Widget.Find<BattlePreparation>().GoToStage(log);
                    }
                    else if (Widget.Find<Menu>().IsActive())
                    {
                        Widget.Find<Menu>().GoToStage(log);
                    }
                }
                else if (Widget.Find<StageLoadingEffect>().IsActive() &&
                         Widget.Find<BattleResultPopup>().IsActive())
                {
                    Widget.Find<BattleResultPopup>().NextMimisbrunnrStage(log);
                }
            }
            else
            {
                var showLoadingScreen = false;
                if (Widget.Find<StageLoadingEffect>().IsActive())
                {
                    Widget.Find<StageLoadingEffect>().Close();
                }

                if (Widget.Find<BattleResultPopup>().IsActive())
                {
                    showLoadingScreen = true;
                    Widget.Find<BattleResultPopup>().Close();
                }

                Game.Game.BackToMainAsync(eval.Exception.InnerException, showLoadingScreen)
                    .Forget();
            }
        }

        private async void ResponseEventDungeonBattleAsync(
            ActionEvaluation<EventDungeonBattle> eval)
        {
            if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
            {
                return;
            }

            if (eval.Exception is not null)
            {
                var showLoadingScreen = false;
                if (Widget.Find<StageLoadingEffect>().IsActive())
                {
                    Widget.Find<StageLoadingEffect>().Close();
                }

                if (Widget.Find<BattleResultPopup>().IsActive())
                {
                    showLoadingScreen = true;
                    Widget.Find<BattleResultPopup>().Close();
                }

                Game.Game.BackToMainAsync(eval.Exception.InnerException, showLoadingScreen)
                    .Forget();
                return;
            }

            await Task.WhenAll(
                States.Instance.UpdateItemSlotStates(BattleType.Adventure),
                States.Instance.UpdateRuneSlotStates(BattleType.Adventure));

            if (eval.Action.BuyTicketIfNeeded)
            {
                UpdateAgentStateAsync(eval).Forget();
            }

            _disposableForBattleEnd?.Dispose();
            _disposableForBattleEnd =
                Game.Game.instance.Stage.onEnterToStageEnd
                    .First()
                    .Subscribe(_ =>
                    {
                        var task = UniTask.Run(() =>
                        {
                            UpdateCurrentAvatarStateAsync(eval).Forget();
                            RxProps.EventDungeonInfo.UpdateAsync().Forget();
                            _disposableForBattleEnd = null;
                            Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                        });
                        task.ToObservable()
                            .First()
                            // ReSharper disable once ConvertClosureToMethodGroup
                            .DoOnError(e => Debug.LogException(e));
                    });

            var playCount = Action.EventDungeonBattle.PlayCount;
            // NOTE: This is a temporary solution. The formula is not yet decided.
            var random = new LocalRandom(eval.RandomSeed);
            var stageId = eval.Action.EventDungeonStageId;
            var stageRow = TableSheets.Instance.EventDungeonStageSheet[stageId];
            var simulator = new StageSimulatorV2(
                random,
                States.Instance.CurrentAvatarState,
                eval.Action.Foods,
                new List<Skill>(),
                eval.Action.EventDungeonId,
                stageId,
                stageRow,
                TableSheets.Instance.EventDungeonStageWaveSheet[stageId],
                RxProps.EventDungeonInfo.Value?.IsCleared(stageId) ?? false,
                RxProps.EventScheduleRowForDungeon.Value.GetStageExp(
                    stageId.ToEventDungeonStageNumber(),
                    Action.EventDungeonBattle.PlayCount),
                TableSheets.Instance.GetSimulatorSheetsV1(),
                TableSheets.Instance.EnemySkillSheet,
                TableSheets.Instance.CostumeStatSheet,
                StageSimulatorV2.GetWaveRewards(
                    random,
                    stageRow,
                    TableSheets.Instance.MaterialItemSheet,
                    Action.EventDungeonBattle.PlayCount));
            simulator.Simulate();
            var log = simulator.Log;
            var stage = Game.Game.instance.Stage;
            stage.StageType = StageType.EventDungeon;
            stage.PlayCount = playCount;

            if (Widget.Find<LoadingScreen>().IsActive())
            {
                if (Widget.Find<BattlePreparation>().IsActive())
                {
                    Widget.Find<BattlePreparation>().GoToStage(log);
                }
                else if (Widget.Find<Menu>().IsActive())
                {
                    Widget.Find<Menu>().GoToStage(log);
                }
            }
            else if (Widget.Find<StageLoadingEffect>().IsActive() &&
                     Widget.Find<BattleResultPopup>().IsActive())
            {
                Widget.Find<BattleResultPopup>().NextStage(log);
            }
        }

        private void ResponseRedeemCode(ActionEvaluation<Action.RedeemCode> eval)
        {
            var key = "UI_REDEEM_CODE_INVALID_CODE";
            if (eval.Exception is null)
            {
                Widget.Find<CodeRewardPopup>().Show(
                    eval.Action.Code,
                    eval.OutputState.GetRedeemCodeState());
                key = "UI_REDEEM_CODE_SUCCESS";
                UpdateCurrentAvatarStateAsync(eval).Forget();
                var msg = L10nManager.Localize(key);
                NotificationSystem.Push(
                    MailType.System,
                    msg,
                    NotificationCell.NotificationType.Information);
            }
            else
            {
                if (eval.Exception.InnerException is DuplicateRedeemException)
                {
                    key = "UI_REDEEM_CODE_ALREADY_USE";
                }

                var msg = L10nManager.Localize(key);
                NotificationSystem.Push(
                    MailType.System,
                    msg,
                    NotificationCell.NotificationType.Alert);
            }
        }

        private void ResponseChargeActionPoint(ActionEvaluation<ChargeActionPoint> eval)
        {
            if (eval.Exception is null)
            {
                var avatarAddress = eval.Action.avatarAddress;
                LocalLayerModifier.ModifyAvatarActionPoint(
                    avatarAddress,
                    -States.Instance.GameConfigState.ActionPointMax);
                var row = TableSheets.Instance.MaterialItemSheet.Values.First(r =>
                    r.ItemSubType == ItemSubType.ApStone);
                LocalLayerModifier.AddItem(avatarAddress, row.ItemId, 1);

                if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.avatarAddress))
                {
                    GameConfigStateSubject.ActionPointState.Remove(eval.Action.avatarAddress);
                }

                UpdateCurrentAvatarStateAsync(eval).Forget();
            }
        }

        private void ResponseClaimMonsterCollectionReward(
            ActionEvaluation<ClaimMonsterCollectionReward> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.avatarAddress;
            if (!eval.OutputState.TryGetAvatarStateV2(
                    agentAddress,
                    avatarAddress,
                    out var avatarState, out _))
            {
                return;
            }

            var mail = avatarState.mailBox.FirstOrDefault(e => e is MonsterCollectionMail);
            if (mail is not MonsterCollectionMail
                {
                    attachment: MonsterCollectionResult monsterCollectionResult
                })
            {
                return;
            }

            // LocalLayer
            var rewardInfos = monsterCollectionResult.rewards;
            for (var i = 0; i < rewardInfos.Count; i++)
            {
                var rewardInfo = rewardInfos[i];
                if (!rewardInfo.ItemId.TryParseAsTradableId(
                        TableSheets.Instance.ItemSheet,
                        out var tradableId))
                {
                    continue;
                }

                if (!rewardInfo.ItemId.TryGetFungibleId(
                        TableSheets.Instance.ItemSheet,
                        out var fungibleId))
                {
                    continue;
                }

                if (avatarState.inventory.TryGetFungibleItems(fungibleId, out var items))
                {
                    var item = items.FirstOrDefault(x => x.item is ITradableItem);
                    if (item?.item is ITradableItem tradableItem)
                    {
                        LocalLayerModifier.RemoveItem(
                            avatarAddress,
                            tradableId,
                            tradableItem.RequiredBlockIndex,
                            rewardInfo.Quantity);
                    }
                }
            }

            LocalLayerModifier.AddNewAttachmentMail(avatarAddress, mail.id);
            // ~LocalLayer

            // Notification
            NotificationSystem.Push(
                MailType.System,
                L10nManager.Localize("NOTIFICATION_CLAIM_MONSTER_COLLECTION_REWARD_COMPLETE"),
                NotificationCell.NotificationType.Information);

            UpdateAgentStateAsync(eval).Forget();
            UpdateCurrentAvatarStateAsync(eval).Forget();
            RenderQuest(avatarAddress, avatarState.questList.completedQuestIds);
        }

        private void ResponseTransferAsset(ActionEvaluation<TransferAsset> eval)
        {
            if (eval.Exception is not null)
            {
                return;
            }

            var senderAddress = eval.Action.Sender;
            var recipientAddress = eval.Action.Recipient;
            var currentAgentAddress = States.Instance.AgentState.address;
            var currentAvatarAddress = States.Instance.CurrentAvatarState.address;
            var playToEarnRewardAddress = new Address("d595f7e85e1757d6558e9e448fa9af77ab28be4c");
            if (senderAddress == currentAgentAddress)
            {
                var amount = eval.Action.Amount;

                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize(
                        "UI_TRANSFERASSET_NOTIFICATION_SENDER",
                        amount,
                        recipientAddress),
                    NotificationCell.NotificationType.Notification);
            }
            else if (recipientAddress == currentAgentAddress)
            {
                var amount = eval.Action.Amount;
                if (senderAddress == playToEarnRewardAddress)
                {
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_PLAYTOEARN_NOTIFICATION_FORMAT", amount),
                        NotificationCell.NotificationType.Notification);
                }
                else
                {
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize(
                            "UI_TRANSFERASSET_NOTIFICATION_RECIPIENT",
                            amount,
                            senderAddress),
                        NotificationCell.NotificationType.Notification);
                }
            }
            else if (recipientAddress == currentAvatarAddress)
            {
                var amount = eval.Action.Amount;
                var currency = amount.Currency;
                States.Instance.CurrentAvatarBalances[currency.Ticker] =
                    eval.OutputState.GetBalance(
                        currentAvatarAddress,
                        eval.Action.Amount.Currency
                    );
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize(
                        "UI_TRANSFERASSET_NOTIFICATION_RECIPIENT",
                        amount,
                        senderAddress),
                    NotificationCell.NotificationType.Notification);
            }

            UpdateAgentStateAsync(eval).Forget();
        }

        private void ResponseGrinding(ActionEvaluation<Grinding> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            var avatarAddress = eval.Action.AvatarAddress;
            var avatarState = eval.OutputState.GetAvatarState(avatarAddress);
            var mail = avatarState.mailBox.OfType<GrindingMail>()
                .FirstOrDefault(m => m.id.Equals(eval.Action.Id));
            if (mail is null)
            {
                return;
            }

            if (eval.Action.ChargeAp)
            {
                var row = TableSheets.Instance.MaterialItemSheet.Values.First(r =>
                    r.ItemSubType == ItemSubType.ApStone);
                LocalLayerModifier.AddItem(avatarAddress, row.ItemId);

                if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.AvatarAddress))
                {
                    GameConfigStateSubject.ActionPointState.Remove(eval.Action.AvatarAddress);
                }
            }

            OneLineSystem.Push(
                MailType.Grinding,
                L10nManager.Localize("UI_GRINDING_NOTIFY"),
                NotificationCell.NotificationType.Information);
            UpdateCurrentAvatarStateAsync(eval).Forget();
            UpdateAgentStateAsync(eval).Forget();
        }

        private async UniTaskVoid ResponseUnlockEquipmentRecipeAsync(
            ActionEvaluation<UnlockEquipmentRecipe> eval)
        {
            Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(false);
            var sharedModel = Craft.SharedModel;
            var recipeIds = eval.Action.RecipeIds;
            if (!(eval.Exception is null))
            {
                foreach (var id in eval.Action.RecipeIds)
                {
                    sharedModel.UnlockingRecipes.Remove(id);
                }

                sharedModel.SetUnlockedRecipes(sharedModel.UnlockedRecipes.Value);
                sharedModel.UpdateUnlockableRecipes();
                return;
            }

            var sheet = TableSheets.Instance.EquipmentItemRecipeSheet;
            var cost = CrystalCalculator.CalculateRecipeUnlockCost(recipeIds, sheet);
            await UniTask.WhenAll(
                LocalLayerModifier.ModifyAgentCrystalAsync(
                    States.Instance.AgentState.address,
                    cost.MajorUnit),
                UpdateCurrentAvatarStateAsync(eval),
                UpdateAgentStateAsync(eval));

            foreach (var id in recipeIds)
            {
                sharedModel.UnlockingRecipes.Remove(id);
                States.Instance.UpdateHammerPointStates(
                    id,
                    new HammerPointState(
                        Addresses.GetHammerPointStateAddress(eval.Action.AvatarAddress, id),
                        id));
            }

            recipeIds.AddRange(sharedModel.UnlockedRecipes.Value);
            sharedModel.SetUnlockedRecipes(recipeIds);
            sharedModel.UpdateUnlockableRecipes();
        }

        private void ResponseUnlockWorld(ActionEvaluation<UnlockWorld> eval)
        {
            Widget.Find<UnlockWorldLoadingScreen>().Close();

            if (eval.Exception is not null)
            {
                Debug.LogError($"unlock world exc : {eval.Exception.InnerException}");
                return;
            }

            var worldMap = Widget.Find<WorldMap>();
            worldMap.SharedViewModel.UnlockedWorldIds.AddRange(eval.Action.WorldIds);
            worldMap.SetWorldInformation(States.Instance.CurrentAvatarState.worldInformation);

            UpdateAgentStateAsync(eval).Forget();
        }

        private void ResponseHackAndSlashRandomBuff(
            ActionEvaluation<HackAndSlashRandomBuff> eval)
        {
            if (!(eval.Exception is null))
            {
                Debug.LogError($"HackAndSlashRandomBuff exc : {eval.Exception.InnerException}");
                return;
            }

            UpdateCurrentAvatarStateAsync(eval).Forget();
            UpdateAgentStateAsync(eval).Forget();
            UpdateCrystalRandomSkillState(eval);

            Widget.Find<BuffBonusLoadingScreen>().Close();
            Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(false);
            var skillState = States.Instance.CrystalRandomSkillState;
            Widget.Find<BuffBonusResultPopup>().Show(skillState.StageId, skillState);
        }

        private void ResponseStake(ActionEvaluation<Stake> eval)
        {
            if (!(eval.Exception is null))
            {
                return;
            }

            NotificationSystem.Push(
                MailType.System,
                L10nManager.Localize("UI_MONSTERCOLLECTION_UPDATED"),
                NotificationCell.NotificationType.Information);

            var (state, level, balance) = GetStakeState(eval);
            if (state != null)
            {
                UpdateStakeState(state, new GoldBalanceState(state.address, balance), level);
            }

            UpdateAgentStateAsync(eval).Forget();
        }

        private void ResponseClaimStakeReward(ActionEvaluation<ActionBase> eval)
        {
            if (eval.Exception is not null)
            {
                return;
            }

            // Notification
            NotificationSystem.Push(
                MailType.System,
                L10nManager.Localize("NOTIFICATION_CLAIM_MONSTER_COLLECTION_REWARD_COMPLETE"),
                NotificationCell.NotificationType.Information);

            UpdateCurrentAvatarStateAsync(eval).Forget();
        }

        public static void RenderQuest(Address avatarAddress, IEnumerable<int> ids)
        {
            if (avatarAddress != States.Instance.CurrentAvatarState.address)
            {
                return;
            }

            var questList = States.Instance.CurrentAvatarState.questList;
            foreach (var id in ids)
            {
                var quest = questList.FirstOrDefault(q => q.Id == id);
                if (quest == null)
                {
                    continue;
                }

                var rewardMap = quest.Reward.ItemMap;

                foreach (var reward in rewardMap)
                {
                    var materialRow = TableSheets.Instance
                        .MaterialItemSheet
                        .First(pair => pair.Key == reward.Item1);

                    LocalLayerModifier.RemoveItem(
                        avatarAddress,
                        materialRow.Value.ItemId,
                        reward.Item2);
                }

                LocalLayerModifier.AddReceivableQuest(avatarAddress, id);
            }
        }

        internal class LocalRandom : System.Random, IRandom
        {
            public int Seed { get; }

            public LocalRandom(int seed) : base(seed)
            {
                Seed = seed;
            }
        }

        private static async UniTaskVoid ResponseJoinArenaAsync(
            ActionEvaluation<JoinArena> eval)
        {
            if (eval.Action.avatarAddress != States.Instance.CurrentAvatarState.address)
            {
                return;
            }

            var arenaJoin = Widget.Find<ArenaJoin>();
            if (eval.Exception != null)
            {
                if (arenaJoin && arenaJoin.IsActive())
                {
                    arenaJoin.OnRenderJoinArena(eval);
                }
            }

            UpdateCrystalBalance(eval);
            await Task.WhenAll(
                States.Instance.UpdateItemSlotStates(BattleType.Arena),
                States.Instance.UpdateRuneSlotStates(BattleType.Arena));

            var currentRound = TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(
                Game.Game.instance.Agent.BlockIndex);
            if (eval.Action.championshipId == currentRound.ChampionshipId &&
                eval.Action.round == currentRound.Round)
            {
                await UniTask.WhenAll(
                    RxProps.ArenaInfoTuple.UpdateAsync(),
                    RxProps.ArenaParticipantsOrderedWithScore.UpdateAsync());
            }
            else
            {
                await RxProps.ArenaInfoTuple.UpdateAsync();
            }

            if (arenaJoin && arenaJoin.IsActive())
            {
                arenaJoin.OnRenderJoinArena(eval);
            }
        }

        private async void ResponseBattleArenaAsync(ActionEvaluation<BattleArena> eval)
        {
            if (!ActionManager.IsLastBattleActionId(eval.Action.Id) ||
                eval.Action.myAvatarAddress != States.Instance.CurrentAvatarState.address)
            {
                return;
            }

            var arenaBattlePreparation = Widget.Find<ArenaBattlePreparation>();
            if (eval.Exception != null)
            {
                if (arenaBattlePreparation && arenaBattlePreparation.IsActive())
                {
                    arenaBattlePreparation.OnRenderBattleArena(eval);
                }

                Game.Game.BackToMainAsync(eval.Exception.InnerException ?? eval.Exception).Forget();

                return;
            }

            await Task.WhenAll(
                States.Instance.UpdateItemSlotStates(BattleType.Arena),
                States.Instance.UpdateRuneSlotStates(BattleType.Arena));
            // NOTE: Start cache some arena info which will be used after battle ends.
            RxProps.ArenaInfoTuple.UpdateAsync().Forget();
            RxProps.ArenaParticipantsOrderedWithScore.UpdateAsync().Forget();

            _disposableForBattleEnd?.Dispose();
            _disposableForBattleEnd = Game.Game.instance.Arena.OnArenaEnd
                .First()
                .Subscribe(_ =>
                {
                    UniTask.Run(() =>
                        {
                            UpdateAgentStateAsync(eval).Forget();
                            UpdateCurrentAvatarStateAsync().Forget();
                            // TODO!!!! [`PlayersArenaParticipant`]를 개별로 업데이트 한다.
                            // RxProps.PlayersArenaParticipant.UpdateAsync().Forget();
                            _disposableForBattleEnd = null;
                            Game.Game.instance.Arena.IsAvatarStateUpdatedAfterBattle = true;
                        }).ToObservable()
                        .First()
                        // ReSharper disable once ConvertClosureToMethodGroup
                        .DoOnError(e => Debug.LogException(e));
                });

            var tableSheets = TableSheets.Instance;
            var (myDigest, enemyDigest) =
                GetArenaPlayerDigest(
					eval.PreviousState,
                    eval.OutputState,
                    eval.Action.myAvatarAddress,
                    eval.Action.enemyAvatarAddress);
            var championshipId = eval.Action.championshipId;
            var round = eval.Action.round;

            var myArenaScoreAdr = ArenaScore.DeriveAddress(
                eval.Action.myAvatarAddress,
                championshipId,
                round);
            var previousMyScore =
                eval.PreviousState.TryGetArenaScore(myArenaScoreAdr, out var myArenaScore)
                    ? myArenaScore.Score
                    : ArenaScore.ArenaScoreDefault;
            int outMyScore = eval.OutputState.TryGetState(
                myArenaScoreAdr,
                out List outputMyScoreList)
                ? (Integer)outputMyScoreList[1]
                : ArenaScore.ArenaScoreDefault;

            var hasMedalReward =
                tableSheets.ArenaSheet[championshipId].TryGetRound(round, out var row) &&
                row.ArenaType != ArenaType.OffSeason;
            var medalItem = ItemFactory.CreateMaterial(
                tableSheets.MaterialItemSheet,
                ArenaHelper.GetMedalItemId(championshipId, round));

            var random = new LocalRandom(eval.RandomSeed);
            var winCount = 0;
            var defeatCount = 0;
            var logs = new List<ArenaLog>();
            var rewards = new List<ItemBase>();
            var arenaSheets = tableSheets.GetArenaSimulatorSheets();
            for (int i = 0; i < eval.Action.ticket; i++)
            {
                var simulator = new ArenaSimulator(random);
                var log = simulator.Simulate(
                    myDigest,
                    enemyDigest,
                    arenaSheets);

                var reward = RewardSelector.Select(
                    random,
                    tableSheets.WeeklyArenaRewardSheet,
                    tableSheets.MaterialItemSheet,
                    myDigest.Level,
                    ArenaHelper.GetRewardCount(previousMyScore));

                if (log.Result.Equals(ArenaLog.ArenaResult.Win))
                {
                    if (hasMedalReward && medalItem is { })
                    {
                        reward.Add(medalItem);
                    }

                    winCount++;
                }
                else
                {
                    defeatCount++;
                }

                log.Score = outMyScore;

                logs.Add(log);
                rewards.AddRange(reward);
            }

            if (arenaBattlePreparation && arenaBattlePreparation.IsActive())
            {
                arenaBattlePreparation.OnRenderBattleArena(eval);
                Game.Game.instance.Arena.Enter(
                    logs.First(),
                    rewards,
                    myDigest,
                    enemyDigest,
                    eval.Action.myAvatarAddress,
                    eval.Action.enemyAvatarAddress,
                    winCount + defeatCount > 1 ? (winCount, defeatCount) : null);
            }
        }

        private (ArenaPlayerDigest myDigest, ArenaPlayerDigest enemyDigest) GetArenaPlayerDigest(
            IAccountStateDelta prevStates,
            IAccountStateDelta outputStates,
            Address myAvatarAddress,
            Address enemyAvatarAddress)
        {
            var myAvatarState = States.Instance.CurrentAvatarState;
            var enemyAvatarState = prevStates.GetAvatarState(enemyAvatarAddress);
            enemyAvatarState.inventory =
                new Model.Item.Inventory((List)prevStates.GetState(enemyAvatarAddress.Derive("inventory")));
            var myItemSlotStateAddress = ItemSlotState.DeriveAddress(myAvatarAddress, BattleType.Arena);
            var myItemSlotState = outputStates.TryGetState(myItemSlotStateAddress, out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Arena);

            var myRuneSlotState = States.Instance.CurrentRuneSlotStates[BattleType.Arena];
            var myRuneStates = new List<RuneState>();
            var myRuneSlotInfos = myRuneSlotState.GetEquippedRuneSlotInfos();
            foreach (var runeId in myRuneSlotInfos.Select(r => r.RuneId))
            {
                if (States.Instance.TryGetRuneState(runeId, out var runeState))
                {
                    myRuneStates.Add(runeState);
                }
            }

            var myDigest = new ArenaPlayerDigest(myAvatarState,
                myItemSlotState.Equipments,
                myItemSlotState.Costumes,
                myRuneStates);

            var enemyItemSlotStateAddress = ItemSlotState.DeriveAddress(enemyAvatarAddress, BattleType.Arena);
            var enemyItemSlotState =
                prevStates.GetState(enemyItemSlotStateAddress) is List enemyRawItemSlotState
                    ? new ItemSlotState(enemyRawItemSlotState)
                    : new ItemSlotState(BattleType.Arena);

            var enemyRuneSlotStateAddress = RuneSlotState.DeriveAddress(enemyAvatarAddress, BattleType.Arena);
            var enemyRuneSlotState =
                prevStates.GetState(enemyRuneSlotStateAddress) is List enemyRawRuneSlotState
                    ? new RuneSlotState(enemyRawRuneSlotState)
                    : new RuneSlotState(BattleType.Arena);

            var enemyRuneStates = new List<RuneState>();
            var enemyRuneSlotInfos = enemyRuneSlotState.GetEquippedRuneSlotInfos();
            var runeAddresses = enemyRuneSlotInfos.Select(info =>
                RuneState.DeriveAddress(enemyAvatarAddress, info.RuneId));
            foreach (var address in runeAddresses)
            {
                if (prevStates.GetState(address) is List rawRuneState)
                {
                    enemyRuneStates.Add(new RuneState(rawRuneState));
                }
            }

            var enemyDigest = new ArenaPlayerDigest(enemyAvatarState,
                enemyItemSlotState.Equipments,
                enemyItemSlotState.Costumes,
                enemyRuneStates);

            return (myDigest, enemyDigest);
        }

        private async void ResponseRaidAsync(ActionEvaluation<Raid> eval)
        {
            if (eval.Exception is not null)
            {
                Game.Game.BackToMainAsync(eval.Exception.InnerException, false).Forget();
                return;
            }

            await Task.WhenAll(
                States.Instance.UpdateItemSlotStates(BattleType.Raid),
                States.Instance.UpdateRuneSlotStates(BattleType.Raid));

            var worldBoss = Widget.Find<WorldBoss>();
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            if (Widget.Find<RaidPreparation>().IsSkipRender)
            {
                Widget.Find<LoadingScreen>().Close();
                worldBoss.Close();
                await WorldBossStates.Set(avatarAddress);
                await States.Instance.InitRuneStoneBalance();
                Game.Event.OnRoomEnter.Invoke(true);
                return;
            }

            if (eval.Action.PayNcg)
            {
                UpdateAgentStateAsync(eval).Forget();
            }

            UpdateCrystalBalance(eval);

            _disposableForBattleEnd?.Dispose();
            _disposableForBattleEnd =
                Game.Game.instance.RaidStage.OnBattleEnded
                    .First()
                    .Subscribe(stage =>
                    {
                        var task = UniTask.Run(() =>
                        {
                            UpdateCurrentAvatarStateAsync(eval).Forget();
                            var avatarState = States.Instance.CurrentAvatarState;
                            RenderQuest(eval.Action.AvatarAddress,
                                avatarState.questList.completedQuestIds);
                            _disposableForBattleEnd = null;
                            stage.IsAvatarStateUpdatedAfterBattle = true;
                        });
                        task.ToObservable()
                            .First()
                            // ReSharper disable once ConvertClosureToMethodGroup
                            .DoOnError(e => Debug.LogException(e));
                    });

            if (!WorldBossFrontHelper.TryGetCurrentRow(eval.BlockIndex, out var row))
            {
                Debug.LogError(
                    $"[Raid] Failed to get current world boss row. BlockIndex : {eval.BlockIndex}");
                return;
            }

            var clonedAvatarState = (AvatarState)States.Instance.CurrentAvatarState.Clone();
            var random = new LocalRandom(eval.RandomSeed);
            var preRaiderState = WorldBossStates.GetRaiderState(avatarAddress);
            var preKillReward = WorldBossStates.GetKillReward(avatarAddress);
            var latestBossLevel = preRaiderState?.LatestBossLevel ?? 0;
            var runeStates = States.Instance.GetEquippedRuneStates(BattleType.Raid);
            var itemSlotStates = States.Instance.CurrentItemSlotStates[BattleType.Raid];

            var simulator = new RaidSimulator(
                row.BossId,
                random,
                clonedAvatarState,
                eval.Action.FoodIds,
                runeStates,
                TableSheets.Instance.GetRaidSimulatorSheets(),
                TableSheets.Instance.CostumeStatSheet
            );
            simulator.Simulate();
            var log = simulator.Log;
            Widget.Find<Menu>().Close();

            var playerDigest = new ArenaPlayerDigest(
                clonedAvatarState,
                itemSlotStates.Equipments,
                itemSlotStates.Costumes,
                runeStates);

            await WorldBossStates.Set(avatarAddress);
            await States.Instance.InitRuneStoneBalance();
            await States.Instance.InitRuneStates();
            var raiderState = WorldBossStates.GetRaiderState(avatarAddress);
            var killRewards = new List<FungibleAssetValue>();
            if (latestBossLevel < raiderState.LatestBossLevel)
            {
                if (preKillReward != null && preKillReward.IsClaimable(raiderState.LatestBossLevel))
                {
                    var filtered = preKillReward
                        .Where(kv => !kv.Value)
                        .Select(kv => kv.Key)
                        .ToList();

                    var bossRow =
                        Game.Game.instance.TableSheets.WorldBossCharacterSheet[row.BossId];
                    var rank = WorldBossHelper.CalculateRank(bossRow, preRaiderState.HighScore);


                    foreach (var level in filtered)
                    {
                        var rewards = RuneHelper.CalculateReward(
                            rank,
                            row.BossId,
                            Game.Game.instance.TableSheets.RuneWeightSheet,
                            Game.Game.instance.TableSheets.WorldBossKillRewardSheet,
                            Game.Game.instance.TableSheets.RuneSheet,
                            random
                        );

                        killRewards.AddRange(rewards);
                    }
                }
            }

            var isNewRecord = raiderState is null ||
                              raiderState.HighScore < simulator.DamageDealt;
            worldBoss.Close(true);

            Widget.Find<LoadingScreen>().Close();
            Game.Game.instance.RaidStage.Play(
                eval.Action.AvatarAddress,
                simulator.BossId,
                log,
                playerDigest,
                simulator.DamageDealt,
                isNewRecord,
                false,
                simulator.AssetReward,
                killRewards);
        }

        private static async void ResponseClaimRaidRewardAsync(
            ActionEvaluation<ClaimRaidReward> eval)
        {
            if (eval.Exception is not null)
            {
                return;
            }

            await States.Instance.InitRuneStoneBalance();
            UpdateCrystalBalance(eval);
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            WorldBossStates.SetReceivingGradeRewards(avatarAddress, false);
            Widget.Find<WorldBossRewardScreen>().Show(new LocalRandom(eval.RandomSeed));
        }

        private void ResponseRuneEnhancement(ActionEvaluation<RuneEnhancement> eval)
        {
            Widget.Find<Rune>().OnActionRender(new LocalRandom(eval.RandomSeed)).Forget();

            if (eval.Exception is not null)
            {
                return;
            }

            UpdateCrystalBalance(eval);
            UpdateAgentStateAsync(eval).Forget();
        }

        private void ResponseUnlockRuneSlot(ActionEvaluation<UnlockRuneSlot> eval)
        {
            if (eval.Exception is not null)
            {
                return;
            }

            for (var i = 1; i < (int)BattleType.End; i++)
            {
                States.Instance.CurrentRuneSlotStates[(BattleType)i].Unlock(eval.Action.SlotIndex);
            }

            LoadingHelper.UnlockRuneSlot.Remove(eval.Action.SlotIndex);
            UpdateAgentStateAsync(eval).Forget();
            NotificationSystem.Push(
                MailType.Workshop,
                L10nManager.Localize("UI_MESSAGE_RUNE_SLOT_OPEN"),
                NotificationCell.NotificationType.Notification);
        }

        private void ResponsePetEnhancement(ActionEvaluation<PetEnhancement> eval)
        {
            LoadingHelper.PetEnhancement.Value = 0;
            var action = eval.Action;
            if (eval.Exception is not null ||
                action.AvatarAddress != States.Instance.CurrentAvatarState.address)
            {
                return;
            }

            if (States.Instance.PetStates.TryGetPetState(action.PetId, out _))
            {
                Widget.Find<PetLevelUpResultScreen>().Show(action);
            }
            else
            {
                Widget.Find<PetSummonResultScreen>().Show(action.PetId);
            }

            UpdateAgentStateAsync(eval).Forget();
            var soulStoneTicker = TableSheets.Instance.PetSheet[action.PetId].SoulStoneTicker;
            States.Instance.CurrentAvatarBalances[soulStoneTicker] = eval.OutputState.GetBalance(
                action.AvatarAddress,
                Currencies.GetMinterlessCurrency(soulStoneTicker)
            );
            UpdatePetState(action.AvatarAddress, eval.OutputState, action.PetId);
            Widget.Find<DccCollection>().UpdateView();
            Game.Game.instance.SavedPetId = action.PetId;
        }

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
        private void Testbed()
        {
            _actionRenderer.EveryRender<CreateTestbed>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe()
                .AddTo(_disposables);

            _actionRenderer.EveryRender<CreateArenaDummy>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe()
                .AddTo(_disposables);
        }

        private void ManipulateState()
        {
            _actionRenderer.EveryRender<ManipulateState>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(async eval =>
                {
                    await UpdateCurrentAvatarStateAsync(eval);
                    await RxProps.SelectAvatarAsync(
                        States.Instance.CurrentAvatarKey,
                        forceNewSelection: true);
                    NotificationSystem.Push(
                        MailType.System,
                        "State Manipulated",
                        NotificationCell.NotificationType.Information);
                })
                .AddTo(_disposables);
        }
#endif

        private static void UpdatePetState(
            Address avatarAddress,
            IAccountState states,
            int petId)
        {
            var rawPetState = states.GetState(
                PetState.DeriveAddress(avatarAddress, petId)
            );
            States.Instance.PetStates.UpdatePetState(
                petId,
                new PetState((List)rawPetState)
            );
        }

        private void ResponseRequestPledge(ActionEvaluation<RequestPledge> eval)
        {
            if (eval.Exception is not null)
            {
                return;
            }

            var agentAddress = States.Instance.AgentState.address;
            var pledgeAddress = agentAddress.GetPledgeAddress();

            Address? address = null;
            var approved = false;
            var mead = 0;
            if (eval.OutputState.GetState(pledgeAddress) is List l)
            {
                address = l[0].ToAddress();
                approved = l[1].ToBoolean();
                mead = l[2].ToInteger();
            }

            States.Instance.SetPledgeStates(address, approved);
        }

        private void ResponseApprovePledge(ActionEvaluation<ApprovePledge> eval)
        {
            if (eval.Exception is not null)
            {
                return;
            }

            var agentAddress = States.Instance.AgentState.address;
            var pledgeAddress = agentAddress.GetPledgeAddress();

            Address? address = null;
            var approved = false;
            var mead = 0;
            if (eval.OutputState.GetState(pledgeAddress) is List l)
            {
                address = l[0].ToAddress();
                approved = l[1].ToBoolean();
                mead = l[2].ToInteger();
            }

            States.Instance.SetPledgeStates(address, approved);
        }
    }
}
