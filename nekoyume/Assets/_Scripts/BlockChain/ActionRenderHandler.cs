using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.Action;
using Nekoyume.Game.Mail;
using Nekoyume.Manager;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI;
using UniRx;
using UnityEngine;
using Combination = Nekoyume.Action.Combination;

namespace Nekoyume.BlockChain
{
    /// <summary>
    /// 현상태 : 각 액션의 랜더 단계에서 즉시 게임 정보에 반영시킴. 아바타를 선택하지 않은 상태에서 이전에 성공시키지 못한 액션을 재수행하고
    ///       이를 핸들링하면, 즉시 게임 정보에 반영시길 수 없기 때문에 에러가 발생함.
    /// 참고 : 이후 언랜더 처리를 고려한 해법이 필요함.
    /// 해법 1: 랜더 단계에서 얻는 `eval` 자체 혹은 변경점을 queue에 넣고, 게임의 상태에 따라 꺼내 쓰도록.
    ///
    /// ToDo. `ActionRenderHandler`의 형태가 완성되면, `ActionUnrenderHandler`도 작성해야 함.
    /// </summary>
    public class ActionRenderHandler
    {
        private static class Singleton
        {
            internal static readonly ActionRenderHandler Value = new ActionRenderHandler();
        }

        public static readonly ActionRenderHandler Instance = Singleton.Value;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private ActionRenderHandler()
        {
        }

        public void Start()
        {
            LoadLocalAvatarState();
            Shop();
            Ranking();
            RewardGold();
            CreateAvatar();
            DeleteAvatar();
            HackAndSlash();
            Combination();
            Sell();
            SellCancellation();
            Buy();
            RankingReward();
            AddItem();
            AddGold();
            DailyReward();
            ItemEnhancement();
            QuestReward();
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private bool ValidateEvaluationForAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            if (States.Instance.AgentState.Value == null)
            {
                return false;
            }

            return evaluation.OutputStates.UpdatedAddresses.Contains(States.Instance.AgentState.Value.address);
        }

        private bool ValidateEvaluationForCurrentAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase =>
            !(States.Instance.CurrentAvatarState.Value is null)
            && evaluation.OutputStates.UpdatedAddresses.Contains(States.Instance.CurrentAvatarState.Value.address);

        private AgentState GetAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.AgentState.Value.address;
            return evaluation.OutputStates.GetAgentState(agentAddress);
        }

        private void UpdateAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            Debug.LogFormat("Called UpdateAgentState<{0}>. Updated Addresses : `{1}`", evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
            var state = GetAgentState(evaluation);
            UpdateAgentState(state);
        }

