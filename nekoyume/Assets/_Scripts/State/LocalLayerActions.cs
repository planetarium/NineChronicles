using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using UnityEngine;

namespace Nekoyume.State
{
    // NOTE: Consider how to directly use the Tx list staged in the Tx pool.
    /// <summary>
    /// Register `GameAction` information for adjust `States`.
    /// </summary>
    public class LocalLayerActions
    {
        public static LocalLayerActions Instance => Game.Game.instance.LocalLayerActions;

        private class Info
        {
            public Action<IAgent, States, TableSheets> payCostAction;
            public long createdBlockIndex;
            public bool isRendered;
        }

        private readonly Stack<Info> _reusableInfoPool = new Stack<Info>();

        private readonly Dictionary<Guid, Info> _infos = new Dictionary<Guid, Info>();

        /// <summary>
        /// Register `GameAction` information.
        /// </summary>
        /// <param name="gameActionId">`GameAction.Id`</param>
        /// <param name="payCostAction">Pay the cost if `GameAction` has.</param>
        /// <param name="createdBlockIndex">Block index when `GameAction` created in ActionManager.</param>
        /// <param name="isRendered">Set `true` when this `GameAction` already rendered by `IActionRenderer`</param>
        public void Register(
            Guid gameActionId,
            Action<IAgent, States, TableSheets> payCostAction,
            long createdBlockIndex,
            bool isRendered = false)
        {
            if (TryGetRegisteredInfo(gameActionId, out var info))
            {
                NcDebug.LogError($"[{nameof(LocalLayerActions)}] Already registered. {gameActionId.ToString()}");

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
        public void SetRendered(Guid gameActionId, bool isRendered)
        {
            if (!TryGetRegisteredInfo(gameActionId, out var info))
            {
                NcDebug.LogError($"[{nameof(LocalLayerActions)}] There is no registered. {gameActionId.ToString()}");

                return;
            }

            info.isRendered = isRendered;
        }

        /// <summary>
        /// Pay the cost of registered `GameAction`s which is not rendered.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="states">Source `States`</param>
        /// <param name="tableSheets"></param>
        public void PayCost(IAgent agent, States states, TableSheets tableSheets)
        {
            foreach (var info in _infos.Values.Where(e => !e.isRendered))
            {
                info.payCostAction(agent, states, tableSheets);
            }
        }

        /// <summary>
        /// Unregister the registered `GameAction` information which is created before `blockIndex`.
        /// </summary>
        /// <param name="blockIndex"></param>
        public void UnregisterCreatedBefore(long blockIndex)
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

        public void UnregisterAll()
        {
            var infos = _infos.Values.ToArray();
            _infos.Clear();
            foreach (var info in infos)
            {
                _reusableInfoPool.Push(info);
            }
        }

        private bool TryGetRegisteredInfo(Guid gameActionId, out Info info)
        {
            if (_infos.ContainsKey(gameActionId))
            {
                info = _infos[gameActionId];
                return true;
            }

            info = null;
            return false;
        }

        private Info GetNewInfo(Action<IAgent, States, TableSheets> payCostAction, long createdBlockIndex,
            bool isRendered = false)
        {
            var info = _reusableInfoPool.Count == 0
                ? new Info()
                : _reusableInfoPool.Pop();

            info.payCostAction = payCostAction;
            info.createdBlockIndex = createdBlockIndex;
            info.isRendered = isRendered;
            return info;
        }
    }
}
