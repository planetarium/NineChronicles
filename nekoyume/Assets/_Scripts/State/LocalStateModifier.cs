using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.State.Modifiers;
using Nekoyume.State.Subjects;

namespace Nekoyume.State
{
    /// <summary>
    /// `LocalStateSettings`의 `Add`와 `Remove`함수를 사용하는 정적 클래스이다.
    /// `States.AgentState`와 `States.AvatarStates`, `States.CurrentAvatarState`를 업데이트 한다.
    /// `ReactiveAgentState`와 `ReactiveAvatarState`를 업데이트 한다.
    /// 반복되는 로직을 모아둔다.
    /// </summary>
    public static class LocalStateModifier
    {
        #region Agent, Avatar / Currency

        /// <summary>
        /// 에이전트의 골드를 변경한다.(휘발성)
        /// </summary>
        /// <param name="agentAddress"></param>
        /// <param name="gold"></param>
        public static void ModifyAgentGold(Address agentAddress, decimal gold)
        {
            if (gold is 0m)
                return;

            var modifier = new AgentGoldModifier(gold);
            LocalStateSettings.Instance.Add(agentAddress, modifier, true);

            var state = States.Instance.AgentState;
            if (state is null ||
                !state.address.Equals(agentAddress))
                return;

            modifier.Modify(state);
            ReactiveAgentState.Gold.SetValueAndForceNotify(state.gold);
        }

        /// <summary>
        /// 아바타의 행동력을 변경한다.(휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="actionPoint"></param>
        public static void ModifyAvatarActionPoint(Address avatarAddress, int actionPoint)
        {
            if (actionPoint is 0)
                return;

            var modifier = new AvatarActionPointModifier(actionPoint);
            LocalStateSettings.Instance.Add(avatarAddress, modifier, true);

            if (!TryGetLoadedAvatarState(avatarAddress, out var outAvatarState, out _, out var isCurrentAvatarState))
                return;

            modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
                return;

            ReactiveAvatarState.ActionPoint.SetValueAndForceNotify(outAvatarState.actionPoint);
        }

        #endregion

        #region Avatar / AddItem

        /// <summary>
        /// (휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="guid"></param>
        public static void AddItem(Address avatarAddress, Guid guid)
        {
            var modifier = new AvatarInventoryNonFungibleItemRemover(guid);
            LocalStateSettings.Instance.Remove(avatarAddress, modifier, true);
            AddItemInternal(avatarAddress);
        }

        /// <summary>
        /// (휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="id"></param>
        /// <param name="count"></param>
        public static void AddItem(Address avatarAddress, HashDigest<SHA256> id, int count)
        {
            if (count is 0)
                return;

            var modifier = new AvatarInventoryFungibleItemRemover(id, count);
            LocalStateSettings.Instance.Remove(avatarAddress, modifier, true);
            AddItemInternal(avatarAddress);
        }

        /// <summary>
        /// (휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="idAndCountDictionary"></param>
        public static void AddItem(Address avatarAddress, Dictionary<HashDigest<SHA256>, int> idAndCountDictionary)
        {
            var modifier = new AvatarInventoryFungibleItemRemover(idAndCountDictionary);
            LocalStateSettings.Instance.Remove(avatarAddress, modifier, true);
            AddItemInternal(avatarAddress);
        }

        private static void AddItemInternal(Address avatarAddress)
        {
            TryResetLoadedAvatarState(avatarAddress, out _, out _);
        }

        #endregion

        #region Avatar / RemoveItem

        /// <summary>
        /// (휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="guid"></param>
        public static void RemoveItem(Address avatarAddress, Guid guid)
        {
            var modifier = new AvatarInventoryNonFungibleItemRemover(guid);
            LocalStateSettings.Instance.Add(avatarAddress, modifier, true);
            RemoveItemInternal(avatarAddress, modifier);
        }

        /// <summary>
        /// (휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="id"></param>
        /// <param name="count"></param>
        public static void RemoveItem(Address avatarAddress, HashDigest<SHA256> id, int count)
        {
            if (count is 0)
                return;

            var modifier = new AvatarInventoryFungibleItemRemover(id, count);
            LocalStateSettings.Instance.Add(avatarAddress, modifier, true);
            RemoveItemInternal(avatarAddress, modifier);
        }