        private void UpdateAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation, int index) where T : ActionBase
        {
            Debug.LogFormat("Called UpdateAvatarState<{0}>. Updated Addresses : `{1}`", evaluation.Action,
                string.Join(",", evaluation.OutputStates.UpdatedAddresses));
            if (!States.Instance.AgentState.Value.avatarAddresses.ContainsKey(index))
            {
                States.Instance.AvatarStates.Remove(index);
                AvatarManager.DeleteAvatarPrivateKey(index);
                return;
            }

            var avatarAddress = States.Instance.AgentState.Value.avatarAddresses[index];
            var avatarState = evaluation.OutputStates.GetAvatarState(avatarAddress);
            if (avatarState is null)
            {
                return;
            }

            UpdateAvatarState(avatarState, index);
        }

        private void UpdateCurrentAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var avatarState = evaluation.OutputStates.GetAvatarState(States.Instance.CurrentAvatarState.Value.address);
            var questList = avatarState.questList.Where(i => i.Complete && !i.Receive).ToList();
            if (questList.Count >= 1)
            {
                if (questList.Count == 1)
                {
                    var quest = questList.First();
                    var format = LocalizationManager.Localize("NOTIFICATION_QUEST_COMPLETE");
                    var msg = string.Format(format, quest.GetName());
                    UI.Notification.Push(MailType.System, msg);
                }
                else
                {
                    var format = LocalizationManager.Localize("NOTIFICATION_MULTIPLE_QUEST_COMPLETE");
                    var msg = string.Format(format, questList.Count);
                    UI.Notification.Push(MailType.System, msg);

                }
            }
            UpdateAvatarState(evaluation, States.Instance.CurrentAvatarKey.Value);
        }

        private void UpdateShopState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            States.Instance.ShopState.Value = new ShopState(
                (Bencodex.Types.Dictionary) evaluation.OutputStates.GetState(ShopState.Address)
            );
        }

        private void UpdateRankingState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            States.Instance.RankingState.Value = new RankingState(
                (Bencodex.Types.Dictionary) evaluation.OutputStates.GetState(RankingState.Address)
            );
        }

        private void Shop()
        {
            ActionBase.EveryRender(ShopState.Address)
                .ObserveOnMainThread()
                .Subscribe(UpdateShopState).AddTo(_disposables);
        }

        private void Ranking()
        {
            ActionBase.EveryRender(RankingState.Address)
                .ObserveOnMainThread()
                .Subscribe(UpdateRankingState).AddTo(_disposables);
        }

        private void RewardGold()
        {
            ActionBase.EveryRender<RewardGold>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(UpdateAgentState).AddTo(_disposables);
        }

        private void CreateAvatar()
        {
            ActionBase.EveryRender<CreateAvatar>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    UpdateAgentState(eval);
                    UpdateAvatarState(eval, eval.Action.index);
                }).AddTo(_disposables);
        }

        private void DeleteAvatar()
        {
            ActionBase.EveryRender<DeleteAvatar>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    UpdateAgentState(eval);
                    UpdateAvatarState(eval, eval.Action.index);
                }).AddTo(_disposables);
        }

        private void HackAndSlash()
        {
            ActionBase.EveryRender<HackAndSlash>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseHackAndSlash).AddTo(_disposables);
        }

        private void Combination()
        {
            ActionBase.EveryRender<Action.Combination>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombination).AddTo(_disposables);
        }

        private void Sell()
        {
            ActionBase.EveryRender<Sell>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseSell).AddTo(_disposables);
        }

        private void SellCancellation()
        {
            ActionBase.EveryRender<SellCancellation>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseSellCancellation).AddTo(_disposables);
        }

        private void Buy()
        {
            ActionBase.EveryRender<Buy>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(ResponseBuy).AddTo(_disposables);
        }

        private void RankingReward()
        {
            ActionBase.EveryRender<RankingReward>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(UpdateAgentState).AddTo(_disposables);
        }

        private void AddItem()
        {
            ActionBase.EveryRender<AddItem>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(UpdateCurrentAvatarState).AddTo(_disposables);
        }

        private void AddGold()
        {
            ActionBase.EveryRender<AddItem>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    UpdateAgentState(eval);
                    UpdateCurrentAvatarState(eval);
                }).AddTo(_disposables);
        }

        private void ItemEnhancement()
        {
            ActionBase.EveryRender<ItemEnhancement>()
                .Where(ValidateEvaluationForAgentState)
                .ObserveOnMainThread()
                .Subscribe(ResponseItemEnhancement).AddTo(_disposables);
        }

        private void DailyReward()
        {
            ActionBase.EveryRender<DailyReward>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(UpdateCurrentAvatarState).AddTo(_disposables);
        }

        private void QuestReward()
        {
            ActionBase.EveryRender<QuestReward>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(ResponseQuestReward).AddTo(_disposables);
        }

        private void ResponseCombination(ActionBase.ActionEvaluation<Combination> evaluation)
        {
            var itemUsable = evaluation.Action.Result.itemUsable;
            var isSuccess = !(itemUsable is null);
            if (isSuccess)
            {
                var format = LocalizationManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
                UI.Notification.Push(MailType.Workshop, string.Format(format, itemUsable.Data.GetLocalizedName()));
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombinationSuccess);
            }
            else
            {
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombinationFail);
                var format = LocalizationManager.Localize("NOTIFICATION_COMBINATION_FAIL");
                UI.Notification.Push(MailType.Workshop, format);
            }
            UpdateCurrentAvatarState(evaluation);
        }

        private static void UpdateAvatarState(AvatarState avatarState, int index)
        {
            if (States.Instance.AvatarStates.ContainsKey(index))
            {
                States.Instance.AvatarStates[index] = avatarState;
            }
            else
            {
                States.Instance.AvatarStates.Add(index, avatarState);
            }
        }

        private static void UpdateAgentState(AgentState state)
        {
            States.Instance.AgentState.Value = state;
        }

        public static void UpdateLocalAvatarState(AvatarState avatarState, int index)
        {
            Debug.LogFormat("Update local avatarState. agentAddress: {0} address: {1} BlockIndex: {2}",
                avatarState.agentAddress, avatarState.address, avatarState.BlockIndex);
            UpdateAvatarState(avatarState, index);
        }

        public static void UpdateLocalAgentState(AgentState agentState)
        {
            Debug.LogFormat("Update local agentSTate. agentAddress: {0} BlockIndex: {1}",
                agentState.address, Game.Game.instance.agent.BlockIndex);
            UpdateAgentState(agentState);
        }

        private void LoadLocalAvatarState()
        {
            if (!(States.Instance.AgentState?.Value is null))
            {
                foreach (var avatarAddress in States.Instance.AgentState.Value.avatarAddresses)
                {
                    var fileName = string.Format(States.CurrentAvatarFileNameFormat,
                        States.Instance.AgentState.Value.address,
                        avatarAddress.Value);
                    var path = Path.Combine(Application.persistentDataPath, fileName);
                    if (File.Exists(path))
                    {
                        var avatarState =
                            ByteSerializer.Deserialize<AvatarState>(File.ReadAllBytes(path));
                        Debug.LogFormat("Load local avatarState. agentAddress: {0} address: {1} BlockIndex: {2}",
                            avatarState.agentAddress, avatarState.address, avatarState.BlockIndex);
                        UpdateLocalAvatarState(avatarState, avatarAddress.Key);
                        File.Delete(path);
                    }
                }
            }
        }

        private void ResponseSell(ActionBase.ActionEvaluation<Sell> eval)
        {
            var format = LocalizationManager.Localize("NOTIFICATION_SELL_COMPLETE");
            UI.Notification.Push(MailType.Auction, string.Format(format, eval.Action.itemUsable.Data.GetLocalizedName()));
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseSellCancellation(ActionBase.ActionEvaluation<SellCancellation> eval)
        {
            var format = LocalizationManager.Localize("NOTIFICATION_SELL_CANCEL_COMPLETE");
            UI.Notification.Push(MailType.Auction, string.Format(format, eval.Action.result.itemUsable.Data.GetLocalizedName()));
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseBuy(ActionBase.ActionEvaluation<Buy> eval)
        {
            if (eval.Action.buyerAvatarAddress == States.Instance.CurrentAvatarState.Value.address)
            {
                var format = LocalizationManager.Localize("NOTIFICATION_BUY_BUYER_COMPLETE");
                UI.Notification.Push(MailType.Auction, string.Format(format, eval.Action.buyerResult.itemUsable.Data.GetLocalizedName()));
            }
            else
            {
                var format = LocalizationManager.Localize("NOTIFICATION_BUY_SELLER_COMPLETE");
                var result = eval.Action.sellerResult;
                UI.Notification.Push(MailType.Auction, string.Format(format, result.itemUsable.Data.GetLocalizedName(), result.gold));
            }

            UpdateCurrentAvatarState(eval);
        }
        
        private void ResponseHackAndSlash(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            UpdateCurrentAvatarState(eval);
            var actionFailPopup = Widget.Find<ActionFailPopup>();
            actionFailPopup.CloseCallback = null;
            actionFailPopup.Close();
            if (Widget.Find<QuestPreparation>().IsActive())
            {
                Widget.Find<QuestPreparation>().GoToStage(eval);
            }
            if (Widget.Find<BattleResult>().IsActive())
            {
                Widget.Find<BattleResult>().NextStage(eval);
            }
        }

        private void ResponseQuestReward(ActionBase.ActionEvaluation<QuestReward> eval)
        {
            UpdateCurrentAvatarState(eval);
            var format = LocalizationManager.Localize("NOTIFICATION_QUEST_REWARD");
            var msg = string.Format(format, eval.Action.Result.GetName());
            UI.Notification.Push(MailType.System, msg);
        }

        private void ResponseItemEnhancement(ActionBase.ActionEvaluation<ItemEnhancement> eval)
        {
            var format = LocalizationManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE");
            UI.Notification.Push(MailType.Workshop,
                string.Format(format, eval.Action.result.itemUsable.Data.GetLocalizedName()));
            UpdateAgentState(eval);
            UpdateCurrentAvatarState(eval);
        }
    }
}
