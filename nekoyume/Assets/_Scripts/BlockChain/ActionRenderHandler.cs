using System;
using System.Collections.Generic;
using System.IO;
using Nekoyume.Action;
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
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private bool ValidateEvaluationForAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            if (States.Instance.agentState.Value == null)
            {
                return false;
            }

            return evaluation.OutputStates.UpdatedAddresses.Contains(States.Instance.agentState.Value.address);
        }

        private bool ValidateEvaluationForCurrentAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation)
            where T : ActionBase =>
            !(States.Instance.currentAvatarState.Value is null)
            && evaluation.OutputStates.UpdatedAddresses.Contains(States.Instance.currentAvatarState.Value.address);

        private AgentState GetAgentState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            var agentAddress = States.Instance.agentState.Value.address;
            return (AgentState) evaluation.OutputStates.GetState(agentAddress);
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
            if (!States.Instance.agentState.Value.avatarAddresses.ContainsKey(index))
            {
                States.Instance.avatarStates.Remove(index);
                AvatarManager.DeleteAvatarPrivateKey(index);
                return;
            }

            var avatarAddress = States.Instance.agentState.Value.avatarAddresses[index];
            var avatarState = (AvatarState) evaluation.OutputStates.GetState(avatarAddress);
            if (avatarState == null)
            {
                return;
            }

            UpdateAvatarState(avatarState, index);
        }

        private void UpdateCurrentAvatarState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            UpdateAvatarState(evaluation, States.Instance.currentAvatarKey.Value);
        }

        private void UpdateShopState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            States.Instance.shopState.Value = (ShopState) evaluation.OutputStates.GetState(ShopState.Address);
        }

        private void UpdateRankingState<T>(ActionBase.ActionEvaluation<T> evaluation) where T : ActionBase
        {
            States.Instance.rankingState.Value = (RankingState) evaluation.OutputStates.GetState(RankingState.Address);
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

        private void DailyReward()
        {
            ActionBase.EveryRender<DailyReward>()
                .Where(ValidateEvaluationForCurrentAvatarState)
                .ObserveOnMainThread()
                .Subscribe(UpdateCurrentAvatarState).AddTo(_disposables);
        }

        private void ResponseCombination(ActionBase.ActionEvaluation<Combination> evaluation)
        {
            var isSuccess = !(evaluation.Action.result.itemUsable is null);
            AnalyticsManager.Instance.OnEvent(isSuccess
                ? AnalyticsManager.EventName.ActionCombinationSuccess
                : AnalyticsManager.EventName.ActionCombinationFail);
            UpdateCurrentAvatarState(evaluation);
        }

        private static void UpdateAvatarState(AvatarState avatarState, int index)
        {
            if (States.Instance.avatarStates.ContainsKey(index))
            {
                States.Instance.avatarStates[index] = avatarState;
            }
            else
            {
                States.Instance.avatarStates.Add(index, avatarState);
            }
        }

        private static void UpdateAgentState(AgentState state)
        {
            States.Instance.agentState.Value = state;
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
            if (!(States.Instance.agentState?.Value is null))
            {
                foreach (var avatarAddress in States.Instance.agentState.Value.avatarAddresses)
                {
                    var fileName = string.Format(States.CurrentAvatarFileNameFormat,
                        States.Instance.agentState.Value.address,
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
            UI.Notification.Push($"{eval.Action.itemUsable.Data.GetLocalizedName()} 상점 등록 완료.");
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseSellCancellation(ActionBase.ActionEvaluation<SellCancellation> eval)
        {
            UI.Notification.Push($"{eval.Action.result.itemUsable.Data.GetLocalizedName()} 판매 취소 완료.");
            UpdateCurrentAvatarState(eval);
        }

        private void ResponseBuy(ActionBase.ActionEvaluation<Buy> eval)
        {
            if (eval.Action.buyerAvatarAddress == States.Instance.currentAvatarState.Value.address)
            {
                UI.Notification.Push($"{eval.Action.buyerResult.itemUsable.Data.GetLocalizedName()} 구매 완료.");
            }
            else
            {
                var result = eval.Action.sellerResult;
                UI.Notification.Push(
                    $"{result.itemUsable.Data.GetLocalizedName()} 판매 완료.\n세금 8% 제외 {result.gold}gold 획득");
            }

            UpdateCurrentAvatarState(eval);
        }
        
        private void ResponseHackAndSlash(ActionBase.ActionEvaluation<HackAndSlash> eval)
        {
            UpdateCurrentAvatarState(eval);
            Widget.Find<ActionFailPopup>().Close();
            if (Widget.Find<QuestPreparation>().IsActive())
            {
                Widget.Find<QuestPreparation>().GoToStage(eval);
            }
            if (Widget.Find<BattleResult>().IsActive())
            {
                Widget.Find<BattleResult>().NextStage(eval);
            }
        }
    }
}
