using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Libplanet;
using Nekoyume.State.Modifiers;

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
        #region Currency

        /// <summary>
        /// 에이전트의 골드를 변경한다.(휘발성)
        /// </summary>
        /// <param name="agentAddress"></param>
        /// <param name="gold"></param>
        public static void ModifyGold(Address agentAddress, decimal gold)
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
        public static void ModifyActionPoint(Address avatarAddress, int actionPoint)
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

        #region AddItem

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
            if (!TryResetLoadedAvatarState(avatarAddress, out var outAvatarState, out var isCurrentAvatarState))
                return;

            if (!isCurrentAvatarState)
                return;

            ReactiveAvatarState.Inventory.SetValueAndForceNotify(outAvatarState.inventory);
        }

        #endregion

        #region RemoveItem

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

        #region Mail

        /// <summary>
        /// `avatarAddress`에 해당하는 아바타 상태의 `MailBox` 안에 `AttachmentMail` 리스트 중, `guid`를 보상으로 갖고 있는 메일을 신규 처리한다.(비휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="guid"></param>
        public static void AddNewAttachmentMail(Address avatarAddress, Guid guid)
        {
            var modifier = new AvatarNewAttachmentMailSetter(guid);
            LocalStateSettings.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(avatarAddress, out var outAvatarState, out _, out var isCurrentAvatarState))
                return;

            modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
                return;

            ReactiveAvatarState.MailBox.SetValueAndForceNotify(outAvatarState.mailBox);
        }

        /// <summary>
        /// `avatarAddress`에 해당하는 아바타 상태의 `MailBox` 안에 `AttachmentMail` 리스트 중, `guid`를 보상으로 갖고 있는 메일의 신규 처리를 회귀한다.(비휘발성)
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="guid"></param>
        public static void RemoveNewAttachmentMail(Address avatarAddress, Guid guid)
        {
            var modifier = new AvatarNewAttachmentMailSetter(guid);
            LocalStateSettings.Instance.Remove(avatarAddress, modifier);

            if (!TryResetLoadedAvatarState(avatarAddress, out var outAvatarState, out var isCurrentAvatarState))
                return;

            if (!isCurrentAvatarState)
                return;

            ReactiveAvatarState.MailBox.SetValueAndForceNotify(outAvatarState.mailBox);
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
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="outAvatarState"></param>
        /// <param name="isCurrentAvatarState"></param>
        private static bool TryResetLoadedAvatarState(Address avatarAddress, out AvatarState outAvatarState,
            out bool isCurrentAvatarState)
        {
            if (!TryGetLoadedAvatarState(avatarAddress, out outAvatarState, out var outKey, out isCurrentAvatarState))
                return false;

            States.Instance.AddOrReplaceAvatarState(avatarAddress, outKey, !isCurrentAvatarState);
            return true;
        }
    }
}
