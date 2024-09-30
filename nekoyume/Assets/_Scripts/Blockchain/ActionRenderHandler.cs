using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.L10n;
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
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using mixpanel;
using Nekoyume.Action.CustomEquipmentCraft;
using Nekoyume.Action.Garages;
using Nekoyume.ApiClient;
using Nekoyume.Arena;
using Nekoyume.EnumType;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Game.Battle;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Market;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module.WorldBoss;
using Skill = Nekoyume.Model.Skill.Skill;

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions.Action;
#endif

namespace Nekoyume.Blockchain
{
    using Model;
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.Action.Exceptions.AdventureBoss;
    using Nekoyume.Battle.AdventureBoss;
    using Data;
    using Nekoyume.TableData.AdventureBoss;
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
            internal static readonly ActionRenderHandler Value = new();
        }

        public static ActionRenderHandler Instance => Singleton.Value;

        private readonly List<IDisposable> _disposables = new();

        private IDisposable _disposableForBattleEnd;

        private ActionRenderer _actionRenderer;

        // approximately 4h == 1200 block count
        private const int WorkshopNotifiedBlockCount = 0;

        private const string WorkshopPushIdentifierKeyFormat = "WORKSHOP_SLOT_{0}_PUSH_IDENTIFIER";

        private ActionRenderHandler()
        {
        }

        public override void Start(ActionRenderer renderer)
        {
            _actionRenderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

            Stop();
            _actionRenderer.BlockEndSubject.ObserveOnMainThread().Subscribe(_ => { NcDebug.Log($"[{nameof(BlockRenderHandler)}] Render actions end"); }).AddTo(_disposables);
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
                        var type = actionType.TypeIdentifier.Inspect();
                        Analyzer.Instance.Track(
                            "Unity/ActionRender",
                            new Dictionary<string, Value>
                            {
                                ["ActionType"] = type,
                                ["Elapsed"] = elapsed,
                                ["AvatarAddress"] = currentAvatarState.address.ToString(),
                                ["AgentAddress"] = agentState.address.ToString()
                            });

                        var category = $"ActionRender_{type}";
                        var evt = new AirbridgeEvent(category);
                        evt.SetValue(elapsed);
                        evt.AddCustomAttribute("agent-address", agentState.address.ToString());
                        evt.AddCustomAttribute("avatar-address", currentAvatarState.address.ToString());
                        AirbridgeUnity.TrackEvent(evt);
                    }

                    var actionTypeName = actionType.TypeIdentifier.Inspect();
                    if (actionTypeName.Contains("transfer_"))
                    {
                        return;
                    }

                    Widget.Find<HeaderMenuStatic>().UpdatePortalRewardDaily();
                }
            }).AddTo(_disposables);

            RewardGold();
            GameConfig();
            CreateAvatar();
            TransferAsset();
            TransferAssets();
            Stake();

            // MeadPledge
            RequestPledge();
            ApprovePledge();

            // Battle
            HackAndSlash();
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
            AuraSummon();
            RuneSummon();
            CustomEquipmentCraft();

            // Market
            RegisterProduct();
            CancelProductRegistration();
            ReRegisterProduct();
            BuyProduct();

            // Consume
            DailyReward();
            RedeemCode();
            ChargeActionPoint();
            ClaimStakeReward();

            // Unlocks
            UnlockEquipmentRecipe();
            UnlockWorld();
            UnlockCombinationSlot();

            // Arena
            InitializeArenaActions();

            // World Boss
            Raid();
            ClaimRaidReward();

            // Rune
            RuneEnhancement();
            UnlockRuneSlot();

            PetEnhancement();

            // Collection
            ActivateCollection();

            // GARAGE
            UnloadFromMyGarages();

            // Claim Items
            ClaimItems();

            // Mint Assets
            MintAssets();
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            Testbed();
            ManipulateState();
