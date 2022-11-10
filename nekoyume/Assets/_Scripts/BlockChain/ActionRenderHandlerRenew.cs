using System;
using System.Collections.Generic;
using Lib9c.Renderer;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.State;
using UnityEngine;
using mixpanel;

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions.Action;
#endif

namespace Nekoyume.BlockChain
{
    using UniRx;

    public partial class ActionRenderHandlerRenew : ActionHandler
    {
        private static class Singleton
        {
            internal static readonly ActionRenderHandlerRenew Value = new();
        }

        public static ActionRenderHandlerRenew Instance => Singleton.Value;

        private readonly List<IDisposable> _disposables = new();

        private IDisposable _disposableForBattleEnd;

        private ActionRenderer _actionRenderer;

        private readonly Dictionary<Type, string> _actionTypeValueDict = new();

        private ActionRenderHandlerRenew()
        {
        }

        public override void Start(ActionRenderer renderer)
        {
            _actionRenderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _actionTypeValueDict.Clear();

            Stop();
            _actionRenderer.BlockEndSubject
                .ObserveOnMainThread()
                .Subscribe(_ =>
                    Debug.Log($"[{nameof(BlockRenderHandler)}] Render actions end"))
                .AddTo(_disposables);
            _actionRenderer.ActionRenderSubject
                .ObserveOnMainThread()
                .Subscribe(eval =>
                {
                    OnActionRender(eval);

                    if (eval.Action is not GameAction gameAction)
                    {
                        return;
                    }

                    if (!ActionManager.Instance.TryPopActionEnqueuedDateTime(
                            gameAction.Id,
                            out var enqueuedDateTime))
                    {
                        return;
                    }

                    var actionType = gameAction.GetActionTypeAttribute();
                    var elapsed = (DateTime.Now - enqueuedDateTime).TotalSeconds;

                    if (States.Instance.CurrentAvatarState is not null)
                    {
                        Analyzer.Instance.Track("Unity/ActionRender",
                            new Dictionary<string, Value>()
                            {
                                ["ActionType"] = actionType.TypeIdentifier,
                                ["Elapsed"] = elapsed,
                                ["AvatarAddress"] =
                                    States.Instance.CurrentAvatarState.address.ToString(),
                                ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                            });
                    }
                }).AddTo(_disposables);
        }

        public void Stop()
        {
            _disposables.DisposeAllAndClear();
        }

        private void OnActionRender(ActionBase.ActionEvaluation<ActionBase> eval)
        {
            var actionType = eval.Action.GetType();
            string actionTypeValue;
            if (_actionTypeValueDict.ContainsKey(actionType))
            {
                actionTypeValue = _actionTypeValueDict[actionType];
            }
            else
            {
                actionTypeValue = ActionTypeAttribute.ValueOf(actionType);
                if (actionTypeValue is null)
                {
                    Debug.LogWarning(
                        $"[{nameof(ActionRenderHandlerRenew)}] {nameof(OnActionRender)}():" +
                        $" {actionType.FullName} is not registered in" +
                        $" {nameof(ActionTypeAttribute)}.");
                    return;
                }

                _actionTypeValueDict[actionType] = actionTypeValue;
            }

            Debug.Log($"[{nameof(ActionRenderHandlerRenew)}] {nameof(OnActionRender)}():" +
                      $" \"{actionTypeValue}\"");

            try
            {
                if (CheckType(actionTypeValue, "hack_and_slash"))
                {
                    OnRenderHackAndSlash(eval);
                }
                else if (CheckType(actionTypeValue, "daily_reward"))
                {
                    OnRenderDailyReward(eval);
                }
                else if (CheckType(actionTypeValue, "claim_stake_reward"))
                {
                    OnRenderClaimStakeReward(eval);
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                Debug.LogWarning(
                    $"{actionType.FullName}(\"{actionTypeValue}\") is not supported: " +
                    $"\n{e.Message}\n{e.StackTrace}");
            }
        }

        private bool CheckType(string actionTypeValue, string prefix)
        {
            if (!actionTypeValue.StartsWith(prefix))
            {
                return false;
            }

            var versionStr = actionTypeValue.Replace(prefix, string.Empty);
            return int.TryParse(versionStr, out _);
        }
    }
}
