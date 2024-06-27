using System;
using System.Numerics;
using System.Security.Cryptography;
using Cysharp.Threading.Tasks;
using Lib9c;
using Lib9c.Renderers;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State.Modifiers;

namespace Nekoyume.State
{
    /// <summary>
    /// This is a static class that collects the patterns of using the `Add` and `Remove` functions of `LocalStateSettings`.
    /// </summary>
    public static class LocalLayerModifier
    {
        #region Agent, Avatar / Currency

        /// <summary>
        /// Modify the agent's gold.
        /// </summary>
        /// <param name="agentAddress"></param>
        /// <param name="gold"></param>
        public static async UniTask ModifyAgentGoldAsync(Address agentAddress, FungibleAssetValue gold)
        {
            if (gold.Sign == 0)
            {
                return;
            }

            var modifier = new AgentGoldModifier(gold);
            LocalLayer.Instance.Add(agentAddress, modifier);

            //FIXME Avoid LocalLayer duplicate modify gold.
            var state = new GoldBalanceState(
                agentAddress,
                await Game.Game.instance.Agent.GetBalanceAsync(agentAddress, gold.Currency));
            if (!state.address.Equals(agentAddress))
            {
                return;
            }

            States.Instance.SetGoldBalanceState(state);
        }

        public static void ModifyAgentGold(Address agentAddress, BigInteger gold)
        {
            if (gold == 0)
            {
                return;
            }

            var fav = new FungibleAssetValue(
                States.Instance.GoldBalanceState.Gold.Currency,
                gold,
                0);
            ModifyAgentGoldAsync(agentAddress, fav).Forget();
        }

        public static void ModifyAgentGold<T>(ActionEvaluation<T> eval, Address agentAddress, BigInteger gold) where T : ActionBase
        {
            if (gold == 0)
            {
                return;
            }

            var fav = new FungibleAssetValue(
                States.Instance.GoldBalanceState.Gold.Currency,
                gold,
                0);

            ModifyAgentGold(eval, agentAddress, fav);
        }

        public static void ModifyAgentGold<T>(ActionEvaluation<T> eval, Address agentAddress, FungibleAssetValue fav) where T : ActionBase
        {
            var modifier = new AgentGoldModifier(fav);
            LocalLayer.Instance.Add(agentAddress, modifier);

            //FIXME Avoid LocalLayer duplicate modify gold.
            var state = new GoldBalanceState(
                agentAddress,
                StateGetter.GetBalance(eval.OutputState, agentAddress, fav.Currency));
            if (!state.address.Equals(agentAddress))
            {
                return;
            }

            States.Instance.SetGoldBalanceState(state);
        }

        public static async UniTask ModifyAgentCrystalAsync(Address agentAddress, BigInteger crystal)
        {
            if (crystal == 0)
            {
                return;
            }

            var fav = new FungibleAssetValue(
                CrystalCalculator.CRYSTAL,
                crystal,
                0);
            var modifier = new AgentCrystalModifier(fav);
            LocalLayer.Instance.Add(agentAddress, modifier);
            var crystalBalance = await Game.Game.instance.Agent.GetBalanceAsync(
                agentAddress,
                CrystalCalculator.CRYSTAL);
            States.Instance.SetCrystalBalance(crystalBalance);
        }

        public static void ModifyAgentCrystal<T>(ActionEvaluation<T> eval, Address agentAddress, BigInteger crystal) where T : ActionBase
        {
            if (crystal == 0)
            {
                return;
            }

            var fav = new FungibleAssetValue(
                CrystalCalculator.CRYSTAL,
                crystal,
                0);
            var modifier = new AgentCrystalModifier(fav);
            LocalLayer.Instance.Add(agentAddress, modifier);
            var crystalBalance =
                StateGetter.GetBalance(eval.OutputState, agentAddress, Currencies.Crystal);
            States.Instance.SetCrystalBalance(crystalBalance);
        }
        #endregion

        #region Avatar / AddItem

        public static async void AddNonFungibleItem(Address avatarAddress, Guid itemId, bool resetState = true)
        {
            var modifier = new AvatarInventoryNonFungibleItemRemover(itemId);
            LocalLayer.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            await TryResetLoadedAvatarState(avatarAddress);
        }

