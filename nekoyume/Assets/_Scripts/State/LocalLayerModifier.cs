using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Libplanet;
using Libplanet.Assets;
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
            var crystalBalance
                = await Game.Game.instance.Agent.GetBalanceAsync(
                    agentAddress,
                    CrystalCalculator.CRYSTAL);
            States.Instance.SetCrystalBalance(crystalBalance);
        }

        /// <summary>
        /// Modify the avatar's action point.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="actionPoint"></param>
        public static void ModifyAvatarActionPoint(Address avatarAddress, int actionPoint)
        {
            if (actionPoint is 0)
            {
                return;
            }

            var modifier = new AvatarActionPointModifier(actionPoint);
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

            if (!isCurrentAvatarState)
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);
            ReactiveAvatarState.UpdateActionPoint(outAvatarState.actionPoint);
        }

        #endregion

        #region Avatar / AddItem

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

        public static void AddNewMail(Address avatarAddress, Guid mailId)
        {
            var modifier = new AvatarMailNewSetter(mailId);
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
        /// <param name="resetState"></param>
        public static async void RemoveNewAttachmentMail(
            Address avatarAddress,
            Guid mailId,
            bool resetState = true)
        {
            var modifier = new AvatarAttachmentMailNewSetter(mailId);
            LocalLayer.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            await TryResetLoadedAvatarState(avatarAddress);
        }

        public static async void RemoveNewMail(
            Address avatarAddress,
            Guid mailId,
            bool resetState = true)
        {
            var modifier = new AvatarMailNewSetter(mailId);
            LocalLayer.Instance.Remove(avatarAddress, modifier);

            if (!resetState)
            {
                return;
            }

            await TryResetLoadedAvatarState(avatarAddress);
        }

        public static async void RemoveAttachmentResult(
            Address avatarAddress,
            Guid mailId,
            bool resetState = true)
        {
            var resultModifier = new AvatarAttachmentMailResultSetter(mailId);
            LocalLayer.Instance.Remove(avatarAddress, resultModifier);

            if (!resetState)
            {
                return;
            }

            await TryResetLoadedAvatarState(avatarAddress);
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

        /// <summary>
        /// Change the daily reward acquisition block index of the avatar.
        /// </summary>
        /// <param name="avatarAddress"></param>
        /// <param name="blockCount"></param>
        public static void IncreaseAvatarDailyRewardReceivedIndex(Address avatarAddress, long blockCount)
        {
            var modifier = new AvatarDailyRewardReceivedIndexModifier(blockCount);
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

            if (!isCurrentAvatarState)
            {
                return;
            }

            outAvatarState = modifier.Modify(outAvatarState);
            ReactiveAvatarState.UpdateDailyRewardReceivedIndex(
                outAvatarState.dailyRewardReceivedIndex);
        }

        public static void ModifyAvatarItemRequiredIndex(
            Address avatarAddress,
            Guid tradableId,
            long blockIndex
        )
        {
            var modifier = new AvatarItemRequiredIndexModifier(blockIndex, tradableId);
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

            ReactiveAvatarState.UpdateDailyRewardReceivedIndex(
                outAvatarState.dailyRewardReceivedIndex);
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