        /// <summary>
        /// (휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="idAndCountDictionary"></param>
        public static void RemoveItem(Address avatarAddress, Dictionary<HashDigest<SHA256>, int> idAndCountDictionary)
        {
            var modifier = new AvatarInventoryFungibleItemRemover(idAndCountDictionary);
            LocalStateSettings.Instance.Add(avatarAddress, modifier, true);
            RemoveItemInternal(avatarAddress, modifier);
        }

        private static void RemoveItemInternal(Address avatarAddress, AvatarStateModifier modifier)
        {
            if (!TryGetLoadedAvatarState(avatarAddress, out var outAvatarState, out _, out var isCurrentAvatarState))
                return;

            modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
                return;

            ReactiveAvatarState.Inventory.SetValueAndForceNotify(outAvatarState.inventory);
        }

        #endregion

        #region Avatar / Mail

        /// <summary>
        /// `avatarAddress`에 해당하는 아바타 상태의 `MailBox` 안에 `AttachmentMail` 리스트 중, `guid`를 보상으로 갖고 있는 메일을 신규 처리한다.(비휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="guid"></param>
        public static void AddNewAttachmentMail(Address avatarAddress, Guid guid)
        {
            var modifier = new AvatarAttachmentMailNewSetter(guid);
            LocalStateSettings.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(avatarAddress, out var outAvatarState, out _, out var isCurrentAvatarState))
                return;

            modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
                return;

            ReactiveAvatarState.MailBox.SetValueAndForceNotify(outAvatarState.mailBox);
        }

        /// <summary>
        /// `AddNewAttachmentMail()` 메서드 로직을 회귀한다.(비휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="guid"></param>
        public static void RemoveNewAttachmentMail(Address avatarAddress, Guid guid)
        {
            var modifier = new AvatarAttachmentMailNewSetter(guid);
            LocalStateSettings.Instance.Remove(avatarAddress, modifier);
            TryResetLoadedAvatarState(avatarAddress, out var outAvatarState, out var isCurrentAvatarState);
        }

        #endregion

        #region Avatar / Quest

        /// <summary>
        /// `avatarAddress`에 해당하는 아바타 상태의 `QuestList` 안의 퀘스트 중, 매개변수의 `id`를 가진 퀘스트를 신규 처리한다.(비휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="id"></param>
        public static void AddReceivableQuest(Address avatarAddress, int id)
        {
            var modifier = new AvatarQuestIsReceivableSetter(id);
            LocalStateSettings.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(avatarAddress, out var outAvatarState, out _, out var isCurrentAvatarState))
                return;

            modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
                return;