        public static async void AddItem(
            Address avatarAddress,
            Guid tradableId,
            long requiredBlockIndex,
            int count,
            bool resetState = true)
        {
            if (count is 0)
            {
                return;
            }

            var modifier = new AvatarInventoryTradableItemRemover(tradableId, requiredBlockIndex, count);
            LocalLayer.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            await TryResetLoadedAvatarState(avatarAddress);
        }

        // TODO: 메서드의 기능 자체가 관련된 컨텍스트를 모두 파악하고 있는게 아니라면 내부 구현을 파악할 수가 없다.
        // 전체적인 로컬레이어 기능 수정하면서 좀 더 명확하게 변경해야할듯
        public static async void AddItem(
            Address avatarAddress,
            HashDigest<SHA256> fungibleId,
            int count = 1,
            bool resetState = true)
        {
            if (count is 0)
            {
                return;
            }

            var modifier = new AvatarInventoryFungibleItemRemover(fungibleId, count);
            LocalLayer.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            await TryResetLoadedAvatarState(avatarAddress);
        }

        #endregion

        #region Avatar / RemoveItem

        public static void RemoveNonFungibleItem(Address avatarAddress, Guid itemId)
        {
            var modifier = new AvatarInventoryNonFungibleItemRemover(itemId);
            LocalLayer.Instance.Add(avatarAddress, modifier);
            RemoveItemInternal(avatarAddress, modifier);
        }

        public static void RemoveItem(Address avatarAddress, Guid tradableId, long requiredBlockIndex, int count)
        {
            var modifier = new AvatarInventoryTradableItemRemover(tradableId, requiredBlockIndex, count);
            LocalLayer.Instance.Add(avatarAddress, modifier);
            RemoveItemInternal(avatarAddress, modifier);
        }

        public static void RemoveItem(Address avatarAddress, HashDigest<SHA256> fungibleId, int count = 1)
        {
            if (count is 0)
            {
                return;
            }

            var modifier = new AvatarInventoryFungibleItemRemover(fungibleId, count);
            LocalLayer.Instance.Add(avatarAddress, modifier);
            RemoveItemInternal(avatarAddress, modifier);
        }


        private static void RemoveItemInternal(Address avatarAddress, AvatarStateModifier modifier)
        {
            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.UpdateInventory(outAvatarState.inventory);
        }

        #endregion

        #region Avatar / Mail

        /// <summary>
        /// Turns into a state where you can receive specific mail.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="mailId"></param>
        public static void AddNewAttachmentMail(Address avatarAddress, Guid mailId)
        {
            var modifier = new AvatarAttachmentMailNewSetter(mailId);
            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.UpdateMailBox(outAvatarState.mailBox);
        }

        public static void AddNewMail(AvatarState avatarState, Guid mailId)
        {
            UnityEngine.Debug.Log($"[AddNewMail] AddNewMail mailid : {mailId}");
            var modifier = new AvatarMailNewSetter(mailId);
            LocalLayer.Instance.Add(avatarState.address, modifier);
            avatarState = modifier.Modify(avatarState);
            ReactiveAvatarState.UpdateMailBox(avatarState.mailBox);
        }

        public static void AddNewMail(Address avatarAddress, Guid mailId)
        {
            UnityEngine.Debug.Log($"[AddNewMail] AddNewMail mailid : {mailId}");
            var modifier = new AvatarMailNewSetter(mailId);
            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                UnityEngine.Debug.LogError($"[AddNewMail] AddNewMail TryGetLoadedAvatarState fail mailid : {mailId}");
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                UnityEngine.Debug.LogError($"[AddNewMail] AddNewMail isCurrentAvatarState fail mailid : {mailId}");
                return;
            }

            ReactiveAvatarState.UpdateMailBox(outAvatarState.mailBox);
        }

        public static void AddNewResultAttachmentMail(
            Address avatarAddress,
            Guid mailId,
            long blockIndex
        )
        {
            var modifier = new AvatarAttachmentMailResultSetter(blockIndex, mailId);
            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            AddNewAttachmentMail(avatarAddress, mailId);
        }

        /// <summary>
        /// Regress the logic of the `AddNewAttachmentMail()` method.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="mailId"></param>
        public static void RemoveNewAttachmentMail(
            Address avatarAddress,
            Guid mailId)
        {
            UnityEngine.Debug.Log($"[MailRead] RemoveNewAttachmentMail mailid : {mailId}");
            var modifier = new AvatarAttachmentMailNewSetter(mailId);
            LocalLayer.Instance.Remove(avatarAddress, modifier);
        }

