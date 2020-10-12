using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("create_avatar")]
    public class CreateAvatar : GameAction
    {
        public Address avatarAddress;
        public int index;
        public int hair;
        public int lens;
        public int ear;
        public int tail;
        public string name;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>()
        {
            ["avatarAddress"] = avatarAddress.Serialize(),
            ["index"] = (Integer) index,
            ["hair"] = (Integer) hair,
            ["lens"] = (Integer) lens,
            ["ear"] = (Integer) ear,
            ["tail"] = (Integer) tail,
            ["name"] = (Text) name,
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            index = (int) ((Integer) plainValue["index"]).Value;
            hair = (int) ((Integer) plainValue["hair"]).Value;
            lens = (int) ((Integer) plainValue["lens"]).Value;
            ear = (int) ((Integer) plainValue["ear"]).Value;
            tail = (int) ((Integer) plainValue["tail"]).Value;
            name = (Text) plainValue["name"];
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(ctx.Signer, MarkChanged);
                for (var i = 0; i < AvatarState.CombinationSlotCapacity; i++)
                {
                    var slotAddress = avatarAddress.Derive(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            CombinationSlotState.DeriveFormat,
                            i
                        )
                    );
                    states = states.SetState(slotAddress, MarkChanged);
                }

                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(Addresses.Ranking, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, GoldCurrencyState.Address, context.Signer);
            }

            if (!Regex.IsMatch(name, GameConfig.AvatarNickNamePattern))
            {
                throw new InvalidNamePatternException(
                    $"Aborted as the input name {name} does not follow the allowed name pattern.");
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("CreateAvatar exec started.");
            AgentState existingAgentState = states.GetAgentState(ctx.Signer);
            var agentState = existingAgentState ?? new AgentState(ctx.Signer);
            var avatarState = states.GetAvatarState(avatarAddress);
            if (!(avatarState is null))
            {
                throw new InvalidAddressException($"Aborted as there is already an avatar at {avatarAddress}.");
            }

            if (agentState.avatarAddresses.ContainsKey(index))
            {
                throw new AvatarIndexAlreadyUsedException(
                    $"Aborted as the signer already has an avatar at index #{index}.");
            }
            sw.Stop();
            Log.Debug("CreateAvatar Get AgentAvatarStates: {Elapsed}", sw.Elapsed);
            sw.Restart();

            Log.Debug("Execute CreateAvatar; player: {AvatarAddress}", avatarAddress);

            agentState.avatarAddresses.Add(index, avatarAddress);

            // Avoid NullReferenceException in test
            var materialItemSheet = ctx.PreviousStates.GetSheet<MaterialItemSheet>();

            var rankingState = ctx.PreviousStates.GetRankingState();

            var rankingMapAddress = rankingState.UpdateRankingMap(avatarAddress);

            avatarState = CreateAvatarState(name, avatarAddress, ctx, materialItemSheet, rankingMapAddress);

            if (hair < 0) hair = 0;
            if (lens < 0) lens = 0;
            if (ear < 0) ear = 0;
            if (tail < 0) tail = 0;

            avatarState.Customize(hair, lens, ear, tail);

            foreach (var address in avatarState.combinationSlotAddresses)
            {
                var slotState =
                    new CombinationSlotState(address, GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);
                states = states.SetState(address, slotState.Serialize());
            }

            avatarState.UpdateQuestRewards(materialItemSheet);

            sw.Stop();
            Log.Debug("CreateAvatar CreateAvatarState: {Elapsed}", sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("CreateAvatar Total Executed Time: {Elapsed}", ended - started);
            return states
                .SetState(ctx.Signer, agentState.Serialize())
                .SetState(Addresses.Ranking, rankingState.Serialize())
                .SetState(avatarAddress, avatarState.Serialize());
        }

        private static AvatarState CreateAvatarState(string name,
            Address avatarAddress,
            IActionContext ctx,
            MaterialItemSheet materialItemSheet,
            Address rankingMapAddress)
        {
            var state = ctx.PreviousStates;
            var gameConfigState = state.GetGameConfigState();
            var avatarState = new AvatarState(
                avatarAddress,
                ctx.Signer,
                ctx.BlockIndex,
                state.GetAvatarSheets(),
                gameConfigState,
                rankingMapAddress,
                name
            );

            if (GameConfig.IsEditor)
            {
                var costumeItemSheet = ctx.PreviousStates.GetSheet<CostumeItemSheet>();
                var equipmentItemSheet = ctx.PreviousStates.GetSheet<EquipmentItemSheet>();
                AddItemsForTest(avatarState, ctx.Random, costumeItemSheet, materialItemSheet, equipmentItemSheet);
            }

            return avatarState;
        }

        private static void AddItemsForTest(
            AvatarState avatarState,
            IRandom random,
            CostumeItemSheet costumeItemSheet,
            MaterialItemSheet materialItemSheet,
            EquipmentItemSheet equipmentItemSheet
        )
        {
            foreach (var row in costumeItemSheet.OrderedList)
            {
                avatarState.inventory.AddItem(ItemFactory.CreateCostume(row));
            }

            foreach (var row in materialItemSheet.OrderedList)
            {
                avatarState.inventory.AddItem(ItemFactory.CreateMaterial(row), 10);
            }

            foreach (var row in equipmentItemSheet.OrderedList.Where(row =>
                row.Id > GameConfig.DefaultAvatarWeaponId))
            {
                var itemId = random.GenerateRandomGuid();
                avatarState.inventory.AddItem(ItemFactory.CreateItemUsable(row, itemId, default));
            }
        }
    }
}
