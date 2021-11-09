using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using UnityEngine;

namespace Nekoyume.State
{
    /// <summary>
    /// Register `GameAction` information for adjust `States`.
    /// </summary>
    public static class LocalLayerActions
    {
        private class Info
        {
            public Action<States, TableSheets> payCostAction;
            public long createdBlockIndex;
            public bool isRendered;
        }

        private static readonly Stack<Info> _reusableInfoPool = new Stack<Info>();

        private static readonly Dictionary<Guid, Info> _infos = new Dictionary<Guid, Info>();

        /// <summary>
        /// Register `GameAction` information.
        /// </summary>
        /// <param name="gameActionId">`GameAction.Id`</param>
        /// <param name="payCostAction">Pay the cost if `GameAction` has.</param>
        /// <param name="createdBlockIndex">Block index when `GameAction` created in ActionManager.</param>
        /// <param name="isRendered">Set `true` when this `GameAction` already rendered by `IActionRenderer`</param>
        public static void Register(
            Guid gameActionId,
            Action<States, TableSheets> payCostAction,
            long createdBlockIndex,
            bool isRendered = false)
        {
            if (TryGetRegisteredInfo(gameActionId, out var info))
            {
                Debug.LogError($"[{nameof(LocalLayerActions)}] Already registered. {gameActionId.ToString()}");

                return;
            }

            info = GetNewInfo(payCostAction, createdBlockIndex, isRendered);
            _infos.Add(gameActionId, info);
        }

        /// <summary>
        /// Set rendered or unrendered flag. 
        /// </summary>
        /// <param name="gameActionId">`GameAction.Id`</param>
        /// <param name="isRendered">Set `True` when rendered. Set `False` when unrendered.</param>
        public static void SetRendered(Guid gameActionId, bool isRendered)
        {
            if (!TryGetRegisteredInfo(gameActionId, out var info))
            {
                Debug.LogError($"[{nameof(LocalLayerActions)}] There is no registered. {gameActionId.ToString()}");

                return;
            }

            info.isRendered = isRendered;
        }

        /// <summary>
        /// Pay the cost of registered `GameAction`s which is not rendered.
        /// </summary>
        /// <param name="states">Source `States`</param>
        /// <param name="tableSheets"></param>
        public static void PayCost(States states, TableSheets tableSheets)
        {
            foreach (var info in _infos.Values.Where(e => !e.isRendered))
            {
                info.payCostAction(states, tableSheets);
            }
        }

        /// <summary>
        /// Unregister the registered `GameAction` information which is created before `blockIndex`.
        /// </summary>
        /// <param name="blockIndex"></param>
        public static void UnregisterCreatedBefore(long blockIndex)
        {
            foreach (var gameActionId in _infos
                .Where(pair => pair.Value.createdBlockIndex < blockIndex)
                .Select(pair => pair.Key)
                .ToArray())
            {
                var info = _infos[gameActionId];
                _infos.Remove(gameActionId);
                _reusableInfoPool.Push(info);
            }
        }

        public static void UnregisterAll()
        {
            var infos = _infos.Values.ToArray();
            _infos.Clear();
            foreach (var info in infos)
            {
                _reusableInfoPool.Push(info);
            }
        }

        private static bool TryGetRegisteredInfo(Guid gameActionId, out Info info)
        {
            if (_infos.ContainsKey(gameActionId))
            {
                info = _infos[gameActionId];
                return true;
            }

            info = null;
            return false;
        }

        private static Info GetNewInfo(Action<States, TableSheets> payCostAction, long createdBlockIndex,
            bool isRendered = false)
        {
            // FIXME
            var info = _reusableInfoPool.Count == 0
                ? new Info
                {
                    payCostAction = payCostAction,
                    createdBlockIndex = createdBlockIndex,
                    isRendered = isRendered,
                }
                : _reusableInfoPool.Pop();

            info.payCostAction = payCostAction;
            info.createdBlockIndex = createdBlockIndex;
            info.isRendered = isRendered;
            return info;
        }
    }
}