        public static void RemoveNewMail(
            Address avatarAddress,
            Guid mailId)
        {
            UnityEngine.Debug.Log($"[MailRead] RemoveNewMail mailid : {mailId}");
            var modifier = new AvatarMailNewSetter(mailId);
            LocalLayer.Instance.Remove(avatarAddress, modifier);
        }

        #endregion

        #region Avatar / Quest

        /// <summary>
        /// Changes to a state where you can receive quests.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="id"></param>
        public static void AddReceivableQuest(Address avatarAddress, int id)
        {
            var modifier = new AvatarQuestIsReceivableSetter(id);
            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.UpdateQuestList(outAvatarState.questList);
        }

        /// <summary>
        /// Regress the logic of the `AddReceivableQuest()` method.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="id"></param>
        /// <param name="resetState"></param>
        public static async void RemoveReceivableQuest(
            Address avatarAddress,
            int id,
            bool resetState = true)
        {
            var modifier = new AvatarQuestIsReceivableSetter(id);
            LocalLayer.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            await TryResetLoadedAvatarState(avatarAddress);
        }

        #endregion

        #region Avatar

        public static void SetItemEquip(
            Address avatarAddress,
            ItemBase item,
            bool equip,
            bool resetState = true)
        {
            if (!(item is INonFungibleItem nonFungibleItem))
            {
                return;
            }

            var modifier = new AvatarInventoryItemEquippedModifier(nonFungibleItem.NonFungibleId, equip);
            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                    avatarAddress,
                    out var outAvatarState,
                    out _,
                    out var isCurrentAvatarState)
               )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!resetState ||
                !isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.UpdateInventory(outAvatarState.inventory);
        }

        /// <summary>
        /// Change the equipment's mounting status.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="nonFungibleId"></param>
        /// <param name="equip"></param>
        /// <param name="resetState"></param>
        public static void SetItemEquip(
            Address avatarAddress,
            Guid nonFungibleId,
            bool equip,
            bool resetState = true)
        {
            var modifier = new AvatarInventoryItemEquippedModifier(nonFungibleId, equip);
            LocalLayer.Instance.Add(avatarAddress, modifier);

            if (!TryGetLoadedAvatarState(
                avatarAddress,
                out var outAvatarState,
                out _,
                out var isCurrentAvatarState)
            )
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);

            if (!resetState ||
                !isCurrentAvatarState)
            {
                return;
            }

            ReactiveAvatarState.UpdateInventory(outAvatarState.inventory);
        }

        public static void AddWorld(Address avatarAddress, int worldId)
        {
            var modifier = new AvatarWorldInformationAddWorldModifier(worldId);
            if (avatarAddress.Equals(States.Instance.CurrentAvatarState.address))
            {
                modifier.Modify(States.Instance.CurrentAvatarState);
            }

            LocalLayer.Instance.Add(avatarAddress, modifier);
        }

        #endregion

        /// <summary>
        /// Returns the same object as `avatarAddress` and its key among the avatar states included in `States.AvatarStates`.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="outAvatarState"></param>
        /// <param name="outKey"></param>
        /// <param name="isCurrentAvatarState"></param>
        /// <returns></returns>
        private static bool TryGetLoadedAvatarState(
            Address avatarAddress,
            out AvatarState outAvatarState,
            out int outKey,
            out bool isCurrentAvatarState)
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
                {
                    continue;
                }

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
        /// Use the `States.AddOrReplaceAvatarState(address, key, initializeReactiveState)` function to newly allocate the already loaded avatar state.
        /// Therefore, there is no need to additionally update `ReactiveAvatarState` after using this function.
        /// </summary>
        /// <param name="avatarAddress"></param>
        private static async UniTask TryResetLoadedAvatarState(Address avatarAddress)
        {
            if (!TryGetLoadedAvatarState(avatarAddress, out _, out var outKey,
                out var isCurrentAvatarState))
            {
                return;
            }

            await States.Instance.AddOrReplaceAvatarStateAsync(
                avatarAddress,
                outKey,
                isCurrentAvatarState);
        }
    }
}