#endif

            //AdventureBoss
            ClaimAdventureBossReward();
            ExploreAdventureBoss();
            SweepAdventureBoss();
            Wanted();
            UnlockFloor();
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
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Subscribe(eval => UpdateAgentStateAsync(eval).Forget())
                .AddTo(_disposables);
        }

        private void CreateAvatar()
        {
            _actionRenderer.EveryRender<CreateAvatar>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Subscribe(ResponseCreateAvatar)
                .AddTo(_disposables);
        }

        private void HackAndSlash()
        {
            _actionRenderer.EveryRender<HackAndSlash>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareHackAndSlash)
                .ObserveOnMainThread()
                .Subscribe(ResponseHackAndSlashAsync)
                .AddTo(_disposables);

            _actionRenderer.EveryRender<HackAndSlash>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsTerminated)
                .ObserveOnMainThread()
                .Subscribe(ExceptionHackAndSlash)
                .AddTo(_disposables);
        }

        private void EventDungeonBattle()
        {
            _actionRenderer.EveryRender<EventDungeonBattle>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareEventDungeonBattle)
                .ObserveOnMainThread()
                .Subscribe(ResponseEventDungeonBattleAsync)
                .AddTo(_disposables);

            _actionRenderer.EveryRender<EventDungeonBattle>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsTerminated)
                .ObserveOnMainThread()
                .Subscribe(ExceptionEventDungeonBattle)
                .AddTo(_disposables);
        }

        private void CombinationConsumable()
        {
            _actionRenderer.EveryRender<CombinationConsumable>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareCombinationConsumable)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombinationConsumable)
                .AddTo(_disposables);
        }

        private void RegisterProduct()
        {
            _actionRenderer.EveryRender<RegisterProduct>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Subscribe(ResponseRegisterProductAsync)
                .AddTo(_disposables);
        }

        private void CancelProductRegistration()
        {
            _actionRenderer.EveryRender<CancelProductRegistration>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseCancelProductRegistrationAsync)
                .AddTo(_disposables);
        }

        private void ReRegisterProduct()
        {
            _actionRenderer.EveryRender<ReRegisterProduct>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseReRegisterProduct)
                .AddTo(_disposables);
        }

        private void BuyProduct()
        {
            _actionRenderer.EveryRender<BuyProduct>()
                .Where(eval =>
                    ValidateEvaluationForCurrentAgent(eval) ||
                    eval.Action.ProductInfos.Any(info => info.AgentAddress.Equals(States.Instance.AgentState.address)))
                .ObserveOnMainThread()
                .Subscribe(ResponseBuyProduct)
                .AddTo(_disposables);
        }

        private void ItemEnhancement()
        {
            _actionRenderer.EveryRender<ItemEnhancement>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Select(PreResponseItemEnhancement)
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareItemEnhancement)
                .ObserveOnMainThread()
                .Subscribe(ResponseItemEnhancement)
                .AddTo(_disposables);
        }

        private void DailyReward()
        {
            _actionRenderer.EveryRender<DailyReward>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(eval => eval.Action.avatarAddress.Equals(States.Instance.CurrentAvatarState.address))
                .Select(PreResponseDailyReward)
                .Where(ValidateEvaluationIsSuccess)
                .ObserveOnMainThread()
                .Subscribe(ResponseDailyReward)
                .AddTo(_disposables);
        }

        private void CombinationEquipment()
        {
            _actionRenderer.EveryRender<CombinationEquipment>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Select(PreResponseCombinationEquipment)
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareCombinationEquipment)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombinationEquipment)
                .AddTo(_disposables);

            _actionRenderer.EveryRender<CombinationEquipment>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsTerminated)
                .ObserveOnMainThread()
                .Subscribe(ExceptionCombinationEquipment)
                .AddTo(_disposables);
        }

        private void Grinding()
        {
            _actionRenderer.EveryRender<Grinding>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Where(ValidateGrindingMailExists)
                .Select(PrepareGrinding)
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
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareHackAndSlashRandomBuff)
                .ObserveOnMainThread()
                .Subscribe(ResponseHackAndSlashRandomBuff)
                .AddTo(_disposables);
        }

        private void RapidCombination()
        {
            _actionRenderer.EveryRender<RapidCombination>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareRapidCombination)
                .ObserveOnMainThread()
                .Subscribe(ResponseRapidCombination)
                .AddTo(_disposables);
        }

        private void GameConfig()
        {
            _actionRenderer.EveryRender<PatchTableSheet>()
                .ObserveOn(Scheduler.ThreadPool)
                .Subscribe(UpdateGameConfigState)
                .AddTo(_disposables);
        }

        private void RedeemCode()
        {
            _actionRenderer.EveryRender<Action.RedeemCode>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Subscribe(ResponseRedeemCode)
                .AddTo(_disposables);
        }

        private void ChargeActionPoint()
        {
            _actionRenderer.EveryRender<ChargeActionPoint>()
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(eval =>
                    eval.Action.avatarAddress.Equals(States.Instance.CurrentAvatarState.address))
                .ObserveOnMainThread()
                .Subscribe(ResponseChargeActionPoint)
                .AddTo(_disposables);
        }

        private void TransferAsset()
        {
            _actionRenderer.EveryRender<TransferAsset>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(eval =>
                    ValidateEvaluationForCurrentAgent(eval) ||
                    eval.Action.Recipient.Equals(States.Instance.AgentState?.address) ||
                    eval.Action.Recipient.Equals(States.Instance.CurrentAvatarState?.address))
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareTransferAsset)
                .ObserveOnMainThread()
                .Subscribe(ResponseTransferAsset)
                .AddTo(_disposables);
        }

        private void TransferAssets()
        {
            _actionRenderer.EveryRender<TransferAssets>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(eval =>
                    ValidateEvaluationForCurrentAgent(eval) ||
                    eval.Action.Recipients.Any(e =>
                        e.recipient.Equals(States.Instance.AgentState?.address) ||
                        e.recipient.Equals(States.Instance.CurrentAvatarState?.address)))
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareTransferAssets)
                .ObserveOnMainThread()
                .Subscribe(ResponseTransferAssets)
                .AddTo(_disposables);
        }

        private void HackAndSlashSweep()
        {
            _actionRenderer.EveryRender<HackAndSlashSweep>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareHackAndSlashSweepAsync)
                .ObserveOnMainThread()
                .Subscribe(ResponseHackAndSlashSweepAsync)
                .AddTo(_disposables);

            _actionRenderer.EveryRender<HackAndSlashSweep>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsTerminated)
                .ObserveOnMainThread()
                .Subscribe(ExceptionHackAndSlashSweep)
                .AddTo(_disposables);
        }

        private void Stake()
        {
            _actionRenderer.EveryRender<Stake>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOnMainThread()
                .Subscribe(ResponseStake)
                .AddTo(_disposables);
        }

        private void ClaimStakeReward()
        {
            _actionRenderer.ActionRenderSubject
                .Where(eval =>
                    ValidateEvaluationForCurrentAgent(eval) &&
                    eval.Action is IClaimStakeReward)
                .ObserveOnMainThread()
                .Subscribe(ResponseClaimStakeReward)
                .AddTo(_disposables);
        }

        private void InitializeArenaActions()
        {
            _actionRenderer.EveryRender<JoinArena>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Select(PrepareJoinArena)
                .ObserveOnMainThread()
                .Subscribe(ResponseJoinArenaAsync)
                .AddTo(_disposables);

            _actionRenderer.EveryRender<BattleArena>()
                .Where(ValidateEvaluationForCurrentAgent)
                .ObserveOn(Scheduler.ThreadPool)
                .Select(PrepareBattleArena)
                .ObserveOnMainThread()
                .Subscribe(ResponseBattleArenaAsync)
                .AddTo(_disposables);
        }

        private void EventConsumableItemCrafts()
        {
            _actionRenderer.EveryRender<EventConsumableItemCrafts>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareEventConsumableItemCrafts)
                .ObserveOnMainThread()
                .Subscribe(ResponseEventConsumableItemCrafts)
                .AddTo(_disposables);
        }

        private void EventMaterialItemCrafts()
        {
            _actionRenderer.EveryRender<EventMaterialItemCrafts>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareEventMaterialItemCrafts)
                .ObserveOnMainThread()
                .Subscribe(ResponseEventMaterialItemCrafts)
                .AddTo(_disposables);
        }

        private void Raid()
        {
            _actionRenderer.EveryRender<Raid>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Select(PrepareRaid)
                .ObserveOnMainThread()
                .Subscribe(ResponseRaidAsync)
                .AddTo(_disposables);
        }

        private void ClaimRaidReward()
        {
            _actionRenderer.EveryRender<ClaimRaidReward>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareClaimRaidReward)
                .ObserveOnMainThread()
                .Subscribe(ResponseClaimRaidReward)
                .AddTo(_disposables);
        }

        private void RuneEnhancement()
        {
            _actionRenderer.EveryRender<RuneEnhancement>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareRuneEnhancement)
                .ObserveOnMainThread()
                .Subscribe(ResponseRuneEnhancement)
                .AddTo(_disposables);
        }

        private void UnlockRuneSlot()
        {
            _actionRenderer.EveryRender<UnlockRuneSlot>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .ObserveOnMainThread()
                .Select(PreResponseUnlockRuneSlot)
                .ObserveOn(Scheduler.ThreadPool)
                .Select(PrepareUnlockRuneSlot)
                .ObserveOnMainThread()
                .Subscribe(ResponseUnlockRuneSlot)
                .AddTo(_disposables);
        }

        private void UnlockCombinationSlot()
        {
            _actionRenderer.EveryRender<UnlockCombinationSlot>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .ObserveOnMainThread()
                .Subscribe(ResponseUnlockCombinationSlot)
                .AddTo(_disposables);

            _actionRenderer.EveryRender<UnlockCombinationSlot>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsTerminated)
                .ObserveOnMainThread()
                .Subscribe(ExceptionUnlockCombinationSlot)
                .AddTo(_disposables);
        }

        private void PetEnhancement()
        {
            _actionRenderer.EveryRender<PetEnhancement>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Where(eval => eval.Action.AvatarAddress == States.Instance.CurrentAvatarState.address)
                .Select(PreparePetEnhancement)
                .ObserveOnMainThread()
                .Subscribe(ResponsePetEnhancement)
                .AddTo(_disposables);
        }

        private void ActivateCollection()
        {
            _actionRenderer.EveryRender<ActivateCollection>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsSuccess)
                .Where(eval => eval.Action.AvatarAddress == States.Instance.CurrentAvatarState.address)
                .Select(PrepareActivateCollection)
                .ObserveOnMainThread()
                .Subscribe(ResponseActivateCollection)
                .AddTo(_disposables);
        }

        private void RequestPledge()
        {
            _actionRenderer.EveryRender<RequestPledge>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(eval => eval.Action.AgentAddress.Equals(States.Instance.AgentState.address))
                .Subscribe(ResponseRequestPledge)
                .AddTo(_disposables);
        }

        private void ApprovePledge()
        {
            _actionRenderer.EveryRender<ApprovePledge>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Subscribe(ResponseApprovePledge)
                .AddTo(_disposables);
        }

        private void UnloadFromMyGarages()
        {
            _actionRenderer.EveryRender<UnloadFromMyGarages>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(eval =>
                    eval.Action.RecipientAvatarAddr.Equals(States.Instance.CurrentAvatarState.address) ||
                    (eval.Action.FungibleAssetValues is not null &&
                        eval.Action.FungibleAssetValues.Any(e =>
                            e.balanceAddr.Equals(States.Instance.AgentState.address) ||
                            e.balanceAddr.Equals(States.Instance.CurrentAvatarState.address))))
                .Select(PrepareUnloadFromMyGarages)
                .ObserveOnMainThread()
                .Subscribe(ResponseUnloadFromMyGarages)
                .AddTo(_disposables);
        }

        private void AuraSummon()
        {
            _actionRenderer.EveryRender<AuraSummon>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(eval => eval.Action.AvatarAddress.Equals(States.Instance.CurrentAvatarState.address))
                .Where(ValidateEvaluationIsSuccess)
                .ObserveOnMainThread()
                .Subscribe(ResponseAuraSummon)
                .AddTo(_disposables);
        }

        private void RuneSummon()
        {
            _actionRenderer.EveryRender<RuneSummon>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(eval => eval.Action.AvatarAddress.Equals(States.Instance.CurrentAvatarState.address))
                .Where(ValidateEvaluationIsSuccess)
                .ObserveOnMainThread()
                .Subscribe(ResponseRuneSummon)
                .AddTo(_disposables);
        }

        /// <summary>
        /// Process the action rendering of <see cref="ClaimItems"/>.
        /// At now, rendering is only for updating the inventory of the current avatar.
        /// </summary>
        private void ClaimItems()
        {
            _actionRenderer.EveryRender<ClaimItems>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(eval =>
                    eval.Action.ClaimData.Any(e =>
                        e.address.Equals(States.Instance?.CurrentAvatarState?.address)))
                .Where(ValidateEvaluationIsSuccess)
                .Select(PrepareClaimItems)
                .ObserveOnMainThread()
                .Subscribe(ResponseClaimItems)
                .AddTo(_disposables);
        }

        /// <summary>
        /// Process the action rendering of <see cref="ClaimItems"/>.
        /// At now, rendering is only for updating the inventory of the current avatar.
        /// </summary>
        private void MintAssets()
        {
            _actionRenderer.EveryRender<MintAssets>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationIsSuccess)
                .Where(eval =>
                {
                    return eval.Action.MintSpecs?
                        .Select(spec => spec.Recipient)
                        .Any(addr =>
                            addr.Equals(States.Instance.CurrentAvatarState?.address) ||
                            addr.Equals(States.Instance.AgentState?.address)) ?? false;
                })
                .Select(PrepareMintAssets)
                .ObserveOnMainThread()
                .Subscribe(ResponseMintAssets)
                .AddTo(_disposables);
        }

        private void CustomEquipmentCraft()
        {
            _actionRenderer.EveryRender<CustomEquipmentCraft>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationIsSuccess)
                .Where(ValidateEvaluationForCurrentAgent)
                .Select(PrepareCustomEquipmentCraft)
                .ObserveOnMainThread()
                .Subscribe(ResponseCustomEquipmentCraft)
                .AddTo(_disposables);
            _actionRenderer.EveryRender<CustomEquipmentCraft>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsTerminated)
                .ObserveOnMainThread()
                .Subscribe(ExceptionCustomEquipmentCraft)
                .AddTo(_disposables);
        }

        private void ResponseCreateAvatar(
            ActionEvaluation<CreateAvatar> eval)
        {
            UniTask.Run(async () =>
            {
                // sloth업데이트 이후 액션에서 지정한 블록보다 클라이언트의 블록이 높아지는 순간 아래 액션을 수행합니다.
                // 타임아웃은 10초로, 일반적인 블록딜레이인 8초보다 조금 크게 설정하였습니다.
                await UniTask.WaitUntil(() => Game.Game.instance.Agent.BlockIndex > eval.BlockIndex).TimeoutWithoutException(TimeSpan.FromSeconds(10));

                await UniTask.SwitchToThreadPool();
                await UpdateAgentStateAsync(eval);
                await UpdateAvatarState(eval, eval.Action.index);
                // 아바타 생성시 States초기화를 위해 forceNewSelection을 true로 설정합니다.
                await RxProps.SelectAvatarAsync(eval.Action.index, eval.OutputState, true);

                await UniTask.SwitchToMainThread();

                var avatarState = States.Instance.AvatarStates[eval.Action.index];
                RenderQuest(avatarState.address, avatarState.questList.completedQuestIds);

                var agentAddr = States.Instance.AgentState.address;
                var avatarAddr = Addresses.GetAvatarAddress(agentAddr, eval.Action.index);
                DialogPopup.DeleteDialogPlayerPrefs(avatarAddr);
                // 액션이 정상적으로 실행되면 최대치로 채워지리라 예상, 최적화를 위해 GetState를 하지 않고 Set합니다.
                ReactiveAvatarState.UpdateActionPoint(Action.DailyReward.ActionPointMax);
                var loginDetail = Widget.Find<LoginDetail>();
                if (loginDetail && loginDetail.IsActive())
                {
                    loginDetail.OnRenderCreateAvatar();
                }
            }).Forget();
        }

        private (ActionEvaluation<RapidCombination> Evaluation, AvatarState AvatarState, List<CombinationSlotState> CombinationSlotStates, Dictionary<int, CombinationSlotState> CurrentCombinationSlotState)
            PrepareRapidCombination(ActionEvaluation<RapidCombination> eval)
        {
            var avatarAddress = eval.Action.avatarAddress;
            var avatarState = States.Instance.AvatarStates.Values
                .FirstOrDefault(x => x.address == avatarAddress);
            var combinationSlotState = avatarState is not null
                ? States.Instance.GetUsedCombinationSlotState(
                    avatarState,
                    Game.Game.instance.Agent.BlockIndex)
                : null;
            var slotStates = new List<CombinationSlotState>();
            foreach (var slotIndex in eval.Action.slotIndexList)
            {
                if (!StateGetter.TryGetCombinationSlotState(
                        eval.OutputState,
                        avatarAddress,
                        slotIndex,
                        out var slotState) || avatarState is null)
                {
                    continue;
                }

                // 액션을 스테이징한 시점에 미리 반영해둔 아이템의 레이어를 먼저 제거하고, 액션의 결과로 나온 실제 상태를 반영
                var result = (RapidCombination5.ResultModel)slotState.Result;
                foreach (var pair in result.cost)
                {
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value, false);
                }

                UpdateCombinationSlotState(avatarAddress, slotIndex, slotState);
                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarStateAsync(eval).Forget();

                if (slotState.PetId.HasValue)
                {
                    UpdatePetState(avatarAddress, slotState.PetId.Value, eval.OutputState);
                }

                slotStates.Add(slotState);
            }

            return (eval, avatarState, slotStates, combinationSlotState);
        }

        private void ResponseRapidCombination(
            (ActionEvaluation<RapidCombination> Evaluation, AvatarState AvatarState, List<CombinationSlotState> CombinationSlotStates, Dictionary<int, CombinationSlotState> CurrentCombinationSlotState) renderArgs)
        {
            var avatarAddress = renderArgs.Evaluation.Action.avatarAddress;
            var slotIndexList = renderArgs.Evaluation.Action.slotIndexList;

            if (renderArgs.CombinationSlotStates is null)
            {
                NcDebug.LogError("CombinationSlotState is null.");
                return;
            }

            if (renderArgs.AvatarState is null)
            {
                NcDebug.LogError("AvatarState is null.");
                return;
            }

            foreach (var slotIndex in slotIndexList)
            {
                var index = slotIndex;
                var result = renderArgs.CombinationSlotStates
                    .Where(state => state.Index == index)
                    .Select(state => (RapidCombination5.ResultModel)state.Result)
                    .FirstOrDefault();

                if (result is null)
                {
                    NcDebug.LogError("Result is null.");
                    continue;
                }

                string formatKey;
                var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;

                var stateResult = renderArgs.CurrentCombinationSlotState[slotIndex]?.Result;
                switch (stateResult)
                {
                    case CombinationConsumable5.ResultModel combineResultModel:
                    {
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

                            // CustomEquipmentCraft 액션으로 만들어진 장비의 경우, AddNewResultAttachmentMail 대신 AddNewMail을 써야합니다.
                            if (equipment.ByCustomCraft)
                            {
                                LocalLayerModifier.AddNewMail(avatarAddress, result.id);
                                break;
                            }
                        }
                        else
                        {
                            formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                        }

                        LocalLayerModifier.AddNewResultAttachmentMail(
                            avatarAddress,
                            combineResultModel.id,
                            currentBlockIndex);
                        break;
                    }
                    case ItemEnhancement13.ResultModel enhancementResultModel:
                    {
                        LocalLayerModifier.AddNewResultAttachmentMail(
                            avatarAddress,
                            enhancementResultModel.id,
                            currentBlockIndex);

                        switch (enhancementResultModel.enhancementResult)
                        {
                            case ItemEnhancement13.EnhancementResult.Success:
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                                break;
                            default:
                                NcDebug.LogError(
                                    $"Unexpected result.enhancementResult: {enhancementResultModel.enhancementResult}");
                                formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                                break;
                        }

                        break;
                    }
                    default:
                        NcDebug.LogError(
                            $"Unexpected state.Result: {stateResult}");
                        formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                        break;
                }

                NotificationSystem.CancelReserve(result.itemUsable.ItemId);
                NotificationSystem.Push(
                    result.itemUsable is Equipment { ByCustomCraft: true }
                        ? MailType.CustomCraft
                        : MailType.Workshop,
                    L10nManager.Localize(formatKey, result.itemUsable.GetLocalizedName()),
                    NotificationCell.NotificationType.Notification);

                var pushIdentifierKey = string.Format(WorkshopPushIdentifierKeyFormat, slotIndex);
                var identifier = PlayerPrefs.GetString(pushIdentifierKey, string.Empty);
                if (!string.IsNullOrEmpty(identifier))
                {
                    PushNotifier.CancelReservation(identifier);
                    PlayerPrefs.DeleteKey(pushIdentifierKey);
                }

                Widget.Find<CombinationSlotsPopup>().OnCraftActionRender(slotIndex);
            }
        }

        private ActionEvaluation<CombinationEquipment> PreResponseCombinationEquipment(ActionEvaluation<CombinationEquipment> eval)
        {
            if (eval.Action.payByCrystal)
            {
                Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(false);
            }

            return eval;
        }

        private (ActionEvaluation<CombinationEquipment> Evaluation, AvatarState AvatarState, CombinationSlotState CombinationSlotState)
            PrepareCombinationEquipment(ActionEvaluation<CombinationEquipment> eval)
        {
            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.avatarAddress;
            var slotIndex = eval.Action.slotIndex;
            var slot = GetStateExtensions.GetCombinationSlotState(eval.OutputState, avatarAddress, slotIndex);
            var result = (CombinationConsumable5.ResultModel)slot.Result;

            if (StateGetter.TryGetAvatarState(
                eval.OutputState,
                agentAddress,
                avatarAddress,
                out var avatarState))
            {
                // 액션을 스테이징한 시점에 미리 반영해둔 아이템의 레이어를 먼저 제거하고, 액션의 결과로 나온 실제 상태를 반영
                foreach (var pair in result.materials)
                {
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value, false);
                }

                UpdateCombinationSlotState(avatarAddress, slotIndex, slot);
                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarStateAsync(eval).Forget();

                var hammerPointStateAddress =
                    Addresses.GetHammerPointStateAddress(avatarAddress, result.recipeId);
                var hammerPointState = new HammerPointState(
                    hammerPointStateAddress,
                    StateGetter.GetState(
                        eval.OutputState,
                        ReservedAddresses.LegacyAccount,
                        hammerPointStateAddress) as List);
                States.Instance.UpdateHammerPointStates(result.recipeId, hammerPointState);

                if (eval.Action.petId.HasValue)
                {
                    UpdatePetState(avatarAddress, eval.Action.petId.Value, eval.OutputState);
                }
            }

            return (eval, avatarState, slot);
        }

        private void ResponseCombinationEquipment((ActionEvaluation<CombinationEquipment> Evaluation, AvatarState AvatarState, CombinationSlotState CombinationSlotState) renderArgs)
        {
            if (renderArgs.AvatarState is null)
            {
                return;
            }

            var agentAddress = renderArgs.Evaluation.Signer;
            var avatarAddress = renderArgs.Evaluation.Action.avatarAddress;
            var slot = renderArgs.CombinationSlotState;
            var result = (CombinationConsumable5.ResultModel)slot.Result;
            var avatarState = renderArgs.AvatarState;

            UniTask.RunOnThreadPool(() =>
            {
                LocalLayerModifier.ModifyAgentGold(renderArgs.Evaluation, agentAddress,
                    result.gold);
            });

            LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

            var tableSheets = Game.Game.instance.TableSheets;
            var nextQuest = avatarState.questList?
                .OfType<CombinationEquipmentQuest>()
                .Where(x => !x.Complete)
                .OrderBy(x => x.StageId)
                .FirstOrDefault(x => tableSheets.EquipmentItemRecipeSheet.TryGetValue(
                    x.RecipeId,
                    out _));

            RenderQuest(avatarAddress, avatarState.questList?.completedQuestIds);

            if (nextQuest is not null)
            {
                var isRecipeMatch = nextQuest.RecipeId == renderArgs.Evaluation.Action.recipeId;
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
                // aura, grimoire has not subRecipeId
                var subRecipeId = renderArgs.Evaluation.Action.subRecipeId;
                if (subRecipeId.HasValue &&
                    TableSheets.Instance.EquipmentItemSubRecipeSheetV2.TryGetValue(subRecipeId.Value, out var row))
                {
                    formatKey = equipment.optionCountFromCombination == row.Options.Count
                        ? "NOTIFICATION_COMBINATION_COMPLETE_GREATER"
                        : "NOTIFICATION_COMBINATION_COMPLETE";

                    Widget.Find<CraftResultPopup>().Show(equipment, subRecipeId.Value);
                }
                else
                {
                    formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                }
            }
            else
            {
                NcDebug.LogError(
                    $"[{nameof(ResponseCombinationEquipment)}] result.itemUsable is not Equipment");
                formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
            }

            var format = L10nManager.Localize(formatKey);
            NotificationSystem.Reserve(
                MailType.Workshop,
                string.Format(format, result.itemUsable.GetLocalizedName()),
                slot.UnlockBlockIndex,
                result.itemUsable.ItemId);

            var slotIndex = renderArgs.Evaluation.Action.slotIndex;
            var blockCount = slot.UnlockBlockIndex - Game.Game.instance.Agent.BlockIndex;
            if (blockCount >= WorkshopNotifiedBlockCount)
            {
                var expectedNotifiedTime =
                    BlockIndexExtensions.BlockToTimeSpan(Mathf.RoundToInt(blockCount));
                var notificationText = L10nManager.Localize(
                    "PUSH_WORKSHOP_CRAFT_COMPLETE_CONTENT",
                    result.itemUsable.GetLocalizedNonColoredName(false));
                var identifier = PushNotifier.Push(
                    notificationText,
                    expectedNotifiedTime,
                    PushNotifier.PushType.Workshop);

                var pushIdentifierKey = string.Format(WorkshopPushIdentifierKeyFormat, slotIndex);
                PlayerPrefs.SetString(pushIdentifierKey, identifier);
            }

            Widget.Find<HeaderMenuStatic>().UpdatePortalRewardOnce(HeaderMenuStatic.PortalRewardNotificationCombineKey);
            // ~Notify

            Widget.Find<CombinationSlotsPopup>().OnCraftActionRender(slotIndex);
        }

        private (ActionEvaluation<CombinationConsumable> Evaluation, AvatarState AvatarState, CombinationSlotState CombinationSlotState)
            PrepareCombinationConsumable(ActionEvaluation<CombinationConsumable> eval)
        {
            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.avatarAddress;
            var slotIndex = eval.Action.slotIndex;
            var slot = GetStateExtensions.GetCombinationSlotState(eval.OutputState, avatarAddress, slotIndex);
            var result = (CombinationConsumable5.ResultModel)slot.Result;

            if (StateGetter.TryGetAvatarState(
                eval.OutputState,
                agentAddress,
                avatarAddress,
                out var avatarState))
            {

                // 액션을 스테이징한 시점에 미리 반영해둔 아이템의 레이어를 먼저 제거하고, 액션의 결과로 나온 실제 상태를 반영
                foreach (var pair in result.materials)
                {
                    LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value, false);
                }

                UpdateCombinationSlotState(avatarAddress, slotIndex, slot);
                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarStateAsync(eval).Forget();
            }

            return (eval, avatarState, slot);
        }

        private void ExceptionCombinationEquipment(ActionEvaluation<CombinationEquipment> q)
        {
            var currentAction = q.Action;
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("On Exception CombinationEquipment Action");
            stringBuilder.AppendLine($"avatarAddress: {currentAction.avatarAddress}");
            stringBuilder.AppendLine($"slotIndex: {currentAction.slotIndex}");
            stringBuilder.AppendLine($"petId: {currentAction.petId}");
            stringBuilder.AppendLine($"recipeId: {currentAction.recipeId}");
            stringBuilder.AppendLine($"payByCrystal: {currentAction.payByCrystal}");
            stringBuilder.AppendLine($"subRecipeId: {currentAction.subRecipeId}");
            stringBuilder.AppendLine($"useHammerPoint: {currentAction.useHammerPoint}");

            var exception = q.Exception;
            if (exception != null)
            {
                stringBuilder.AppendLine($"Exception: {exception.Message}");
                stringBuilder.AppendLine($"StackTrace: {exception.StackTrace}");

                var innerException = exception.InnerException;
                if (innerException != null)
                {
                    stringBuilder.AppendLine($"InnerException: {innerException.Message}");
                    stringBuilder.AppendLine($"InnerStackTrace: {innerException.StackTrace}");
                }
            }

            NcDebug.LogError(stringBuilder.ToString());

            // TODO: workshop ui 갱신
        }

        private void ResponseCombinationConsumable(
            (ActionEvaluation<CombinationConsumable> Evaluation, AvatarState AvatarState, CombinationSlotState CombinationSlotState) renderArgs)
        {
            if (renderArgs.AvatarState is null)
            {
                return;
            }

            var agentAddress = renderArgs.Evaluation.Signer;
            var avatarAddress = renderArgs.Evaluation.Action.avatarAddress;
            var slot = renderArgs.CombinationSlotState;
            var result = (CombinationConsumable5.ResultModel)slot.Result;

            UniTask.RunOnThreadPool(() =>
            {
                LocalLayerModifier.ModifyAgentGold(renderArgs.Evaluation, agentAddress,
                    result.gold);
            });

            LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

            RenderQuest(avatarAddress, renderArgs.AvatarState.questList.completedQuestIds);

            // Notify
            var format = L10nManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
            NotificationSystem.Reserve(
                MailType.Workshop,
                string.Format(format, result.itemUsable.GetLocalizedName()),
                slot.UnlockBlockIndex,
                result.itemUsable.ItemId);
            Widget.Find<HeaderMenuStatic>().UpdatePortalRewardOnce(HeaderMenuStatic.PortalRewardNotificationCombineKey);
            // ~Notify

            Widget.Find<CombinationSlotsPopup>()
                .OnCraftActionRender(renderArgs.Evaluation.Action.slotIndex);
        }

        private (ActionEvaluation<EventConsumableItemCrafts> Evaluation, CombinationSlotState CombinationSlotState) PrepareEventConsumableItemCrafts(
            ActionEvaluation<EventConsumableItemCrafts> eval)
        {
            var avatarAddress = eval.Action.AvatarAddress;
            var slotIndex = eval.Action.SlotIndex;
            var slot = GetStateExtensions.GetCombinationSlotState(eval.OutputState, avatarAddress, slotIndex);
            var result = (CombinationConsumable5.ResultModel)slot.Result;
            // 액션을 스테이징한 시점에 미리 반영해둔 아이템의 레이어를 먼저 제거하고, 액션의 결과로 나온 실제 상태를 반영
            foreach (var pair in result.materials)
            {
                LocalLayerModifier.AddItem(avatarAddress, pair.Key.ItemId, pair.Value, false);
            }

            UpdateCombinationSlotState(avatarAddress, slotIndex, slot);
            UpdateAgentStateAsync(eval).Forget();
            UpdateCurrentAvatarStateAsync(eval).Forget();

            return (eval, slot);
        }

        private void ResponseEventConsumableItemCrafts(
            (ActionEvaluation<EventConsumableItemCrafts> Evaluation, CombinationSlotState CombinationSlotState) renderArgs)
        {
            var agentAddress = renderArgs.Evaluation.Signer;
            var avatarAddress = renderArgs.Evaluation.Action.AvatarAddress;
            var slot = renderArgs.CombinationSlotState;
            var result = (CombinationConsumable5.ResultModel)slot.Result;
            var itemUsable = result.itemUsable;

            UniTask.RunOnThreadPool(() =>
            {
                LocalLayerModifier.ModifyAgentGold(renderArgs.Evaluation, agentAddress,
                    result.gold);
            });

            LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

            // Notify
            var format = L10nManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
            NotificationSystem.Reserve(
                MailType.Workshop,
                string.Format(format, result.itemUsable.GetLocalizedName()),
                slot.UnlockBlockIndex,
                result.itemUsable.ItemId);
            Widget.Find<HeaderMenuStatic>().UpdatePortalRewardOnce(HeaderMenuStatic.PortalRewardNotificationCombineKey);
            // ~Notify

            Widget.Find<CombinationSlotsPopup>()
                .OnCraftActionRender(renderArgs.Evaluation.Action.SlotIndex);
        }

        private ActionEvaluation<EventMaterialItemCrafts> PrepareEventMaterialItemCrafts(ActionEvaluation<EventMaterialItemCrafts> eval)
        {
            var materialsToUse = eval.Action.MaterialsToUse;
            // 액션을 스테이징한 시점에 미리 반영해둔 아이템의 레이어를 먼저 제거하고, 액션의 결과로 나온 실제 상태를 반영
            foreach (var material in materialsToUse)
            {
                var id = TableSheets.Instance.MaterialItemSheet[material.Key].ItemId;
                LocalLayerModifier.AddItem(eval.Action.AvatarAddress, id, material.Value, false);
            }

            UpdateAgentStateAsync(eval).Forget();
            UpdateCurrentAvatarStateAsync(eval).Forget();
            return eval;
        }

        private void ResponseEventMaterialItemCrafts(
            ActionEvaluation<EventMaterialItemCrafts> eval)
        {
            var avatarAddress = eval.Action.AvatarAddress;
            var materialsToUse = eval.Action.MaterialsToUse;
            var recipe = TableSheets.Instance.EventMaterialItemRecipeSheet[
                eval.Action.EventMaterialItemRecipeId];
            var resultItem = ItemFactory.CreateMaterial(
                TableSheets.Instance.MaterialItemSheet,
                recipe.ResultMaterialItemId);


            // Notify
            var format = L10nManager.Localize(
                "NOTIFICATION_COMBINATION_COMPLETE",
                resultItem.GetLocalizedName(false));
            NotificationSystem.Reserve(MailType.Workshop, format, 1, Guid.Empty);
            Widget.Find<HeaderMenuStatic>().UpdatePortalRewardOnce(HeaderMenuStatic.PortalRewardNotificationCombineKey);
            // ~Notify
        }

        private ActionEvaluation<ItemEnhancement> PreResponseItemEnhancement(ActionEvaluation<ItemEnhancement> eval)
        {
            Widget.Find<HeaderMenuStatic>().Crystal.SetProgressCircle(false);
            return eval;
        }

        private (ActionEvaluation<ItemEnhancement> Evaluation, AvatarState AvatarState, CombinationSlotState CombinationSlotState)
            PrepareItemEnhancement(ActionEvaluation<ItemEnhancement> eval)
        {
            var agentAddress = eval.Signer;
            var avatarAddress = eval.Action.avatarAddress;
            var slotIndex = eval.Action.slotIndex;
            var slot = GetStateExtensions.GetCombinationSlotState(eval.OutputState, avatarAddress, slotIndex);
            var result = (ItemEnhancement13.ResultModel)slot.Result;
            var itemUsable = result.itemUsable;

            if (StateGetter.TryGetAvatarState(
                eval.OutputState,
                agentAddress,
                avatarAddress,
                out var avatarState))
            {
                // 액션을 스테이징한 시점에 미리 반영해둔 아이템의 레이어를 먼저 제거하고, 액션의 결과로 나온 실제 상태를 반영
                LocalLayerModifier.AddNonFungibleItem(avatarAddress, eval.Action.itemId, false);
                foreach (var tradableId in result.materialItemIdList)
                {
                    if (avatarState.inventory.TryGetNonFungibleItem(
                        tradableId,
                        out ItemUsable materialItem))
                    {
                        if (itemUsable.ItemSubType is ItemSubType.Aura or ItemSubType.Grimoire)
                        {
                            // Because aura is a tradable item, removal or addition in local layer will fail and exceptions will be handled.
                            LocalLayerModifier.AddNonFungibleItem(avatarAddress, tradableId, false);
                        }
                        else
                        {
                            LocalLayerModifier.AddItem(
                                avatarAddress,
                                tradableId,
                                materialItem.RequiredBlockIndex,
                                1,
                                false);
                        }
                    }
                }

                UpdateCombinationSlotState(avatarAddress, slotIndex, slot);
                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarStateAsync(eval).Forget();
            }

            return (eval, avatarState, slot);
        }

        private void ResponseItemEnhancement(
            (ActionEvaluation<ItemEnhancement> Evaluation, AvatarState AvatarState, CombinationSlotState CombinationSlotState) renderArgs)
        {
            if (renderArgs.AvatarState is null)
            {
                return;
            }

            var agentAddress = renderArgs.Evaluation.Signer;
            var avatarAddress = renderArgs.Evaluation.Action.avatarAddress;
            var result = (ItemEnhancement13.ResultModel)renderArgs.CombinationSlotState.Result;

            UniTask.RunOnThreadPool(() =>
            {
                LocalLayerModifier.ModifyAgentGold(renderArgs.Evaluation, agentAddress,
                    result.gold);
                LocalLayerModifier.ModifyAgentCrystal(renderArgs.Evaluation, agentAddress,
                    -result.CRYSTAL.MajorUnit);
            });

            LocalLayerModifier.AddNewAttachmentMail(avatarAddress, result.id);

            RenderQuest(avatarAddress, renderArgs.AvatarState.questList.completedQuestIds);

            // Notify
            string formatKey;
            switch (result.enhancementResult)
            {
                /*case Action.ItemEnhancement.EnhancementResult.GreatSuccess:
                    formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_GREATER";
                    break;*/
                case ItemEnhancement13.EnhancementResult.Success:
                    formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                    break;
                /*case Action.ItemEnhancement.EnhancementResult.Fail:
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
                    break;*/
                default:
                    NcDebug.LogError(
                        $"Unexpected result.enhancementResult: {result.enhancementResult}");
                    formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                    break;
            }

            var format = L10nManager.Localize(formatKey);
            NotificationSystem.Reserve(
                MailType.Workshop,
                string.Format(format, result.itemUsable.GetLocalizedName()),
                renderArgs.CombinationSlotState.UnlockBlockIndex,
                result.itemUsable.ItemId);

            var slotIndex = renderArgs.Evaluation.Action.slotIndex;
            var blockCount = renderArgs.CombinationSlotState.UnlockBlockIndex - Game.Game.instance.Agent.BlockIndex;
            if (blockCount >= WorkshopNotifiedBlockCount)
            {
                var expectedNotifiedTime =
                    BlockIndexExtensions.BlockToTimeSpan(Mathf.RoundToInt(blockCount));
                var notificationText = L10nManager.Localize(
                    "PUSH_WORKSHOP_UPGRADE_COMPLETE_CONTENT",
                    result.itemUsable.GetLocalizedNonColoredName(false));
                var identifier = PushNotifier.Push(
                    notificationText,
                    expectedNotifiedTime,
                    PushNotifier.PushType.Workshop);

                var pushIdentifierKey = string.Format(WorkshopPushIdentifierKeyFormat, slotIndex);
                PlayerPrefs.SetString(pushIdentifierKey, identifier);
            }
            // ~Notify

            var avatarSlotIndex = States.Instance.AvatarStates
                .FirstOrDefault(x => x.Value.address == renderArgs.Evaluation.Action.avatarAddress).Key;
            var itemSlotStates = States.Instance.ItemSlotStates[avatarSlotIndex];

            for (var i = 1; i < (int)BattleType.End; i++)
            {
                var battleType = (BattleType)i;
                var currentItemSlotState = States.Instance.CurrentItemSlotStates[battleType];
                currentItemSlotState.Costumes.Remove(renderArgs.Evaluation.Action.itemId);
                currentItemSlotState.Equipments.Remove(renderArgs.Evaluation.Action.itemId);

                var itemSlotState = itemSlotStates[battleType];
                itemSlotState.Costumes.Remove(renderArgs.Evaluation.Action.itemId);
                itemSlotState.Equipments.Remove(renderArgs.Evaluation.Action.itemId);
            }

            Widget.Find<CombinationSlotsPopup>().OnCraftActionRender(slotIndex);
        }

        private void ResponseAuraSummon(ActionEvaluation<AuraSummon> eval)
        {
            UniTask.RunOnThreadPool(async () =>
            {
                await UpdateAgentStateAsync(eval);
                await UpdateCurrentAvatarStateAsync(eval);
            }).ToObservable().ObserveOnMainThread().Subscribe(_ => { Widget.Find<Summon>().OnActionRender(eval); });
        }

        private void ResponseRuneSummon(ActionEvaluation<RuneSummon> eval)
        {
            UniTask.RunOnThreadPool(async () =>
            {
                await UpdateAgentStateAsync(eval);
                await UpdateCurrentAvatarStateAsync(eval);
                UpdateCurrentAvatarRuneStoneBalance(eval);
            }).ToObservable().ObserveOnMainThread().Subscribe(_ =>
            {
                var action = eval.Action;
                var tableSheets = Game.Game.instance.TableSheets;
                var summonRow = tableSheets.SummonSheet[action.GroupId];
                var materialRow = tableSheets.MaterialItemSheet[summonRow.CostMaterial];
                var count = summonRow.CostMaterialCount * action.SummonCount;

                Widget.Find<Summon>().OnActionRender(eval);
            });
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

                States.Instance.SetCurrentAvatarBalance(eval, assetInfo.Asset.Currency);
                var shopSell = Widget.Find<ShopSell>();
                if (shopSell.isActiveAndEnabled)
                {
                    shopSell.UpdateInventory();
                }

                return;
            }

            var info = eval.Action.RegisterInfos.FirstOrDefault();
            if (info is null)
            {
                return;
            }

            var count = 1;
            var itemName = string.Empty;

            foreach (var registerInfo in eval.Action.RegisterInfos)
            {
                if (registerInfo is RegisterInfo r)
                {
                    LocalLayerModifier.AddItem(r.AvatarAddress, r.TradableId, eval.BlockIndex, r.ItemCount, false);
                }
            }

            UniTask.RunOnThreadPool(async () =>
            {
                switch (info)
                {
                    case RegisterInfo registerInfo:
                        count = registerInfo.ItemCount;
                        var rand = new LocalRandom(eval.RandomSeed);
                        var productId = rand.GenerateRandomGuid();
                        var deriveAddress = Product.DeriveAddress(productId);
                        var rawState = (List)StateGetter.GetState(
                            eval.OutputState, ReservedAddresses.LegacyAccount, deriveAddress);
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
                        States.Instance.SetCurrentAvatarBalance(eval, assetInfo.Asset.Currency);
                        itemName = assetInfo.Asset.GetLocalizedName();
                        count = MathematicsExtensions.ConvertToInt32(assetInfo.Asset.GetQuantityString());
                        break;
                }

                if (eval.Action.ChargeAp)
                {
                    // 액션을 스테이징한 시점에 미리 반영해둔 아이템의 레이어를 먼저 제거하고, 액션의 결과로 나온 실제 상태를 반영
                    var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                        .First(r => r.ItemSubType == ItemSubType.ApStone);
                    LocalLayerModifier.AddItem(eval.Action.AvatarAddress, row.ItemId, 1, false);
                }

                UpdateCurrentAvatarStateAsync(eval).Forget();
                ReactiveAvatarState.UpdateActionPoint(GetActionPoint(eval, eval.Action.AvatarAddress));
                await ReactiveShopState.RequestSellProductsAsync();
            }).ToObservable().ObserveOnMainThread().Subscribe(_ =>
            {
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

                if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.AvatarAddress))
                {
                    GameConfigStateSubject.ActionPointState.Remove(eval.Action.AvatarAddress);
                }

                OneLineSystem.Push(
                    MailType.Auction,
                    message,
                    NotificationCell.NotificationType.Information);
            });
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
                // 액션을 스테이징한 시점에 미리 반영해둔 아이템의 레이어를 먼저 제거하고, 액션의 결과로 나온 실제 상태를 반영
                LocalLayerModifier.AddItem(eval.Action.AvatarAddress, row.ItemId, 1, false);
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
                    await ApiClients.Instance.MarketServiceClient.GetProductInfo(
                        productInfo.ProductId);
                var count = 0;
                if (itemProduct is not null)
                {
                    count = (int)itemProduct.Quantity;
                }

                if (favProduct is not null)
                {
                    count = (int)favProduct.Quantity;
                    var currency = Currencies.GetMinterlessCurrency(favProduct.Ticker);
                    UniTask.RunOnThreadPool(() => { States.Instance.SetCurrentAvatarBalance(eval, currency); }).Forget();
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

            UniTask.RunOnThreadPool(async () =>
            {
                await UpdateCurrentAvatarStateAsync(eval);
                await ReactiveShopState.RequestSellProductsAsync();
                ReactiveAvatarState.UpdateActionPoint(GetActionPoint(eval, eval.Action.AvatarAddress));
            }).Forget();
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
                // 액션을 스테이징한 시점에 미리 반영해둔 아이템의 레이어를 먼저 제거하고, 액션의 결과로 나온 실제 상태를 반영
                LocalLayerModifier.AddItem(eval.Action.AvatarAddress, row.ItemId, 1, false);
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
                    await ApiClients.Instance.MarketServiceClient.GetProductInfo(
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

            await ReactiveShopState.RequestSellProductsAsync();

            UniTask.RunOnThreadPool(async () =>
            {
                await UpdateCurrentAvatarStateAsync(eval);
                ReactiveAvatarState.UpdateActionPoint(GetActionPoint(eval, eval.Action.AvatarAddress));
            }).Forget();
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
            UniTask.RunOnThreadPool(async () =>
            {
                await UpdateAgentStateAsync(eval);
                await UpdateCurrentAvatarStateAsync(eval);
            }).ToObservable().ObserveOnMainThread().Subscribe(unit =>
            {
                if (eval.Action.AvatarAddress == avatarAddress) // buyer
                {
                    foreach (var info in productInfos)
                    {
                        var count = 0;
                        var productMail =
                            States.Instance.CurrentAvatarState.mailBox.FirstOrDefault(mail =>
                                mail.id == info.ProductId) as ProductBuyerMail;
                        string itemName = string.Empty;

                        if (productMail?.Product is ItemProduct itemProduct)
                        {
                            if (States.Instance.CurrentAvatarState.inventory.TryGetTradableItem(
                                itemProduct.TradableItem.TradableId,
                                itemProduct.TradableItem.RequiredBlockIndex, itemProduct.ItemCount,
                                out var boughtItem))
                            {
                                count = itemProduct.ItemCount;
                                itemName = boughtItem.item.GetLocalizedName();
                            }
                        }

                        if (productMail?.Product is FavProduct favProduct)
                        {
                            count = (int) favProduct.Asset.MajorUnit;
                            itemName = favProduct.Asset.GetLocalizedName();

                            UniTask.RunOnThreadPool(() =>
                            {
                                States.Instance.SetCurrentAvatarBalance(eval,
                                    favProduct.Asset.Currency);
                            }).Forget();
                        }

                        var price = info.Price;

                        UniTask.RunOnThreadPool(() =>
                        {
                            LocalLayerModifier.ModifyAgentGold(eval, agentAddress, price);
                        });
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
                        var count = 0;
                        var productMail =
                            States.Instance.CurrentAvatarState.mailBox.FirstOrDefault(mail =>
                                mail.id == info.ProductId) as ProductSellerMail;
                        var itemName = productMail.Product is ItemProduct itemProduct
                            ? itemProduct.TradableItem.ItemSubType.GetLocalizedString()
                            : ((FavProduct) productMail.Product).Asset.GetLocalizedInformation();
                        if (productMail.Product is ItemProduct itemProd)
                        {
                            count = itemProd.ItemCount;
                        }

                        if (productMail.Product is FavProduct favProd)
                        {
                            UniTask.RunOnThreadPool(() =>
                            {
                                States.Instance.SetCurrentAvatarBalance(eval, favProd.Asset.Currency);
                            }).Forget();
                        }

                        var taxedPrice = productMail!.Product.Price.DivRem(100, out _) * Buy.TaxRate;
                        UniTask.RunOnThreadPool(() =>
                        {
                            LocalLayerModifier.ModifyAgentGold(eval, agentAddress, -taxedPrice);
                        });
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

                Widget.Find<HeaderMenuStatic>()
                    .UpdatePortalRewardOnce(HeaderMenuStatic.PortalRewardNotificationTradingKey);

                RenderQuest(avatarAddress,
                    States.Instance.CurrentAvatarState.questList.completedQuestIds);
            });
        }

        /// <summary>
        /// This method is used to preprocess a daily reward evaluation before returning it.
        /// 액션 성공 여부와 관계없이 로딩 UI를 초기화 시키는 동작을 합니다.
        /// </summary>
        /// <param name="eval">The action evaluation for getting avatar address.</param>
        /// <returns>the eval inputted by param.</returns>
        private ActionEvaluation<DailyReward> PreResponseDailyReward(ActionEvaluation<DailyReward> eval)
        {
            if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.avatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(eval.Action.avatarAddress);
            }

            States.Instance.SetCurrentAvatarBalance(StateGetter.GetBalance(
                eval.OutputState, eval.Action.avatarAddress, RuneHelper.DailyRewardRune));

            return eval;
        }

        /// <summary>
        /// Method to handle the response for daily reward.
        /// ThreadPool에서 아바타 상태를 업데이트 한 뒤, 메인 스레드에서 렌더에 관련된 동작을 처리합니다.
        /// </summary>
        /// <param name="eval">The action evaluation for render daily reward.</param>
        private void ResponseDailyReward(ActionEvaluation<DailyReward> eval)
        {
            // 액션이 정상적으로 실행되면 최대치로 채워지리라 예상, 최적화를 위해 GetState를 하지 않고 Set합니다.
            ReactiveAvatarState.UpdateActionPoint(Action.DailyReward.ActionPointMax);
            ReactiveAvatarState.UpdateDailyRewardReceivedIndex(eval.BlockIndex);
            LocalLayer.Instance.ClearAvatarModifiers<AvatarDailyRewardReceivedIndexModifier>(
                eval.Action.avatarAddress);

            NotificationSystem.Push(
                MailType.System,
                L10nManager.Localize("UI_RECEIVED_DAILY_REWARD"),
                NotificationCell.NotificationType.Notification);
            var expectedNotifiedTime = BlockIndexExtensions.BlockToTimeSpan(Mathf.RoundToInt(
                Action.DailyReward.DailyRewardInterval));
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
            const int amount = Action.DailyReward.DailyRuneRewardAmount;
            NotificationSystem.Push(
                MailType.System,
                $" {L10nManager.Localize("OBTAIN")} : {runeName} x {amount}",
                NotificationCell.NotificationType.RuneAcquisition);
        }

        private (ActionEvaluation<HackAndSlash> eval,
            AvatarState nextAvatarState,
            CrystalRandomSkillState prevSkillState,
            CrystalRandomSkillState updatedSkillState,
            long actionPoint) PrepareHackAndSlash(ActionEvaluation<HackAndSlash> eval)
        {
            if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
            {
                return (eval, null, null, null, 0L);
            }

            var prevState = eval.PreviousState;
            var outputState = eval.OutputState;
            var avatarAddr = eval.Action.AvatarAddress;

            var prevAvatarState = UpdatePreviousAvatarState(eval.PreviousState, eval.Action.AvatarAddress);

            // 전투에서 사용한 장비와 룬은 Action 인자로 들어가기 때문에, OutputState로 갱신시켜줍니다.
            UpdateCurrentAvatarItemSlotState(eval, BattleType.Adventure);
            UpdateCurrentAvatarRuneSlotState(eval, BattleType.Adventure);

            CrystalRandomSkillState prevSkillState = null;
            if (!prevAvatarState.worldInformation
                .IsStageCleared(eval.Action.StageId))
            {
                prevSkillState = GetCrystalRandomSkillState(prevState);
            }

            var updatedSkillState = GetCrystalRandomSkillState(outputState);
            var nextAvatarState =
                StateGetter.GetAvatarState(outputState, avatarAddr);

            return (eval,
                nextAvatarState,
                prevSkillState,
                updatedSkillState,
                GetActionPoint(eval, eval.Action.AvatarAddress));
        }

        private void ResponseHackAndSlashAsync((ActionEvaluation<HackAndSlash>, AvatarState, CrystalRandomSkillState, CrystalRandomSkillState, long) prepared)
        {
            var (eval, newAvatarState, prevSkillState, newRandomSkillState, actionPoint) = prepared;
            if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
            {
                return;
            }

            _disposableForBattleEnd?.Dispose();
            _disposableForBattleEnd =
                Game.Game.instance.Stage.OnEnterToStageEnd
                    .First()
                    .Subscribe(_ =>
                    {
                        UniTask.Void(async () =>
                        {
                            try
                            {
                                Widget.Find<WorldMap>().SetWorldInformation(newAvatarState.worldInformation);
                                await UpdateCurrentAvatarStateAsync(newAvatarState);
                                ReactiveAvatarState.UpdateActionPoint(actionPoint);
                                States.Instance.SetCrystalRandomSkillState(newRandomSkillState);
                                RenderQuest(
                                    eval.Action.AvatarAddress,
                                    newAvatarState.questList.completedQuestIds);
                                _disposableForBattleEnd = null;
                                Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                            }
                            catch (Exception e)
                            {
                                NcDebug.LogException(e);
                            }
                        });
                    });

            var tableSheets = TableSheets.Instance;
            var skillsOnWaveStart = new List<Skill>();
            if (prevSkillState != null && prevSkillState.StageId == eval.Action.StageId && prevSkillState.SkillIds.Any())
            {
                var actionArgsBuffId = eval.Action.StageBuffId;
                var skillId =
                    actionArgsBuffId.HasValue &&
                    prevSkillState.SkillIds.Contains(actionArgsBuffId.Value)
                        ? actionArgsBuffId.Value
                        : prevSkillState.GetHighestRankSkill(tableSheets.CrystalRandomBuffSheet);
                var skill = CrystalRandomSkillState.GetSkill(
                    skillId,
                    tableSheets.CrystalRandomBuffSheet,
                    tableSheets.SkillSheet);
                skillsOnWaveStart.Add(skill);
            }

            var tempPlayer = (AvatarState)States.Instance.CurrentAvatarState.Clone();
            tempPlayer.EquipEquipments(States.Instance.CurrentItemSlotStates[BattleType.Adventure].Equipments);
            var resultModel = eval.GetHackAndSlashReward(
                tempPlayer,
                States.Instance.AllRuneState,
                States.Instance.CurrentRuneSlotStates[BattleType.Adventure],
                States.Instance.CollectionState,
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
                        ["RandomSkillId"] = eval.Action.StageBuffId.Value,
                        ["IsCleared"] = simulator.Log.IsClear,
                        ["AvatarAddress"] =
                            States.Instance.CurrentAvatarState.address.ToString(),
                        ["AgentAddress"] = States.Instance.AgentState.address.ToString()
                    });

                var evt = new AirbridgeEvent("Use_Crystal_Bonus_Skill");
                evt.SetValue(eval.Action.StageBuffId.Value);
                evt.AddCustomAttribute("is-clear", simulator.Log.IsClear);
                evt.AddCustomAttribute("agent-address", States.Instance.AgentState.address.ToString());
                evt.AddCustomAttribute("avatar-address", States.Instance.CurrentAvatarState.address.ToString());
                AirbridgeUnity.TrackEvent(evt);
            }

            BattleRenderer.Instance.PrepareStage(log);
        }

        private void ExceptionHackAndSlash(ActionEvaluation<HackAndSlash> eval)
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

        private ActionEvaluation<HackAndSlashSweep> PrepareHackAndSlashSweepAsync(
            ActionEvaluation<HackAndSlashSweep> eval)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var avatarState = StateGetter.GetAvatarState(eval.OutputState, avatarAddress);
            if (eval.Action.apStoneCount > 0)
            {
                var row = TableSheets.Instance.MaterialItemSheet.Values.First(r =>
                    r.ItemSubType == ItemSubType.ApStone);
                // 액션을 스테이징한 시점에 미리 반영해둔 아이템의 레이어를 먼저 제거하고, 액션의 결과로 나온 실제 상태를 반영
                LocalLayerModifier.AddItem(avatarAddress, row.ItemId, eval.Action.apStoneCount, false);
            }

            UpdateCurrentAvatarStateAsync(avatarState).Forget();
            UpdateCurrentAvatarItemSlotState(eval, BattleType.Adventure);
            UpdateCurrentAvatarRuneSlotState(eval, BattleType.Adventure);
            var actionPoint = GetActionPoint(eval, avatarAddress);
            ReactiveAvatarState.UpdateActionPoint(actionPoint);
            return eval;
        }

        private void ResponseHackAndSlashSweepAsync(
            ActionEvaluation<HackAndSlashSweep> eval)
        {
            Widget.Find<SweepResultPopup>().OnActionRender(new LocalRandom(eval.RandomSeed));
            Widget.Find<BattlePreparation>().UpdateInventoryView();
        }

        private void ExceptionHackAndSlashSweep(ActionEvaluation<HackAndSlashSweep> eval)
        {
            Widget.Find<SweepResultPopup>().Close();
            Game.Game.BackToMainAsync(eval.Exception.InnerException).Forget();
        }

        private ActionEvaluation<EventDungeonBattle> PrepareEventDungeonBattle(
            ActionEvaluation<EventDungeonBattle> eval)
        {
            if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
            {
                return eval;
            }

            if (eval.Action.BuyTicketIfNeeded)
            {
                UpdateAgentStateAsync(eval).Forget();
            }

            UpdatePreviousAvatarState(eval.PreviousState, eval.Action.AvatarAddress);

            UpdateCurrentAvatarItemSlotState(eval, BattleType.Adventure);
            UpdateCurrentAvatarRuneSlotState(eval, BattleType.Adventure);

            _disposableForBattleEnd?.Dispose();
            _disposableForBattleEnd =
                Game.Game.instance.Stage.OnEnterToStageEnd
                    .First()
                    .Subscribe(_ =>
                    {
                        var task = UniTask.RunOnThreadPool(async () =>
                        {
                            await UpdateCurrentAvatarStateAsync(eval);
                            await RxProps.EventDungeonInfo.UpdateAsync(eval.OutputState);
                            _disposableForBattleEnd = null;
                            Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                        }, false);
                        task.ToObservable()
                            .First()
                            // ReSharper disable once ConvertClosureToMethodGroup
                            .DoOnError(e => NcDebug.LogException(e));
                    });
            return eval;
        }

        private void ResponseEventDungeonBattleAsync(
            ActionEvaluation<EventDungeonBattle> eval)
        {
            if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
            {
                return;
            }

            var playCount = Action.EventDungeonBattle.PlayCount;
            // NOTE: This is a temporary solution. The formula is not yet decided.
            var random = new LocalRandom(eval.RandomSeed);
            var stageId = eval.Action.EventDungeonStageId;
            var stageRow = TableSheets.Instance.EventDungeonStageSheet[stageId];
            var tableSheets = TableSheets.Instance;
            var simulator = new StageSimulator(
                random,
                States.Instance.CurrentAvatarState,
                eval.Action.Foods,
                States.Instance.AllRuneState,
                States.Instance.CurrentRuneSlotStates[BattleType.Adventure],
                new List<Skill>(),
                eval.Action.EventDungeonId,
                stageId,
                stageRow,
                TableSheets.Instance.EventDungeonStageWaveSheet[stageId],
                RxProps.EventDungeonInfo.Value?.IsCleared(stageId) ?? false,
                RxProps.EventScheduleRowForDungeon.Value.GetStageExp(
                    stageId.ToEventDungeonStageNumber(),
                    Action.EventDungeonBattle.PlayCount),
                TableSheets.Instance.GetStageSimulatorSheets(),
                TableSheets.Instance.EnemySkillSheet,
                TableSheets.Instance.CostumeStatSheet,
                StageSimulator.GetWaveRewards(
                    random,
                    stageRow,
                    TableSheets.Instance.MaterialItemSheet,
                    Action.EventDungeonBattle.PlayCount),
                States.Instance.CollectionState.GetEffects(tableSheets.CollectionSheet),
                tableSheets.DeBuffLimitSheet,
                tableSheets.BuffLinkSheet,
                true,
                States.Instance.GameConfigState.ShatterStrikeMaxDamage);
            simulator.Simulate();
            var log = simulator.Log;
            var stage = Game.Game.instance.Stage;
            stage.StageType = StageType.EventDungeon;
            stage.PlayCount = playCount;

            BattleRenderer.Instance.PrepareStage(log);
        }

        private void ExceptionEventDungeonBattle(ActionEvaluation<EventDungeonBattle> eval)
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

            Game.Game.BackToMainAsync(eval.Exception?.InnerException, showLoadingScreen)
                .Forget();
        }

        private void ResponseRedeemCode(ActionEvaluation<Action.RedeemCode> eval)
        {
            var key = "UI_REDEEM_CODE_INVALID_CODE";
            RedeemCodeState redeem = null;
            UniTask.RunOnThreadPool(() =>
            {
                if (StateGetter.TryGetRedeemCodeState(eval.OutputState, out redeem))
                {
                    UpdateCurrentAvatarStateAsync(eval).Forget();
                }
            }).ToObservable().ObserveOnMainThread().Subscribe(_ =>
            {
                if (eval.Exception is null && redeem is not null)
                {
                    Widget.Find<CodeRewardPopup>().Show(
                        eval.Action.Code,
                        redeem);
                    key = "UI_REDEEM_CODE_SUCCESS";
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
            });
        }

        /// <summary>
        /// Handles the response of charging action points.
        /// </summary>
        /// <param name="eval">The evaluation result of the action point charging operation.</param>
        private void ResponseChargeActionPoint(ActionEvaluation<ChargeActionPoint> eval)
        {
            if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.avatarAddress))
            {
                GameConfigStateSubject.ActionPointState.Remove(eval.Action.avatarAddress);
            }

            if (eval.Exception is not null)
            {
                NcDebug.LogError($"Failed to charge action point. {eval.Exception}");
                return;
            }

            // Observe on main thread
            UniTask.Run(async () =>
            {
                var avatarAddress = eval.Action.avatarAddress;
                var row = TableSheets.Instance.MaterialItemSheet.Values
                    .First(r => r.ItemSubType == ItemSubType.ApStone);
                LocalLayerModifier.AddItem(avatarAddress, row.ItemId, 1, false);

                await UniTask.SwitchToThreadPool();
                await UpdateCurrentAvatarStateAsync(eval);

                await UniTask.SwitchToMainThread();
                // 액션이 정상적으로 실행되면 최대치로 채워지리라 예상, 최적화를 위해 GetState를 하지 않고 Set합니다.
                ReactiveAvatarState.UpdateActionPoint(Action.DailyReward.ActionPointMax);
            });
        }

        private ActionEvaluation<TransferAsset> PrepareTransferAsset(
            ActionEvaluation<TransferAsset> eval)
        {
            UpdateAgentStateAsync(eval).Forget();
            return eval;
        }

        private void ResponseTransferAsset(ActionEvaluation<TransferAsset> eval)
        {
            TransferAssetInternal(
                eval.OutputState,
                eval.Action.Sender,
                eval.Action.Recipient,
                eval.Action.Amount);
        }

        private ActionEvaluation<TransferAssets> PrepareTransferAssets(
            ActionEvaluation<TransferAssets> eval)
        {
            UpdateAgentStateAsync(eval).Forget();
            return eval;
        }

        private void ResponseTransferAssets(ActionEvaluation<TransferAssets> eval)
        {
            foreach (var (recipientAddress, amount) in eval.Action.Recipients)
            {
                TransferAssetInternal(
                    eval.OutputState,
                    eval.Action.Sender,
                    recipientAddress,
                    amount);
            }
        }

        private static void TransferAssetInternal(
            HashDigest<SHA256> outputState,
            Address senderAddr,
            Address recipientAddress,
            FungibleAssetValue amount)
        {
            var currentAgentAddress = States.Instance.AgentState.address;
            var currentAvatarAddress = States.Instance.CurrentAvatarState.address;
            var playToEarnRewardAddress = new Address("d595f7e85e1757d6558e9e448fa9af77ab28be4c");
            if (senderAddr == currentAgentAddress)
            {
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
                if (senderAddr == playToEarnRewardAddress)
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
                            senderAddr),
                        NotificationCell.NotificationType.Notification);
                }
            }
            else if (recipientAddress == currentAvatarAddress)
            {
                var currency = amount.Currency;
                States.Instance.CurrentAvatarBalances[currency.Ticker] =
                    StateGetter.GetBalance(
                        outputState,
                        currentAvatarAddress,
                        currency);
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize(
                        "UI_TRANSFERASSET_NOTIFICATION_RECIPIENT",
                        amount,
                        senderAddr),
                    NotificationCell.NotificationType.Notification);
            }
        }

        private bool ValidateGrindingMailExists(ActionEvaluation<Grinding> eval)
        {
            var avatarAddress = eval.Action.AvatarAddress;
            var avatarState = StateGetter.GetAvatarState(eval.OutputState, avatarAddress);
            var mail = avatarState.mailBox.OfType<GrindingMail>()
                .FirstOrDefault(m => m.id.Equals(eval.Action.Id));
            return mail is not null;
        }

        private (ActionEvaluation<Grinding> eval, List<Equipment> equipmentList)
            PrepareGrinding(ActionEvaluation<Grinding> eval)
        {
            var avatarAddress = eval.Action.AvatarAddress;
            if (eval.Action.ChargeAp)
            {
                // 액션을 스테이징한 시점에 미리 반영해둔 아이템의 레이어를 먼저 제거하고, 액션의 결과로 나온 실제 상태를 반영
                var row = TableSheets.Instance.MaterialItemSheet.Values.First(r =>
                    r.ItemSubType == ItemSubType.ApStone);
                LocalLayerModifier.AddItem(avatarAddress, row.ItemId, 1, false);

                if (GameConfigStateSubject.ActionPointState.ContainsKey(eval.Action.AvatarAddress))
                {
                    GameConfigStateSubject.ActionPointState.Remove(eval.Action.AvatarAddress);
                }
            }

            UpdateCurrentAvatarStateAsync(eval).Forget();
            UpdateAgentStateAsync(eval).Forget();
            ReactiveAvatarState.UpdateActionPoint(GetActionPoint(eval, eval.Action.AvatarAddress));

            var inventory = StateGetter.GetInventory(eval.PreviousState, eval.Action.AvatarAddress);
            var equipmentList = new List<Equipment>();
            foreach (var equipmentId in eval.Action.EquipmentIds)
            {
                if (!inventory.TryGetNonFungibleItem(equipmentId, out Equipment equipment) ||
                    equipment.RequiredBlockIndex > eval.BlockIndex ||
                    !inventory.RemoveNonFungibleItem(equipmentId))
                {
                    Debug.LogError($"Grinding action failed to remove equipment {equipmentId}");
                    OneLineSystem.Push(
                        MailType.Grinding,
                        L10nManager.Localize("ERROR_UNKNOWN"),
                        NotificationCell.NotificationType.Alert);
                    return (eval, new List<Equipment>());
                }

                equipmentList.Add(equipment);
            }

            return (eval, equipmentList);
        }

        private void ResponseGrinding((ActionEvaluation<Grinding> eval, List<Equipment> equipmentList) prepared)
        {
            var crystalReward = CrystalCalculator.CalculateCrystal(
                prepared.equipmentList,
                false,
                TableSheets.Instance.CrystalEquipmentGrindingSheet,
                TableSheets.Instance.CrystalMonsterCollectionMultiplierSheet,
                States.Instance.StakingLevel);
            var itemRewards = Action.Grinding.CalculateMaterialReward(
                    prepared.equipmentList,
                    TableSheets.Instance.CrystalEquipmentGrindingSheet,
                    TableSheets.Instance.MaterialItemSheet)
                .OrderBy(pair => pair.Key.GetMaterialPriority())
                .ThenByDescending(pair => pair.Key.Grade)
                .ThenBy(pair => pair.Key.Id)
                .Select(pair => ((ItemBase)pair.Key, pair.Value)).ToArray();

            var mailRewards = new List<MailReward> { new(crystalReward, (int)crystalReward.MajorUnit) };
            mailRewards.AddRange(itemRewards.Select(pair => new MailReward(pair.Item1, pair.Item2)));

            Widget.Find<RewardScreen>().Show(mailRewards, "NOTIFICATION_CLAIM_GRINDING_REWARD");
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
            UniTask.RunOnThreadPool(() =>
            {
                LocalLayerModifier.ModifyAgentCrystal(
                    eval,
                    States.Instance.AgentState.address,
                    cost.MajorUnit);
                UniTask.WhenAll(
                    UpdateCurrentAvatarStateAsync(eval),
                    UpdateAgentStateAsync(eval));
            }).ToObservable().ObserveOnMainThread().Subscribe(_ =>
            {
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
            });
        }

        private void ResponseUnlockWorld(ActionEvaluation<UnlockWorld> eval)
        {
            Widget.Find<LoadingScreen>().Close();

            if (eval.Exception is not null)
            {
                NcDebug.LogError($"unlock world exc : {eval.Exception.InnerException}");
                return;
            }

            var worldMap = Widget.Find<WorldMap>();
            worldMap.SharedViewModel.UnlockedWorldIds.AddRange(eval.Action.WorldIds);
            worldMap.SetWorldInformation(States.Instance.CurrentAvatarState.worldInformation);

            UniTask.RunOnThreadPool(async () => { await UpdateAgentStateAsync(eval); }).Forget();
        }

        private ActionEvaluation<HackAndSlashRandomBuff> PrepareHackAndSlashRandomBuff(
            ActionEvaluation<HackAndSlashRandomBuff> eval)
        {
            UpdateCurrentAvatarStateAsync(eval).Forget();
            UpdateAgentStateAsync(eval).Forget();
            UpdateCrystalRandomSkillState(eval);
            return eval;
        }

        private void ResponseHackAndSlashRandomBuff(
            ActionEvaluation<HackAndSlashRandomBuff> eval)
        {
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

            UniTask.RunOnThreadPool(async () =>
            {
                await UpdateStakeStateAsync(eval);
                await UpdateAgentStateAsync(eval);
                await UpdateCurrentAvatarStateAsync(eval);
            }).ToObservable().ObserveOnMainThread().Subscribe(_ =>
            {
                NotificationSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_MONSTERCOLLECTION_UPDATED"),
                    NotificationCell.NotificationType.Information);

                Widget.Find<StakingPopup>().SetView();
            });
        }

        private void ResponseClaimStakeReward(ActionEvaluation<ActionBase> eval)
        {
            LoadingHelper.ClaimStakeReward.Value = false;
            if (eval.Exception is not null)
            {
                return;
            }

            var prevStakeState = States.Instance.StakeStateV2.GetValueOrDefault();
            UniTask.RunOnThreadPool(async () =>
            {
                await UpdateStakeStateAsync(eval);
                await UpdateCurrentAvatarStateAsync(eval);
                UpdateCrystalBalance(eval);
                UpdateCurrentAvatarRuneStoneBalance(eval);
            }).ToObservable().ObserveOnMainThread().Subscribe(_ =>
            {
                // Calculate rewards~
                var stakeRegularFixedRewardSheet = States.Instance.StakeRegularFixedRewardSheet;
                var stakeRegularRewardSheet = States.Instance.StakeRegularRewardSheet;
                var stakingLevel = States.Instance.StakingLevel;
                var stakedNcg = States.Instance.StakedBalanceState.Gold;
                var itemSheet = TableSheets.Instance.ItemSheet;
                // The first reward is given at the claimable block index.
                var rewardSteps = prevStakeState.ClaimableBlockIndex == eval.BlockIndex
                    ? 1
                    : 1 + (int)Math.DivRem(
                        eval.BlockIndex - prevStakeState.ClaimableBlockIndex,
                        prevStakeState.Contract.RewardInterval,
                        out var _);
                var rand = new LocalRandom(eval.RandomSeed);
                var rewardItems = StakeRewardCalculator.CalculateFixedRewards(stakingLevel, rand,
                    stakeRegularFixedRewardSheet, itemSheet, rewardSteps);
                var (itemRewards, favs) = StakeRewardCalculator.CalculateRewards(GoldCurrency, stakedNcg, stakingLevel, rewardSteps,
                    stakeRegularRewardSheet, itemSheet, rand);
                // ~Calculate rewards

                var mailRewards = new List<MailReward>();
                foreach (var rewardPair in itemRewards)
                {
                    if (rewardItems.Keys.FirstOrDefault(key => key.Id == rewardPair.Key.Id) is { } itemBase)
                    {
                        rewardItems[itemBase] += rewardPair.Value;
                    }
                    else
                    {
                        rewardItems.Add(rewardPair.Key, rewardPair.Value);
                    }
                }

                mailRewards.AddRange(rewardItems.Select(pair => new MailReward(pair.Key, pair.Value)));
                mailRewards.AddRange(favs.Select(fav => new MailReward(fav, (int)fav.MajorUnit)));

                Widget.Find<RewardScreen>().Show(mailRewards, "NOTIFICATION_CLAIM_MONSTER_COLLECTION_REWARD_COMPLETE");
                Widget.Find<StakingPopup>().SetView();
            });
        }


        internal class LocalRandom : System.Random, IRandom
        {
            public int Seed { get; }

            public LocalRandom(int seed) : base(seed)
            {
                Seed = seed;
            }
        }

        private static ActionEvaluation<JoinArena> PrepareJoinArena(
            ActionEvaluation<JoinArena> eval)
        {
            UpdateCrystalBalance(eval);
            UpdateCurrentAvatarItemSlotState(eval, BattleType.Arena);
            UpdateCurrentAvatarRuneSlotState(eval, BattleType.Arena);
            return eval;
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

            var currentRound = TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(
                Game.Game.instance.Agent.BlockIndex);
            if (eval.Action.championshipId == currentRound.ChampionshipId &&
                eval.Action.round == currentRound.Round)
            {
                await UniTask.WhenAll(
                    RxProps.ArenaInfoTuple.UpdateAsync(eval.OutputState),
                    RxProps.ArenaInformationOrderedWithScore.UpdateAsync(eval.OutputState));
            }
            else
            {
                await RxProps.ArenaInfoTuple.UpdateAsync(eval.OutputState);
            }

            if (arenaJoin && arenaJoin.IsActive())
            {
                arenaJoin.OnRenderJoinArena(eval);
            }
        }

        private static ActionEvaluation<BattleArena> PrepareBattleArena(
            ActionEvaluation<BattleArena> eval)
        {
            UpdatePreviousAvatarState(eval.PreviousState, eval.Action.myAvatarAddress);
            UpdateCurrentAvatarItemSlotState(eval, BattleType.Arena);
            UpdateCurrentAvatarRuneSlotState(eval, BattleType.Arena);
            return eval;
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

            // NOTE: Start cache some arena info which will be used after battle ends.
            await UniTask.WhenAll(RxProps.ArenaInfoTuple.UpdateAsync(eval.OutputState),
                RxProps.ArenaInformationOrderedWithScore.UpdateAsync(eval.OutputState));

            void OnBattleEnd()
            {
                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarStateAsync(eval).Forget();
                // TODO!!!! [`PlayersArenaParticipant`]를 개별로 업데이트 한다.
                // RxProps.PlayersArenaParticipant.UpdateAsync().Forget();
                _disposableForBattleEnd = null;
                Game.Game.instance.Arena.IsAvatarStateUpdatedAfterBattle = true;
            }

            _disposableForBattleEnd?.Dispose();
            _disposableForBattleEnd = Game.Game.instance.Arena.OnArenaEnd
                .First()
                .Subscribe(_ =>
                {
                    UniTask.RunOnThreadPool(OnBattleEnd)
                        .ToObservable()
                        .First()
                        .DoOnError(e => NcDebug.LogException(e));
                });

            var tableSheets = TableSheets.Instance;
            ArenaPlayerDigest? myDigest = null;
            ArenaPlayerDigest? enemyDigest = null;
            CollectionState myCollectionState = null;
            CollectionState enemyCollectionState = null;

            var championshipId = eval.Action.championshipId;
            var round = eval.Action.round;

            var myArenaScoreAdr = ArenaScore.DeriveAddress(
                eval.Action.myAvatarAddress,
                championshipId,
                round);

            var previousMyScore = ArenaScore.ArenaScoreDefault;
            var outMyScore = ArenaScore.ArenaScoreDefault;

            var prepareObserve = UniTask.RunOnThreadPool(() =>
            {
                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd = Game.Game.instance.Arena.OnArenaEnd
                    .First()
                    .Subscribe(_ =>
                    {
                        UniTask.RunOnThreadPool(OnBattleEnd, false)
                            .ToObservable()
                            .First()
                            .DoOnError(e => NcDebug.LogException(e));
                    });
                previousMyScore = StateGetter.TryGetArenaScore(
                    eval.PreviousState,
                    myArenaScoreAdr,
                    out var myArenaScore)
                    ? myArenaScore.Score
                    : ArenaScore.ArenaScoreDefault;
                outMyScore = StateGetter.TryGetState(
                    eval.OutputState,
                    ReservedAddresses.LegacyAccount,
                    myArenaScoreAdr,
                    out var outputMyScoreList)
                    ? (Integer)((List)outputMyScoreList)[1]
                    : ArenaScore.ArenaScoreDefault;
                (myDigest, enemyDigest) = GetArenaPlayerDigest(
                    eval.PreviousState,
                    eval.OutputState,
                    eval.Action.myAvatarAddress,
                    eval.Action.enemyAvatarAddress);
                myCollectionState = StateGetter.GetCollectionState(eval.OutputState, eval.Action.myAvatarAddress);
                enemyCollectionState = StateGetter.GetCollectionState(eval.OutputState, eval.Action.enemyAvatarAddress);
            }).ToObservable().ObserveOnMainThread();

            prepareObserve.Subscribe(_ =>
            {
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
                for (var i = 0; i < eval.Action.ticket; i++)
                {
                    var simulator = new ArenaSimulator(random,
                        BattleArena.HpIncreasingModifier,
                        States.Instance.GameConfigState.ShatterStrikeMaxDamage);
                    var log = simulator.Simulate(
                        myDigest.Value,
                        enemyDigest.Value,
                        arenaSheets,
                        myCollectionState.GetEffects(tableSheets.CollectionSheet),
                        enemyCollectionState.GetEffects(tableSheets.CollectionSheet),
                        tableSheets.DeBuffLimitSheet,
                        tableSheets.BuffLinkSheet,
                        true);

                    var reward = RewardSelector.Select(
                        random,
                        tableSheets.WeeklyArenaRewardSheet,
                        tableSheets.MaterialItemSheet,
                        myDigest.Value.Level,
                        ArenaHelper.GetRewardCount(previousMyScore));

                    if (log.Result.Equals(ArenaLog.ArenaResult.Win))
                    {
                        if (hasMedalReward && medalItem is not null)
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

                if (!arenaBattlePreparation || !arenaBattlePreparation.IsActive())
                {
                    return;
                }

                arenaBattlePreparation.OnRenderBattleArena(eval);
                Game.Game.instance.Arena.Enter(
                    logs.First(),
                    rewards,
                    myDigest.Value,
                    enemyDigest.Value,
                    eval.Action.myAvatarAddress,
                    eval.Action.enemyAvatarAddress,
                    winCount + defeatCount > 1 ? (winCount, defeatCount) : null);
            });
        }

        private (ArenaPlayerDigest myDigest, ArenaPlayerDigest enemyDigest) GetArenaPlayerDigest(
            HashDigest<SHA256> prevStates,
            HashDigest<SHA256> outputStates,
            Address myAvatarAddress,
            Address enemyAvatarAddress)
        {
            var myAvatarState = States.Instance.CurrentAvatarState;
            var enemyAvatarState =
                Game.Game.instance.Agent.GetAvatarStatesAsync(prevStates, new[] { enemyAvatarAddress }).Result[enemyAvatarAddress];
            var myItemSlotStateAddress = ItemSlotState.DeriveAddress(myAvatarAddress, BattleType.Arena);
            var myItemSlotState = StateGetter.TryGetState(
                outputStates,
                ReservedAddresses.LegacyAccount,
                myItemSlotStateAddress, out var rawItemSlotState)
                ? new ItemSlotState((List)rawItemSlotState)
                : new ItemSlotState(BattleType.Arena);

            var myAllRuneState = States.Instance.AllRuneState;
            var myRuneSlotState = States.Instance.CurrentRuneSlotStates[BattleType.Arena];

            var myDigest = new ArenaPlayerDigest(myAvatarState,
                myItemSlotState.Equipments,
                myItemSlotState.Costumes,
                myAllRuneState,
                myRuneSlotState);

            var enemyItemSlotStateAddress = ItemSlotState.DeriveAddress(enemyAvatarAddress, BattleType.Arena);
            var enemyItemSlotState =
                StateGetter.GetState(
                    prevStates,
                    ReservedAddresses.LegacyAccount,
                    enemyItemSlotStateAddress) is List enemyRawItemSlotState
                    ? new ItemSlotState(enemyRawItemSlotState)
                    : new ItemSlotState(BattleType.Arena);

            var enemyAllRuneState = GetStateExtensions.GetAllRuneState(prevStates, enemyAvatarAddress);

            var enemyRuneSlotStateAddress = RuneSlotState.DeriveAddress(enemyAvatarAddress, BattleType.Arena);
            var enemyRuneSlotState =
                StateGetter.GetState(
                    prevStates,
                    ReservedAddresses.LegacyAccount,
                    enemyRuneSlotStateAddress) is List enemyRawRuneSlotState
                    ? new RuneSlotState(enemyRawRuneSlotState)
                    : new RuneSlotState(BattleType.Arena);

            var enemyDigest = new ArenaPlayerDigest(enemyAvatarState,
                enemyItemSlotState.Equipments,
                enemyItemSlotState.Costumes,
                enemyAllRuneState,
                enemyRuneSlotState);

            return (myDigest, enemyDigest);
        }

        private ActionEvaluation<Raid> PrepareRaid(ActionEvaluation<Raid> eval)
        {
            if (eval.Exception is not null)
            {
                return eval;
            }

            if (eval.Action.PayNcg)
            {
                UpdateAgentStateAsync(eval).Forget();
            }

            UpdateCrystalBalance(eval);
            UpdatePreviousAvatarState(eval.PreviousState, eval.Action.AvatarAddress);
            UpdateCurrentAvatarItemSlotState(eval, BattleType.Raid);
            UpdateCurrentAvatarRuneSlotState(eval, BattleType.Raid);
            UpdateCurrentAvatarRuneStoneBalance(eval);

            _disposableForBattleEnd?.Dispose();
            _disposableForBattleEnd =
                Game.Game.instance.RaidStage.OnBattleEnded
                    .First()
                    .Subscribe(stage =>
                    {
                        var task = UniTask.RunOnThreadPool(() =>
                        {
                            UpdateCurrentAvatarStateAsync(eval).Forget();
                            var avatarState = States.Instance.CurrentAvatarState;
                            RenderQuest(eval.Action.AvatarAddress,
                                avatarState.questList.completedQuestIds);
                            _disposableForBattleEnd = null;
                            stage.IsAvatarStateUpdatedAfterBattle = true;
                        }, false);
                        task.ToObservable()
                            .First()
                            // ReSharper disable once ConvertClosureToMethodGroup
                            .DoOnError(e => NcDebug.LogException(e));
                    });
            return eval;
        }

        private async void ResponseRaidAsync(ActionEvaluation<Raid> eval)
        {
            if (eval.Exception is not null)
            {
                Game.Game.BackToMainAsync(eval.Exception.InnerException, false).Forget();
                return;
            }

            var worldBoss = Widget.Find<WorldBoss>();
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            if (Widget.Find<RaidPreparation>().IsSkipRender)
            {
                Widget.Find<LoadingScreen>().Close();
                worldBoss.Close();
                await WorldBossStates.Set(eval.OutputState, eval.BlockIndex, avatarAddress);

                Game.Event.OnRoomEnter.Invoke(true);
                return;
            }

            if (!WorldBossFrontHelper.TryGetCurrentRow(eval.BlockIndex, out var row))
            {
                NcDebug.LogError(
                    $"[Raid] Failed to get current world boss row. BlockIndex : {eval.BlockIndex}");
                return;
            }

            var clonedAvatarState = (AvatarState)States.Instance.CurrentAvatarState.Clone();
            var random = new LocalRandom(eval.RandomSeed);
            var preRaiderState = WorldBossStates.GetRaiderState(avatarAddress);
            var preKillReward = WorldBossStates.GetKillReward(avatarAddress);
            var latestBossLevel = preRaiderState?.LatestBossLevel ?? 0;
            var allRuneState = States.Instance.AllRuneState;
            var runeSlotStates = States.Instance.CurrentRuneSlotStates[BattleType.Raid];
            var itemSlotStates = States.Instance.CurrentItemSlotStates[BattleType.Raid];

            var simulator = new RaidSimulator(
                row.BossId,
                random,
                clonedAvatarState,
                eval.Action.FoodIds,
                allRuneState,
                runeSlotStates,
                TableSheets.Instance.GetRaidSimulatorSheets(),
                TableSheets.Instance.CostumeStatSheet,
                States.Instance.CollectionState.GetEffects(TableSheets.Instance.CollectionSheet),
                TableSheets.Instance.DeBuffLimitSheet,
                TableSheets.Instance.BuffLinkSheet
            );
            simulator.Simulate();
            Widget.Find<Menu>().Close();

            var playerDigest = new ArenaPlayerDigest(
                clonedAvatarState,
                itemSlotStates.Equipments,
                itemSlotStates.Costumes,
                allRuneState,
                runeSlotStates);

            await WorldBossStates.Set(eval.OutputState, eval.BlockIndex, avatarAddress);
            var raiderState = WorldBossStates.GetRaiderState(avatarAddress);
            var killRewards = new WorldBossRewards();
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
                        var rewards = WorldBossHelper.CalculateReward(
                            rank,
                            row.BossId,
                            Game.Game.instance.TableSheets.RuneWeightSheet,
                            Game.Game.instance.TableSheets.WorldBossKillRewardSheet,
                            Game.Game.instance.TableSheets.RuneSheet,
                            Game.Game.instance.TableSheets.MaterialItemSheet,
                            random
                        );

                        killRewards.Assets.AddRange(rewards.assets);
                    }
                }
            }

            var isNewRecord = raiderState is null ||
                raiderState.HighScore < simulator.DamageDealt;
            worldBoss.Close(true);

            Widget.Find<LoadingScreen>().Close();
            var raidStartData = new RaidStage.RaidStartData(
                eval.Action.AvatarAddress,
                simulator.BossId,
                simulator.Log,
                playerDigest,
                simulator.DamageDealt,
                isNewRecord,
                false,
                new WorldBossRewards(simulator.AssetReward, simulator.Reward),
                killRewards);

            Game.Game.instance.RaidStage.Play(raidStartData);
        }

        private static ActionEvaluation<ClaimRaidReward> PrepareClaimRaidReward(
            ActionEvaluation<ClaimRaidReward> eval)
        {
            UpdateCurrentAvatarRuneStoneBalance(eval);
            UpdateCrystalBalance(eval);
            UpdateCurrentAvatarInventory(eval);

            return eval;
        }

        private static void ResponseClaimRaidReward(
            ActionEvaluation<ClaimRaidReward> eval)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            WorldBossStates.SetReceivingGradeRewards(avatarAddress, false);
            Widget.Find<WorldBossRewardScreen>().Show(new LocalRandom(eval.RandomSeed));
        }

        private (ActionEvaluation<RuneEnhancement>, FungibleAssetValue runeStone, AllRuneState previousState)
            PrepareRuneEnhancement(ActionEvaluation<RuneEnhancement> eval)
        {
            var action = eval.Action;
            var runeRow = TableSheets.Instance.RuneSheet[action.RuneId];

            var previousState = States.Instance.AllRuneState;
            States.Instance.SetAllRuneState(
                GetStateExtensions.GetAllRuneState(eval.OutputState, action.AvatarAddress));

            UpdateCrystalBalance(eval);
            UpdateAgentStateAsync(eval).Forget();
            var runeStone = StateGetter.GetBalance(
                eval.OutputState,
                action.AvatarAddress,
                Currencies.GetRune(runeRow.Ticker));
            States.Instance.SetCurrentAvatarBalance(runeStone);
            return (eval, runeStone, previousState);
        }

        private void ResponseRuneEnhancement((ActionEvaluation<RuneEnhancement> eval, FungibleAssetValue runeStone, AllRuneState previousState) prepared)
        {
            Widget.Find<Rune>().OnActionRender(
                new LocalRandom(prepared.eval.RandomSeed),
                prepared.runeStone,
                Util.GetCpChanged(prepared.previousState, States.Instance.AllRuneState));
        }

        private ActionEvaluation<UnlockRuneSlot> PreResponseUnlockRuneSlot(ActionEvaluation<UnlockRuneSlot> eval)
        {
            for (var i = 1; i < (int)BattleType.End; i++)
            {
                States.Instance.CurrentRuneSlotStates[(BattleType)i].Unlock(eval.Action.SlotIndex);
            }

            LoadingHelper.UnlockRuneSlot.Remove(eval.Action.SlotIndex);
            return eval;
        }

        private ActionEvaluation<UnlockRuneSlot> PrepareUnlockRuneSlot(ActionEvaluation<UnlockRuneSlot> eval)
        {
            UpdateAgentStateAsync(eval).Forget();
            return eval;
        }

        private void ResponseUnlockRuneSlot(ActionEvaluation<UnlockRuneSlot> eval)
        {
            NotificationSystem.Push(
                MailType.Workshop,
                L10nManager.Localize("UI_MESSAGE_RUNE_SLOT_OPEN"),
                NotificationCell.NotificationType.Notification);
        }

        private void ResponseUnlockCombinationSlot(ActionEvaluation<UnlockCombinationSlot> eval)
        {
            UniTask.RunOnThreadPool(async () =>
            {
                var avatarAddress = States.Instance.CurrentAvatarState.address;
                var slotIndex = eval.Action.SlotIndex;
                var slot = GetStateExtensions.GetCombinationSlotState(eval.OutputState, avatarAddress, slotIndex);
                UpdateCombinationSlotState(avatarAddress, slotIndex, slot);
                await UpdateAgentStateAsync(eval);
                await UpdateCurrentAvatarStateAsync(eval);
            }).ToObservable().ObserveOnMainThread().Subscribe(_ =>
            {
                var combinationSlotsPopup = Widget.Find<CombinationSlotsPopup>();
                combinationSlotsPopup.UpdateSlots();
                combinationSlotsPopup.SetLockLoading(eval.Action.SlotIndex, false);
            });
        }

        private void ExceptionUnlockCombinationSlot(ActionEvaluation<UnlockCombinationSlot> eval)
        {
            var combinationSlotsPopup = Widget.Find<CombinationSlotsPopup>();
            combinationSlotsPopup.SetLockLoading(eval.Action.SlotIndex, false);
        }

        private ActionEvaluation<PetEnhancement> PreparePetEnhancement(ActionEvaluation<PetEnhancement> eval)
        {
            UpdateAgentStateAsync(eval).Forget();
            var soulStoneTicker = TableSheets.Instance.PetSheet[eval.Action.PetId].SoulStoneTicker;
            States.Instance.CurrentAvatarBalances[soulStoneTicker] = StateGetter.GetBalance(
                eval.OutputState,
                eval.Action.AvatarAddress,
                Currencies.GetMinterlessCurrency(soulStoneTicker));
            UpdatePetState(eval.Action.AvatarAddress, eval.Action.PetId, eval.OutputState);
            return eval;
        }

        private void ResponsePetEnhancement(ActionEvaluation<PetEnhancement> eval)
        {
            LoadingHelper.PetEnhancement.Value = 0;
            var action = eval.Action;
            var petId = action.PetId;
            var targetLevel = action.TargetLevel;

            if (targetLevel > 1)
            {
                Widget.Find<PetLevelUpResultScreen>().Show(petId, targetLevel - 1, targetLevel);
            }
            else
            {
                Widget.Find<PetSummonResultScreen>().Show(petId);
            }

            Widget.Find<DccCollection>().UpdateView();
            Game.Game.instance.SavedPetId = action.PetId;
        }

        private (ActionEvaluation<ActivateCollection> eval, CollectionState previousState) PrepareActivateCollection(ActionEvaluation<ActivateCollection> eval)
        {
            var previousState = States.Instance.CollectionState;
            States.Instance.SetCollectionState(
                StateGetter.GetCollectionState(eval.OutputState, eval.Action.AvatarAddress));

            return (eval, previousState);
        }

        private void ResponseActivateCollection((ActionEvaluation<ActivateCollection> eval, CollectionState previousState) prepared)
        {
            Widget.Find<Collection>().OnActionRender();

            var (eval, previousState) = prepared;
            var collectionSheet = TableSheets.Instance.CollectionSheet;
            var collectionId = eval.Action.CollectionData.First().collectionId;
            var collectionRow = collectionSheet[collectionId];

            var completionRate = (States.Instance.CollectionState.Ids.Count, collectionSheet.Count);
            var cp = Util.GetCpChanged(previousState, States.Instance.CollectionState);
            Widget.Find<CollectionResultScreen>().Show(collectionRow, completionRate, cp);


            UniTask.RunOnThreadPool(() => UpdateCurrentAvatarStateAsync(eval).Forget());
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
                .Select(eval =>
                {
                    UpdateCurrentAvatarStateAsync(eval).Forget();
                    return eval;
                })
                .Subscribe(eval =>
                {
                    RxProps.SelectAvatarAsync(
                        States.Instance.CurrentAvatarKey,
                        eval.OutputState,
                        true).Forget();
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
            int petId,
            HashDigest<SHA256> hash)
        {
            var rawPetState = StateGetter.GetState(
                hash,
                ReservedAddresses.LegacyAccount,
                PetState.DeriveAddress(avatarAddress, petId));
            States.Instance.PetStates.UpdatePetState(
                petId,
                new PetState((List)rawPetState));
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
            if (StateGetter.GetState(
                    eval.OutputState,
                    ReservedAddresses.LegacyAccount,
                    pledgeAddress) is List l)
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
            if (StateGetter.GetState(
                    eval.OutputState,
                    ReservedAddresses.LegacyAccount,
                    pledgeAddress) is List l)
            {
                address = l[0].ToAddress();
                approved = l[1].ToBoolean();
                mead = l[2].ToInteger();
            }

            States.Instance.SetPledgeStates(address, approved);
        }

        private ActionEvaluation<UnloadFromMyGarages> PrepareUnloadFromMyGarages(
            ActionEvaluation<UnloadFromMyGarages> eval)
        {
            var gameStates = Game.Game.instance.States;
            var agentAddr = gameStates.AgentState.address;
            var avatarAddr = gameStates.CurrentAvatarState.address;
            var states = eval.OutputState;
            var action = eval.Action;
            if (action.FungibleAssetValues is not null)
            {
                foreach (var (balanceAddr, value) in action.FungibleAssetValues)
                {
                    if (balanceAddr.Equals(agentAddr))
                    {
                        var balance = StateGetter.GetBalance(
                            states,
                            balanceAddr,
                            value.Currency);
                        if (value.Currency.Equals(GoldCurrency))
                        {
                            var goldState = new GoldBalanceState(balanceAddr, balance);
                            gameStates.SetGoldBalanceState(goldState);
                        }
                        else if (value.Currency.Equals(Currencies.Crystal))
                        {
                            gameStates.SetCrystalBalance(balance);
                        }
                        else if (value.Currency.Equals(Currencies.Garage))
                        {
                            AgentStateSubject.OnNextGarage(value);
                        }
                    }
                    else if (balanceAddr.Equals(avatarAddr))
                    {
                        var balance = StateGetter.GetBalance(
                            states,
                            balanceAddr,
                            value.Currency);
                        gameStates.SetCurrentAvatarBalance(balance);
                    }
                }
            }

            UpdateCurrentAvatarStateAsync(StateGetter.GetAvatarState(states, avatarAddr)).Forget();
            return eval;
        }

        // Caution: This assumes that the avatar is migrated to V2.
        private void ResponseUnloadFromMyGarages(ActionEvaluation<UnloadFromMyGarages> eval)
        {
            if (eval.Exception is not null)
            {
                NcDebug.Log(eval.Exception.Message);
                return;
            }

            var gameStates = Game.Game.instance.States;
            var avatarAddr = gameStates.CurrentAvatarState.address;
            var states = eval.OutputState;
            UnloadFromMyGaragesRecipientMail mail = null;
            UniTask.RunOnThreadPool(() =>
            {
                // Note : AvatarState를 업데이트하지 않고도 MailBox만 사용하기 위함
                var mailBox = StateGetter.GetMailBox(states, avatarAddr);
                var sameBlockIndexMailList = mailBox
                    .OfType<UnloadFromMyGaragesRecipientMail>()
                    .Where(m => m.blockIndex == eval.BlockIndex)
                    .ToList();
                if (sameBlockIndexMailList.Any())
                {
                    var memoCheckedMail = sameBlockIndexMailList.FirstOrDefault(m => m.Memo == eval.Action.Memo);
                    mail = memoCheckedMail ?? sameBlockIndexMailList.First();
                }

                if (mail is not null)
                {
                    if (eval.Action.RecipientAvatarAddr.Equals(States.Instance.CurrentAvatarState.address) &&
                        eval.Action.FungibleIdAndCounts is not null &&
                        !(mail.Memo != null && mail.Memo.Contains("season_pass")))
                    {
                        UpdateCurrentAvatarInventory(eval);
                    }
                }
            }).ToObservable().ObserveOnMainThread().Subscribe(_ =>
            {
                if (Widget.TryFind<MobileShop>(out var mobileShop) && mobileShop.IsActive())
                {
                    Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                }

                if (mail is not null)
                {
                    UniTask.RunOnThreadPool(() =>
                    {
                        var avatarState = StateGetter.GetAvatarState(states, avatarAddr);
                        LocalLayerModifier.AddNewMail(avatarState, mail.id);
                    }).Forget();
                    if (mail.Memo != null && mail.Memo.Contains("season_pass"))
                    {
                        OneLineSystem.Push(MailType.System,
                            L10nManager.Localize(
                                "NOTIFICATION_SEASONPASS_REWARD_CLAIMED_MAIL_RECEIVED"),
                            NotificationCell.NotificationType.Notification);
                        return;
                    }

                    if (mail.Memo != null && mail.Memo.Contains("iap"))
                    {
                        var product = MailExtensions.GetProductFromMemo(mail.Memo);
                        if (product != null)
                        {
                            var productName = L10nManager.Localize(product.L10n_Key);
                            var format = L10nManager.Localize(
                                "NOTIFICATION_IAP_PURCHASE_DELIVERY_COMPLETE");
                            OneLineSystem.Push(MailType.System,
                                string.Format(format, productName),
                                NotificationCell.NotificationType.Notification);
                            return;
                        }
                    }
                }

                NcDebug.LogWarning($"Not found UnloadFromMyGaragesRecipientMail from " +
                    $"the render context of UnloadFromMyGarages action.\n" +
                    $"tx id: {eval.TxId}, action id: {eval.Action.Id}");
            });
        }

        private ActionEvaluation<ClaimItems> PrepareClaimItems(
            ActionEvaluation<ClaimItems> eval)
        {
            var gameStates = Game.Game.instance.States;
            var agentAddr = gameStates.AgentState.address;
            var avatarAddr = gameStates.CurrentAvatarState.address;
            var states = eval.OutputState;
            var action = eval.Action;
            if (action.ClaimData is not null)
            {
                foreach (var (addr, favList) in action.ClaimData)
                {
                    if (addr.Equals(avatarAddr))
                    {
                        foreach (var fav in favList)
                        {
                            var tokenCurrency = fav.Currency;
                            if (Currencies.IsWrappedCurrency(tokenCurrency))
                            {
                                var currency = Currencies.GetUnwrappedCurrency(tokenCurrency);
                                var recipientAddress = Currencies.PickAddress(currency, agentAddr,
                                    avatarAddr);
                                var isCrystal = currency.Equals(Currencies.Crystal);
                                var balance = StateGetter.GetBalance(
                                    states,
                                    recipientAddress,
                                    currency);
                                if (isCrystal)
                                {
                                    gameStates.SetCrystalBalance(balance);
                                }
                                else
                                {
                                    gameStates.SetCurrentAvatarBalance(balance);
                                }
                            }
                        }
                    }
                }
            }

            UpdateCurrentAvatarStateAsync(StateGetter.GetAvatarState(states, avatarAddr)).Forget();
            return eval;
        }

        private void ResponseClaimItems(ActionEvaluation<ClaimItems> eval)
        {
            if (eval.Exception is not null)
            {
                NcDebug.Log(eval.Exception.Message);
                return;
            }

            var gameStates = Game.Game.instance.States;
            var avatarAddr = gameStates.CurrentAvatarState.address;
            var states = eval.OutputState;
            MailBox mailBox = null;
            UniTask.RunOnThreadPool(() =>
            {
                mailBox = StateGetter.GetMailBox(states, avatarAddr);

                UpdateCurrentAvatarInventory(eval);
            }).ToObservable().ObserveOnMainThread().Subscribe(_ =>
            {
                if (Widget.TryFind<MobileShop>(out var mobileShop) && mobileShop.IsActive())
                {
                    Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
                }

                ClaimItemsMail mail = null;
                var sameBlockIndexMailList = mailBox
                    .OfType<ClaimItemsMail>()
                    .Where(m => m.blockIndex == eval.BlockIndex)
                    .ToList();
                if (sameBlockIndexMailList.Any())
                {
                    var memoCheckedMail = sameBlockIndexMailList.FirstOrDefault(m => m.Memo == eval.Action.Memo);
                    mail = memoCheckedMail ?? sameBlockIndexMailList.First();
                }

                if (mail is not null)
                {
                    UniTask.RunOnThreadPool(() =>
                    {
                        var avatarState = StateGetter.GetAvatarState(states, avatarAddr);
                        LocalLayerModifier.AddNewMail(avatarState, mail.id);
                    }).Forget();
                    if (mail.Memo != null && mail.Memo.Contains("season_pass"))
                    {
                        OneLineSystem.Push(MailType.System,
                            L10nManager.Localize(
                                "NOTIFICATION_SEASONPASS_REWARD_CLAIMED_MAIL_RECEIVED"),
                            NotificationCell.NotificationType.Notification);
                        return;
                    }

                    if (mail.Memo != null && mail.Memo.Contains("iap"))
                    {
                        var product = MailExtensions.GetProductFromMemo(mail.Memo);
                        if (product != null)
                        {
                            var productName = L10nManager.Localize(product.L10n_Key);
                            var format = L10nManager.Localize(
                                "NOTIFICATION_IAP_PURCHASE_DELIVERY_COMPLETE");
                            OneLineSystem.Push(MailType.System,
                                string.Format(format, productName),
                                NotificationCell.NotificationType.Notification);
                            return;
                        }
                    }
                }

                NcDebug.LogWarning($"Not found ClaimItemsRecipientMail from " +
                    $"the render context of ClaimItems action.\n" +
                    $"tx id: {eval.TxId}, action id: {eval.Action.Id}");
            });
        }

        private ActionEvaluation<MintAssets> PrepareMintAssets(
            ActionEvaluation<MintAssets> eval)
        {
            var gameStates = Game.Game.instance.States;
            var agentAddr = gameStates.AgentState.address;
            var avatarAddr = gameStates.CurrentAvatarState.address;
            var states = eval.OutputState;
            var action = eval.Action;
            if (action.MintSpecs is { } specs)
            {
                var requiredUpdateAvatarState = false;
                foreach (var spec in specs.Where(spec => spec.Recipient.Equals(avatarAddr) || spec.Recipient.Equals(agentAddr)))
                {
                    if (spec.Assets.HasValue)
                    {
                        var fav = spec.Assets.Value;
                        {
                            var tokenCurrency = fav.Currency;
                            var recipientAddress = Currencies.PickAddress(tokenCurrency, agentAddr,
                                avatarAddr);
                            var isCrystal = tokenCurrency.Equals(Currencies.Crystal);
                            var balance = StateGetter.GetBalance(
                                states,
                                recipientAddress,
                                tokenCurrency);
                            if (isCrystal)
                            {
                                gameStates.SetCrystalBalance(balance);
                            }
                            else
                            {
                                gameStates.SetCurrentAvatarBalance(balance);
                            }
                        }
                    }

                    requiredUpdateAvatarState |= spec.Items.HasValue;
                }

                if (requiredUpdateAvatarState)
                {
                    UpdateCurrentAvatarStateAsync(StateGetter.GetAvatarState(states, avatarAddr)).Forget();
                }
            }

            return eval;
        }

        private void ResponseMintAssets(ActionEvaluation<MintAssets> eval)
        {
            if (eval.Exception is not null)
            {
                NcDebug.Log(eval.Exception.Message);
                return;
            }

            var gameStates = Game.Game.instance.States;
            var avatar = gameStates.CurrentAvatarState;
            var mailBox = avatar.mailBox;
            UnloadFromMyGaragesRecipientMail mail = null;
            var sameBlockIndexMailList = mailBox
                .OfType<UnloadFromMyGaragesRecipientMail>()
                .Where(m => m.blockIndex == eval.BlockIndex)
                .ToList();
            if (sameBlockIndexMailList.Any())
            {
                var memoCheckedMail = sameBlockIndexMailList.FirstOrDefault(m => m.Memo == eval.Action.Memo);
                mail = memoCheckedMail ?? sameBlockIndexMailList.First();
            }

            if (mail is not null)
            {
                UniTask.RunOnThreadPool(() =>
                {
                    var avatarAddr = gameStates.CurrentAvatarState.address;
                    var states = eval.OutputState;
                    var avatarState = StateGetter.GetAvatarState(states, avatarAddr);
                    LocalLayerModifier.AddNewMail(avatarState, mail.id);
                }).Forget();
            }

            if (Widget.TryFind<MobileShop>(out var mobileShop) && mobileShop.IsActive())
            {
                Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Shop);
            }
        }

        private void Wanted()
        {
            _actionRenderer.EveryRender<Wanted>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationIsSuccess)
                .ObserveOnMainThread()
                .Subscribe(ResponseWanted)
                .AddTo(_disposables);

            _actionRenderer.EveryRender<Wanted>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsTerminated)
                .ObserveOnMainThread()
                .Subscribe(ExceptionWanted)
                .AddTo(_disposables);
        }

        private void ExceptionWanted(ActionEvaluation<Wanted> eval)
        {
            if (eval.Exception != null)
            {
                NcDebug.LogError(eval.Exception);
                Widget.Find<WorldMap>().SetAdventureBossButtonLoading(false);
                if (eval.Exception.InnerException is PreviousBountyException)
                {
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_ADVENTURE_BOSS_PREVIUS_BOUNTY_EXCEPTION"), NotificationCell.NotificationType.Alert);
                }
                else
                {
                    OneLineSystem.Push(MailType.System, eval.Exception.InnerException.Message, NotificationCell.NotificationType.Alert);
                }
            }
        }

        private void ResponseWanted(ActionEvaluation<Wanted> eval)
        {
            //최초 입찰여부를 알기위한 변수
            var isFirstWanted = false;
            var bountyBoard = Game.Game.instance.AdventureBossData.BountyBoard;
            UniTask.RunOnThreadPool(async () =>
            {
                isFirstWanted = bountyBoard.Value == null;
                if (eval.Action.AvatarAddress.Equals(States.Instance.CurrentAvatarState.address))
                {
                    await UpdateAgentStateAsync(eval);
                }
            }).ToObservable().ObserveOnMainThread().Subscribe(async _ =>
            {
                await Game.Game.instance.AdventureBossData.RefreshAllByCurrentState(eval.OutputState, eval.BlockIndex);

                var action = eval.Action;

                if (eval.Action.AvatarAddress.Equals(States.Instance.CurrentAvatarState.address))
                {
                    Widget.Find<WorldMap>().SetAdventureBossButtonLoading(false);
                }

                if (bountyBoard.Value == null)
                {
                    return;
                }

                //최초입찰여부
                if (isFirstWanted && bountyBoard.Value.Investors.Count >= 1 && bountyBoard.Value.Investors[0].Count >= 1)
                {
                    Widget.Find<AdventureBossOpenInfoPopup>().Show();
                }
            });
        }

        private void UnlockFloor()
        {
            _actionRenderer.EveryRender<UnlockFloor>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(eval => eval.Action.AvatarAddress.Equals(States.Instance.CurrentAvatarState.address))
                .Where(ValidateEvaluationIsSuccess)
                .Subscribe(ResponseUnlockFloor)
                .AddTo(_disposables);
        }

        private void ResponseUnlockFloor(ActionEvaluation<UnlockFloor> eval)
        {
            UniTask.RunOnThreadPool(async () =>
            {
                await UpdateAgentStateAsync(eval);
                await UpdateCurrentAvatarStateAsync(eval);
            }).ToObservable().ObserveOnMainThread().Subscribe(_ => { Widget.Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.AdventureBoss); });
        }

        private void ClaimAdventureBossReward()
        {
            _actionRenderer.EveryRender<ClaimAdventureBossReward>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationIsSuccess)
                .ObserveOnMainThread()
                .Subscribe(ResponseClaimAdventureBossReward)
                .AddTo(_disposables);
        }

        private void ResponseClaimAdventureBossReward(ActionEvaluation<ClaimAdventureBossReward> eval)
        {
            var seasonInfo = Game.Game.instance.AdventureBossData.SeasonInfo.Value;
            var lastSeason = seasonInfo.Season;
            UniTask.RunOnThreadPool(async () =>
            {
                if (eval.Action.AvatarAddress.Equals(States.Instance.CurrentAvatarState.address))
                {
                    await UpdateAgentStateAsync(eval);
                    await UpdateCurrentAvatarStateAsync(eval);
                    UpdateCurrentAvatarRuneStoneBalance(eval);
                    UpdateCurrentAvatarInventory(eval);
                }
            }).ToObservable().ObserveOnMainThread().Subscribe(async _ =>
            {
                if (Game.Game.instance.AdventureBossData.EndedExploreBoards.TryGetValue(lastSeason, out var exploreBoard))
                {
                    if (exploreBoard.RaffleWinner == null)
                    {
                        //이름을 가져오기 위해 바로 갱신함.
                        await Game.Game.instance.AdventureBossData.RefreshEndedSeasons(eval.OutputState, eval.BlockIndex);
                        //최초
                        Widget.Find<AdventureBossNcgRandomRewardPopup>().Show(lastSeason);
                    }
                    else
                    {
                        //기존정보 업데이트 보상수령 정보를 갱신하기위함.
                        Game.Game.instance.AdventureBossData.RefreshEndedSeasons(eval.OutputState, eval.BlockIndex).Forget();
                    }
                }
            });
        }

        private void StageExceptionHandle(Exception innerException)
        {
            var showLoadingScreen = false;
            if (Widget.Find<StageLoadingEffect>().IsActive())
            {
                Widget.Find<StageLoadingEffect>().Close();
            }

            if (Widget.Find<AdventureBossResultPopup>().IsActive())
            {
                showLoadingScreen = true;
                Widget.Find<AdventureBossResultPopup>().Close();
            }

            Game.Game.BackToMainAsync(innerException, showLoadingScreen)
                .Forget();
        }

        private void ExploreAdventureBoss()
        {
            _actionRenderer.EveryRender<ExploreAdventureBoss>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(eval => eval.Action.AvatarAddress.Equals(States.Instance.CurrentAvatarState.address))
                .Where(ValidateEvaluationIsSuccess)
                .Subscribe(ResponseExploreAdventureBoss)
                .AddTo(_disposables);

            _actionRenderer.EveryRender<ExploreAdventureBoss>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsTerminated)
                .ObserveOnMainThread()
                .Subscribe(ExceptionExploreAdventureBoss)
                .AddTo(_disposables);
        }

        private void ResponseExploreAdventureBoss(ActionEvaluation<ExploreAdventureBoss> eval)
        {
            var firstFloor = 1;
            var maxFloor = 5;
            var lastFloor = firstFloor;
            var prevTotalScore = 0;
            UniTask.RunOnThreadPool(() =>
            {
                if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
                {
                    return eval;
                }

                var exploreInfo = Game.Game.instance.AdventureBossData.ExploreInfo.Value;
                firstFloor = exploreInfo == null ? 1 : exploreInfo.Floor + 1;
                maxFloor = exploreInfo == null ? 5 : exploreInfo.MaxFloor;
                lastFloor = firstFloor;
                prevTotalScore = exploreInfo == null ? 0 : exploreInfo.Score;

                UpdatePreviousAvatarState(eval.PreviousState, eval.Action.AvatarAddress);
                
                UpdateCurrentAvatarItemSlotState(eval, BattleType.Adventure);
                UpdateCurrentAvatarRuneSlotState(eval, BattleType.Adventure);
                UpdateCurrentAvatarRuneStoneBalance(eval);
                UpdateCurrentAvatarInventory(eval);

                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd =
                    Game.Game.instance.Stage.OnEnterToStageEnd
                        .First()
                        .Subscribe(_ =>
                        {
                            var task = UniTask.RunOnThreadPool(() =>
                            {
                                UpdateCurrentAvatarStateAsync(eval).Forget();
                                _disposableForBattleEnd = null;
                                Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                            }, false);
                            task.ToObservable()
                                .First()
                                // ReSharper disable once ConvertClosureToMethodGroup
                                .DoOnError(e => NcDebug.LogException(e));
                        });
                return eval;
            }).ToObservable().ObserveOnMainThread().Subscribe(_ =>
            {
                if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
                {
                    NcDebug.LogError("Not last battle action id.");
                    StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                    return;
                }

                var seasonInfo = Game.Game.instance.AdventureBossData.SeasonInfo.Value;
                if (seasonInfo == null || seasonInfo.Season != eval.Action.Season)
                {
                    NcDebug.LogError("SeasonInfo is null or season is not matched.");
                    StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                    return;
                }

                var exploreInfo = Game.Game.instance.AdventureBossData.ExploreInfo.Value;
                var random = new LocalRandom(eval.RandomSeed);
                var tableSheets = TableSheets.Instance;
                var selector = new WeightedSelector<AdventureBossFloorSheet.RewardData>(random);
                AdventureBossSimulator simulator = null;
                var score = 0;
                var rewardList = new List<AdventureBossSheet.RewardAmountData>();
                var firstRewardList = new List<AdventureBossSheet.RewardAmountData>();

                var bossRow = tableSheets.AdventureBossSheet.Values.FirstOrDefault(row => row.BossId == seasonInfo.BossId);
                if (bossRow == null)
                {
                    NcDebug.LogError($"BossSheet is not found. BossId: {seasonInfo.BossId}");
                    StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                    return;
                }

                var floorRows = tableSheets.AdventureBossFloorSheet.Values.Where(row => row.AdventureBossId == bossRow.Id).ToList();

                var floorIdList = new List<int>();
                for (var fl = firstFloor; fl <= maxFloor; fl++)
                {
                    var floorRow = floorRows.FirstOrDefault(row => row.Floor == fl);
                    if (floorRow is null)
                    {
                        NcDebug.LogError($"FloorSheet is not found. Floor: {fl}");
                        StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                        return;
                    }

                    if (!tableSheets.AdventureBossFloorWaveSheet.TryGetValue(floorRow.Id, out var waveRows))
                    {
                        NcDebug.LogError($"FloorWaveSheet is not found. Floor: {fl}");
                        StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                        return;
                    }

                    var rewards = AdventureBossSimulator.GetWaveRewards(random, floorRow, tableSheets.MaterialItemSheet);

                    simulator = new AdventureBossSimulator(
                        seasonInfo.BossId,
                        floorRow.Id,
                        random,
                        States.Instance.CurrentAvatarState,
                        fl == firstFloor ? eval.Action.Foods : new List<Guid>(),
                        States.Instance.AllRuneState,
                        States.Instance.CurrentRuneSlotStates[BattleType.Adventure],
                        floorRow,
                        waveRows,
                        tableSheets.GetStageSimulatorSheets(),
                        tableSheets.EnemySkillSheet,
                        tableSheets.CostumeStatSheet,
                        rewards,
                        States.Instance.CollectionState.GetEffects(tableSheets.CollectionSheet),
                        tableSheets.DeBuffLimitSheet,
                        tableSheets.BuffLinkSheet,
                        true,
                        States.Instance.GameConfigState.ShatterStrikeMaxDamage);

                    simulator.Simulate();
                    lastFloor = fl;
                    floorIdList.Add(floorRow.Id);

                    // Get Reward if cleared
                    if (simulator.Log.IsClear)
                    {
                        // Add point, reward
                        if (!tableSheets.AdventureBossFloorPointSheet.TryGetValue(fl, out var pointRow))
                        {
                            NcDebug.LogError($"FloorPointSheet is not found. Floor: {fl}");
                            StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                            return;
                        }

                        var point = random.Next(pointRow.MinPoint, pointRow.MaxPoint + 1);

                        score += point;

                        var stageId = floorRows.First(row => row.Floor == fl).Id;
                        if (!tableSheets.AdventureBossFloorFirstRewardSheet.TryGetValue(stageId, out var firstReward))
                        {
                            NcDebug.LogError($"FloorFirstRewardSheet is not found. StageId: {stageId}");
                            StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                            return;
                        }

                        foreach (var reward in firstReward.Rewards)
                        {
                            firstRewardList.Add(new AdventureBossSheet.RewardAmountData(
                                reward.ItemType, reward.ItemId, reward.Amount));
                        }

                        selector.Clear();

                        var floorReward = floorRows.First(row => row.Floor == fl);
                        foreach (var reward in floorReward.Rewards)
                        {
                            selector.Add(reward, reward.Ratio);
                        }

                        var selected = selector.Select(1).First();
                        rewardList.Add(new AdventureBossSheet.RewardAmountData(
                            selected.ItemType,
                            selected.ItemId,
                            random.Next(selected.Min, selected.Max + 1))
                        );
                    }
                    else
                    {
                        break;
                    }
                }

                floorIdList.Remove(floorIdList.Last());
                if (simulator is not null && lastFloor > firstFloor)
                {
                    simulator.AddBreakthrough(floorIdList, tableSheets.AdventureBossFloorWaveSheet);
                }

                var log = simulator.Log;
                var stage = Game.Game.instance.Stage;
                stage.StageType = StageType.AdventureBoss;
                log.score = score;

                if (!Game.Game.instance.AdventureBossData.GetCurrentBossData(out var bossData))
                {
                    NcDebug.LogError("BossData is null");
                    StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                    return;
                }

                var totalApPotionUsed = (maxFloor - firstFloor + 1) * bossData.ExploreAp;
                var apPotionUsed = (lastFloor - firstFloor + 1) * bossData.ExploreAp;
                var lastClearFloor = log.IsClear ? lastFloor : lastFloor - 1;

                Widget.Find<AdventureBossResultPopup>().SetData(apPotionUsed, totalApPotionUsed, lastClearFloor, prevTotalScore, rewardList, firstRewardList);
                Widget.Find<UI.Battle>().FloorProgressBar.SetData(firstFloor, maxFloor, lastFloor);

                BattleRenderer.Instance.PrepareStage(log);
            });
        }

        private void ExceptionExploreAdventureBoss(ActionEvaluation<ExploreAdventureBoss> eval)
        {
            StageExceptionHandle(eval.Exception?.InnerException);
        }

        private void SweepAdventureBoss()
        {
            _actionRenderer.EveryRender<SweepAdventureBoss>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(eval => eval.Action.AvatarAddress.Equals(States.Instance.CurrentAvatarState.address))
                .Where(ValidateEvaluationIsSuccess)
                .Subscribe(ResponseSweepAdventureBoss)
                .AddTo(_disposables);

            _actionRenderer.EveryRender<SweepAdventureBoss>()
                .ObserveOn(Scheduler.ThreadPool)
                .Where(ValidateEvaluationForCurrentAgent)
                .Where(ValidateEvaluationIsTerminated)
                .ObserveOnMainThread()
                .Subscribe(ExceptionSweepAdventureBoss)
                .AddTo(_disposables);
        }

        private void ResponseSweepAdventureBoss(ActionEvaluation<SweepAdventureBoss> eval)
        {
            var firstFloor = 1;
            var maxFloor = 5;
            var lastFloor = firstFloor;
            var prevTotalScore = 0;
            UniTask.RunOnThreadPool(() =>
            {
                if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
                {
                    return eval;
                }

                var exploreInfo = Game.Game.instance.AdventureBossData.ExploreInfo.Value;
                firstFloor = exploreInfo == null ? 1 : exploreInfo.Floor + 1;
                maxFloor = exploreInfo == null ? 5 : exploreInfo.MaxFloor;
                lastFloor = firstFloor;
                prevTotalScore = exploreInfo == null ? 0 : exploreInfo.Score;

                UpdateAgentStateAsync(eval).Forget();
                UpdateCurrentAvatarItemSlotState(eval, BattleType.Adventure);
                UpdateCurrentAvatarRuneSlotState(eval, BattleType.Adventure);
                UpdateCurrentAvatarRuneStoneBalance(eval);
                UpdateCurrentAvatarInventory(eval);

                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd =
                    Game.Game.instance.Stage.OnEnterToStageEnd
                        .First()
                        .Subscribe(_ =>
                        {
                            var task = UniTask.RunOnThreadPool(() =>
                            {
                                UpdateCurrentAvatarStateAsync(eval).Forget();
                                _disposableForBattleEnd = null;
                                Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                            }, false);
                            task.ToObservable()
                                .First()
                                // ReSharper disable once ConvertClosureToMethodGroup
                                .DoOnError(e => NcDebug.LogException(e));
                        });
                return eval;
            }).ToObservable().ObserveOnMainThread().Subscribe(_ =>
            {
                if (!ActionManager.IsLastBattleActionId(eval.Action.Id))
                {
                    NcDebug.LogError("Not last battle action id.");
                    StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                    return;
                }

                var seasonInfo = Game.Game.instance.AdventureBossData.SeasonInfo.Value;
                if (seasonInfo == null || seasonInfo.Season != eval.Action.Season)
                {
                    NcDebug.LogError("SeasonInfo is null or season is not matched.");
                    StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                    return;
                }

                var exploreInfo = Game.Game.instance.AdventureBossData.ExploreInfo.Value;
                var random = new LocalRandom(eval.RandomSeed);
                var tableSheets = TableSheets.Instance;

                var bossRow = tableSheets.AdventureBossSheet.Values.FirstOrDefault(row => row.BossId == seasonInfo.BossId);
                if (bossRow == null)
                {
                    NcDebug.LogError($"BossSheet is not found. BossId: {seasonInfo.BossId}");
                    StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                    return;
                }

                var floorRow = tableSheets.AdventureBossFloorSheet.Values.FirstOrDefault(
                    row => row.AdventureBossId == bossRow.Id && row.Floor == exploreInfo.Floor
                );
                if (floorRow is null)
                {
                    NcDebug.LogError($"FloorSheet is not found. BossId: {seasonInfo.BossId}, Floor: {exploreInfo.Floor}");
                    StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                    return;
                }

                var breakThroughFloorRows = tableSheets.AdventureBossFloorSheet.Values.Where(
                    row => row.AdventureBossId == bossRow.Id && row.Floor <= exploreInfo.Floor).Select(floorRow => floorRow.Id).ToList();

                var simulator = new AdventureBossSimulator(seasonInfo.BossId, floorRow.Id, random, States.Instance.CurrentAvatarState, tableSheets.GetSimulatorSheets(), false);
                simulator.AddBreakthrough(breakThroughFloorRows, tableSheets.AdventureBossFloorWaveSheet);

                // Add point, reward
                var point = 0;
                var rewardList = new List<AdventureBossSheet.RewardAmountData>();
                var selector = new WeightedSelector<AdventureBossFloorSheet.RewardData>(random);
                var floorRows = tableSheets.AdventureBossFloorSheet.Values.Where(row => row.AdventureBossId == bossRow.Id);

                for (var fl = 1; fl <= exploreInfo.Floor; fl++)
                {
                    if (!tableSheets.AdventureBossFloorPointSheet.TryGetValue(fl, out var pointRow))
                    {
                        NcDebug.LogError($"FloorPointSheet is not found. Floor: {fl}");
                        StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                        return;
                    }

                    point += random.Next(pointRow.MinPoint, pointRow.MaxPoint + 1);

                    selector.Clear();

                    var floorReward = floorRows.First(row => row.Floor == fl);
                    foreach (var reward in floorReward.Rewards)
                    {
                        selector.Add(reward, reward.Ratio);
                    }

                    var selected = selector.Select(1).First();
                    rewardList.Add(new AdventureBossSheet.RewardAmountData(
                        selected.ItemType,
                        selected.ItemId,
                        random.Next(selected.Min, selected.Max + 1)
                    ));
                }

                var log = simulator.Log;
                var stage = Game.Game.instance.Stage;
                stage.StageType = StageType.AdventureBoss;

                if (!Game.Game.instance.AdventureBossData.GetCurrentBossData(out var bossData))
                {
                    NcDebug.LogError("BossData is null");
                    StageExceptionHandle(new Exception(L10nManager.Localize("ADVENTURE_BOSS_BATTLE_EXCEPTION")));
                    return;
                }

                log.score = point;
                var totalApPotionUsed = exploreInfo.Floor * bossData.SweepAp;

                Widget.Find<AdventureBossResultPopup>().SetData(totalApPotionUsed, totalApPotionUsed, exploreInfo.Floor, prevTotalScore, rewardList);
                Widget.Find<UI.Battle>().FloorProgressBar.SetData(1, exploreInfo.Floor, exploreInfo.Floor);

                BattleRenderer.Instance.PrepareStage(log);
            });
        }

        private void ExceptionSweepAdventureBoss(ActionEvaluation<SweepAdventureBoss> evaluation)
        {
            StageExceptionHandle(evaluation.Exception?.InnerException);
        }

        private void ExceptionCustomEquipmentCraft(
            ActionEvaluation<CustomEquipmentCraft> eval)
        {
            LoadingHelper.CustomEquipmentCraft.Value = false;
            if (States.Instance.CurrentAvatarState.address == eval.Action.AvatarAddress && eval.Action.CraftList.Count > 0)
            {
                Widget.Find<CombinationSlotsPopup>()
                    .SetLockLoading(eval.Action.CraftList.First().SlotIndex, false);
            }

            NcDebug.LogException(eval.Exception?.InnerException ?? eval.Exception);
        }

        private (ActionEvaluation<CustomEquipmentCraft>, CombinationSlotState) PrepareCustomEquipmentCraft(
            ActionEvaluation<CustomEquipmentCraft> eval)
        {
            var avatarAddress = eval.Action.AvatarAddress;
            var slotIndex = eval.Action.CraftList.FirstOrDefault().SlotIndex;
            ReactiveAvatarState.UpdateRelationship(
                (Integer)StateGetter.GetState(eval.OutputState, Addresses.Relationship, avatarAddress)
            );
            var slot = GetStateExtensions.GetCombinationSlotState(eval.OutputState, avatarAddress, slotIndex);
            UpdateCombinationSlotState(avatarAddress, slotIndex, slot);
            UpdateAgentStateAsync(eval).Forget();
            UpdateCurrentAvatarStateAsync(eval).Forget();

            return (eval, slot);
        }

        private void ResponseCustomEquipmentCraft((ActionEvaluation<CustomEquipmentCraft>, CombinationSlotState) prepared)
        {
            var (evaluation, slot) = prepared;
            var agentAddress = evaluation.Signer;
            var avatarAddress = evaluation.Action.AvatarAddress;
            var result = (CombinationConsumable5.ResultModel)slot.Result;

            Widget.Find<CustomCraft>().RequiredUpdateCraftCount = true;

            UniTask.RunOnThreadPool(() =>
            {
                LocalLayerModifier.ModifyAgentGold(evaluation, agentAddress,
                    result.gold);
            });

            LocalLayerModifier.AddNewMail(avatarAddress, result.id);

            // Notify
            var message = L10nManager.Localize(
                "NOTIFICATION_COMBINATION_COMPLETE",
                result.itemUsable.GetLocalizedName());
            NotificationSystem.Reserve(
                MailType.CustomCraft,
                message,
                slot.UnlockBlockIndex,
                result.itemUsable.ItemId);

            var slotIndex = evaluation.Action.CraftList.FirstOrDefault().SlotIndex;
            var blockCount = slot.UnlockBlockIndex - Game.Game.instance.Agent.BlockIndex;
            if (blockCount >= WorkshopNotifiedBlockCount)
            {
                var expectedNotifiedTime =
                    BlockIndexExtensions.BlockToTimeSpan(Mathf.RoundToInt(blockCount));
                var notificationText = L10nManager.Localize(
                    "PUSH_WORKSHOP_CRAFT_COMPLETE_CONTENT",
                    result.itemUsable.GetLocalizedNonColoredName(false));
                var identifier = PushNotifier.Push(
                    notificationText,
                    expectedNotifiedTime,
                    PushNotifier.PushType.Workshop);

                var pushIdentifierKey = string.Format(WorkshopPushIdentifierKeyFormat, slotIndex);
                PlayerPrefs.SetString(pushIdentifierKey, identifier);
            }

            Widget.Find<HeaderMenuStatic>().UpdatePortalRewardOnce(HeaderMenuStatic.PortalRewardNotificationCombineKey);
            // ~Notify

            Widget.Find<CombinationSlotsPopup>().OnCraftActionRender(slotIndex);
            Widget.Find<CustomCraftResultPopup>().Show((Equipment)result.itemUsable);
            LoadingHelper.CustomEquipmentCraft.Value = false;
        }

        /// <summary>
        /// 정확한 전투 재현을 위해 관련 상태를 모두 PreviousState로 가져와서 갱신합니다.
        /// 여기엔 AvatarState, AllRuneState, CollectionState가 해당됩니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prevState"></param>
        /// <param name="avatarAddr"></param>
        private static AvatarState UpdatePreviousAvatarState(HashDigest<SHA256> prevState,
            Address avatarAddr)
        {
            var prevAvatarState = StateGetter.GetAvatarState(prevState, avatarAddr);
            States.Instance.UpdateCurrentAvatarState(prevAvatarState);
            States.Instance.SetAllRuneState(
                GetStateExtensions.GetAllRuneState(prevState, avatarAddr));
            States.Instance.SetCollectionState(
                StateGetter.GetCollectionState(prevState, avatarAddr));
            return prevAvatarState;
        }
    }
}
