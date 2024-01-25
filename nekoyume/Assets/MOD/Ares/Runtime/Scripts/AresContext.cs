using System;
using System.Collections.Generic;
using System.Linq;
using BalanceTool;
using Cysharp.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using NineChronicles.MOD.Ares.UI;
using NineChronicles.MOD.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace NineChronicles.MOD.Ares
{
    public class AresContext
    {
        #region Bridge
        public IAgent Agent => Game.instance.Agent;
        public long BlockIndex => Game.instance.Agent.BlockIndex;
        public Dictionary<Type, (Address address, ISheet sheet)> Sheets =>
            TableSheets.Instance.ToSheets();
        public AgentState AgentState => States.Instance?.AgentState;
        public Address? AgentAddress => AgentState?.address;
        public AvatarState[] AvatarStates => States.Instance?.AvatarStates?.Values.ToArray();
        public Address[] AvatarAddresses => AvatarStates?
            .Select(a => a.address)
            .ToArray();
        public int? SelectedAvatarIndex => States.Instance?.CurrentAvatarKey;
        public AvatarState SelectedAvatarState => SelectedAvatarIndex.HasValue
            ? States.Instance.AvatarStates?[SelectedAvatarIndex.Value]
            : null;
        public Address? SelectedAvatarAddress => SelectedAvatarState?.address;
        public ItemSlotState SelectedItemSlotStatesForArena =>
            States.Instance?.CurrentItemSlotStates?[BattleType.Arena];
        public RuneSlotState SelectedRuneSlotStatesForArena =>
            States.Instance?.CurrentRuneSlotStates?[BattleType.Arena];
        public List<ArenaParticipantModel> ArenaParticipants =>
            RxProps.ArenaInformationOrderedWithScore?.Value;
        #endregion Bridge

        #region UI
        public IUI CurrentUI { get; set; }
        public int ArenaScoreBoardPage { get; set; }
        public readonly Dictionary<Address, (bool inProgress, float winRate)?> WinRates = new();
        #endregion UI

        public void Track(string eventName)
        {
            Analyzer.Instance.Track(eventName);
        }

        public ArenaParticipantModel[] GetArenaParticipants(int startIndex, int count)
        {
            if (ArenaParticipants == null)
            {
                return Array.Empty<ArenaParticipantModel>();
            }

            return ArenaParticipants
                .Skip(startIndex)
                .Take(count)
                .ToArray();
        }

        public Sprite GetItemIcon(int itemId)
        {
            return SpriteHelper.GetItemIcon(itemId);
        }

        public async UniTask<(bool inProgress, float winRate)?> GetWinRateAsync(Address enemyAvatarAddress)
        {
            if (WinRates.TryGetValue(enemyAvatarAddress, out var tuple))
            {
                return tuple;
            }

            var runeStates = await Agent.GetRuneStatesAsync(
                    SelectedAvatarAddress.Value,
                    runeSlotInfos: SelectedRuneSlotStatesForArena.GetEquippedRuneSlotInfos());

            var enemyAvatarStateValue = await Agent.GetAvatarStatesAsync(new[] { enemyAvatarAddress });
            var enemyAvatarState = enemyAvatarStateValue[enemyAvatarAddress];
            if (enemyAvatarState is null)
            {
                Debug.LogError($"Failed to get enemy avatar state: {enemyAvatarAddress}");
                return (false, 0);
            }

            var enemyItemSlotState = await Agent.GetItemSlotStateAsync(enemyAvatarAddress);
            var enemyRuneStates = await Agent.GetRuneStatesAsync(
                enemyAvatarAddress,
                runeSlotInfos: null);
            var winRate = ArenaCalculator.ExecuteArena(
                sheets: Sheets,
                randomSeed: null,
                avatarState: SelectedAvatarState,
                equipmentIds: SelectedItemSlotStatesForArena.Equipments,
                costumeIds: SelectedItemSlotStatesForArena.Costumes,
                runeStates: runeStates,
                enemyAvatarState: enemyAvatarState,
                enemyEquipmentIds: enemyItemSlotState.Equipments,
                enemyCostumeIds: enemyItemSlotState.Costumes,
                enemyRuneStates: enemyRuneStates,
                playCount: 700); // 700: 93% ~ 97% confidence interval
            tuple = (false, winRate);
            WinRates[enemyAvatarAddress] = tuple;
            return tuple;
        }
    }
}