            ReactiveAvatarState.QuestList.SetValueAndForceNotify(outAvatarState.questList);
        }

        /// <summary>
        /// `avatarAddress`에 해당하는 아바타 상태의 `QuestList` 안의 퀘스트 중, 매개변수의 `id`를 가진 퀘스트의 신규 처리를 회귀한다.(비휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="id"></param>
        public static void RemoveReceivableQuest(Address avatarAddress, int id)
        {
            var modifier = new AvatarQuestIsReceivableSetter(id);
            LocalStateSettings.Instance.Remove(avatarAddress, modifier);
            TryResetLoadedAvatarState(avatarAddress, out _, out _);
        }

        #endregion

        #region WeeklyArena

        /// <summary>
        /// 현재 바라보고 있는 주간 아레나 상태의 `Gold`를 변경한다.(휘발)
        /// </summary>
        /// <param name="gold"></param>
        public static void ModifyWeeklyArenaGold(decimal gold)
        {
            if (gold is 0m)
                return;
            
            var state = States.Instance.WeeklyArenaState;
            if (state is null)
                return;

            var modifier = new WeeklyArenaGoldModifier(gold);
            LocalStateSettings.Instance.Add(state.address, modifier, true);
            modifier.Modify(state);
            WeeklyArenaStateSubject.Gold.OnNext(state.Gold);
        }

        /// <summary>
        /// 현재 바라보고 있는 주간 아레나 상태가 포함하고 있는 `ArenaInfo` 중 현재 아바타 상태의 주소에 해당하는 것을 활성화 시킨다.(휘발)
        /// </summary>
        /// <param name="addArenaInfoIfNotContained">주간 아레나 상태에 현재 아바타 정보가 없으면 넣어준다.</param>
        public static void AddWeeklyArenaInfoActivator(bool addArenaInfoIfNotContained = true)
        {
            var avatarState = States.Instance.CurrentAvatarState;
            var avatarAddress = avatarState.address;
            var weeklyArenaState = States.Instance.WeeklyArenaState;
            var weeklyArenaAddress = weeklyArenaState.address;

            if (addArenaInfoIfNotContained &&
                !weeklyArenaState.ContainsKey(avatarAddress))
            {
                weeklyArenaState.Set(avatarState);
            }
            
            var modifier = new WeeklyArenaInfoActivator(avatarAddress);
            LocalStateSettings.Instance.Add(weeklyArenaAddress, modifier, true);
            modifier.Modify(weeklyArenaState);
            WeeklyArenaStateSubject.WeeklyArenaState.OnNext(weeklyArenaState);
        }
        
        /// <summary>
        /// `AddWeeklyArenaInfoActivator()` 메서드 로직을 회귀한다.(휘발)
        /// </summary>
        /// <param name="weeklyArenaAddress"></param>
        /// <param name="avatarAddress"></param>
        public static void RemoveWeeklyArenaInfoActivator(Address weeklyArenaAddress, Address avatarAddress)
        {
            var modifier = new WeeklyArenaInfoActivator(avatarAddress);
            LocalStateSettings.Instance.Remove(weeklyArenaAddress, modifier, true);
            
            var state = States.Instance.WeeklyArenaState;
            if (!state.address.Equals(weeklyArenaAddress))
                return;
            
            modifier.Modify(state);
            WeeklyArenaStateSubject.WeeklyArenaState.OnNext(state);
        }

        #endregion

        /// <summary>
        /// `States.AvatarStates`가 포함하고 있는 아바타 상태 중에 `avatarAddress`와 같은 객체와 그 키를 반환한다.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="outAvatarState"></param>
        /// <param name="outKey"></param>
        /// <param name="isCurrentAvatarState"></param>
        /// <returns></returns>
        private static bool TryGetLoadedAvatarState(Address avatarAddress, out AvatarState outAvatarState,
            out int outKey, out bool isCurrentAvatarState)
        {
            var agentState = States.Instance.AgentState;
            if (agentState is null ||
                !agentState.avatarAddresses.ContainsValue(avatarAddress))
            {
                outAvatarState = null;
                outKey = -1;
                isCurrentAvatarState = false;
                return false;
            }

            foreach (var pair in States.Instance.AvatarStates)
            {
                if (!pair.Value.address.Equals(avatarAddress))
                    continue;

                outAvatarState = pair.Value;
                outKey = pair.Key;
                isCurrentAvatarState = outKey.Equals(States.Instance.CurrentAvatarKey);
                return true;
            }

            outAvatarState = null;
            outKey = -1;
            isCurrentAvatarState = false;
            return false;
        }

        /// <summary>
        /// `States.AddOrReplaceAvatarState(address, key, initializeReactiveState)`함수를 사용해서
        /// 이미 로드되어 있는 아바타 상태를 새로 할당한다.
        /// 따라서 이 함수를 사용한 후에 `ReactiveAvatarState`를 추가로 갱신할 필요가 없다.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="outAvatarState"></param>
        /// <param name="isCurrentAvatarState"></param>
        private static bool TryResetLoadedAvatarState(Address avatarAddress, out AvatarState outAvatarState,
            out bool isCurrentAvatarState)
        {
            if (!TryGetLoadedAvatarState(avatarAddress, out outAvatarState, out var outKey, out isCurrentAvatarState))
                return false;

            outAvatarState = States.Instance.AddOrReplaceAvatarState(avatarAddress, outKey, isCurrentAvatarState);
            return true;
        }
    }
}
