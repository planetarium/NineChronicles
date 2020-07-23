namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class WeeklyArenaRewardTest : IDisposable
    {
        private TableSheets _tableSheets;

        public WeeklyArenaRewardTest()
        {
            _tableSheets = new TableSheets();
            _tableSheets.SetToSheet(nameof(WorldSheet), "test");
            _tableSheets.SetToSheet(nameof(QuestSheet), "test");
            _tableSheets.SetToSheet(nameof(QuestRewardSheet), "test");
            _tableSheets.SetToSheet(nameof(QuestItemRewardSheet), "test");
            _tableSheets.SetToSheet(nameof(EquipmentItemRecipeSheet), "test");
            _tableSheets.SetToSheet(nameof(EquipmentItemSubRecipeSheet), "test");
            _tableSheets.SetToSheet(
                nameof(CharacterSheet),
                "id,_name,size_type,elemental_type,hp,atk,def,cri,hit,spd,lv_hp,lv_atk,lv_def,lv_cri,lv_hit,lv_spd,attack_range,run_speed\n100010,전사,S,0,300,20,10,10,90,70,12,0.8,0.4,0,3.6,2.8,2,3");
        }

        public void Dispose()
        {
            _tableSheets = null;
        }

        [Fact]
        public void Execute()
        {
            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();
            var agent = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatar = new AvatarState(avatarAddress, agentAddress, 0, _tableSheets, new GameConfigState());
            agent.avatarAddresses.Add(0, avatarAddress);

            var weekly = new WeeklyArenaState(default(Address));
            weekly.Set(avatar, _tableSheets.CharacterSheet);
            weekly.End();

            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(default, weekly.Serialize())
                .Add(agentAddress, agent.Serialize())
                .Add(avatarAddress, avatar.Serialize()));

            state = (State)state.MintAsset(default, Currencies.Gold, 1000);
            state.TryGetGoldBalance(agentAddress, out var agentBalance);
            state.TryGetGoldBalance(default, out var arenaBalance);

            Assert.Equal(1000, arenaBalance);
            Assert.Equal(0, agentBalance);

            var action = new WeeklyArenaReward
            {
                AvatarAddress = avatarAddress,
                WeeklyArenaAddress = default,
            };
            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                BlockIndex = 1,
            });

            nextState.TryGetGoldBalance(agentAddress, out var reward);
            var nextInfo = nextState.GetWeeklyArenaState(default)[avatarAddress];

            Assert.True(nextInfo.Receive);
            Assert.True(reward > 0);
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new WeeklyArenaReward();
            var address = default(Address);
            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = new State(ImmutableDictionary<Address, IValue>.Empty),
                Signer = address,
                Rehearsal = true,
                BlockIndex = 1,
            });

            Assert.Equal(
                ImmutableHashSet.Create(
                    address
                ),
                nextState.UpdatedAddresses
            );
        }

        [Fact]
        public void ThrowAlreadyReceivedException()
        {
            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();
            var agent = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatar = new AvatarState(avatarAddress, agentAddress, 0, _tableSheets, new GameConfigState());
            agent.avatarAddresses.Add(0, avatarAddress);

            var weekly = new WeeklyArenaState(default(Address));
            weekly.Set(avatar, _tableSheets.CharacterSheet);
            weekly.End();
            weekly.SetReceive(avatarAddress);

            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(default, weekly.Serialize())
                .Add(agentAddress, agent.Serialize())
                .Add(avatarAddress, avatar.Serialize()));

            var action = new WeeklyArenaReward
            {
                AvatarAddress = avatarAddress,
                WeeklyArenaAddress = default,
            };

            var exception = Assert.Throws<AlreadyReceivedException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                BlockIndex = 1,
            }));

            Assert.Equal(
                $"Already Received Address. WeeklyArenaAddress: {default(Address)} AvatarAddress: {avatarAddress}",
                exception.Message);
        }

        [Fact]
        public void ThrowArenaNotEndedException()
        {
            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();
            var agent = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatar = new AvatarState(avatarAddress, agentAddress, 0, _tableSheets, new GameConfigState());
            agent.avatarAddresses.Add(0, avatarAddress);

            var weekly = new WeeklyArenaState(default(Address));
            weekly.Set(avatar, _tableSheets.CharacterSheet);

            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(default, weekly.Serialize())
                .Add(agentAddress, agent.Serialize())
                .Add(avatarAddress, avatar.Serialize()));

            var action = new WeeklyArenaReward
            {
                AvatarAddress = avatarAddress,
                WeeklyArenaAddress = default,
            };

            var exception = Assert.Throws<ArenaNotEndedException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                BlockIndex = 1,
            }));

            Assert.Equal(
                $"Arena has not ended yet. Address: {default(Address)}",
                exception.Message);
        }

        [Fact]
        public void ThrowFailedLoadStateExceptionAgentState()
        {
            var action = new WeeklyArenaReward();
            var address = default(Address);
            var exception = Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = new State(ImmutableDictionary<Address, IValue>.Empty),
                Signer = address,
                BlockIndex = 1,
            }));

            Assert.Equal($"Failed Load State: {nameof(AgentState)}. Address: {address}", exception.Message);
        }

        [Fact]
        public void ThrowFailedLoadStateExceptionAvatarState()
        {
            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();
            var agent = new AgentState(agentAddress);
            var avatarAddress = agentAddress.Derive("avatar");

            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(agentAddress, agent.Serialize()));

            var action = new WeeklyArenaReward
            {
                AvatarAddress = avatarAddress,
                WeeklyArenaAddress = default,
            };
            var exception = Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                BlockIndex = 1,
            }));

            Assert.Equal($"Failed Load State: {nameof(AvatarState)}. Address: {avatarAddress}", exception.Message);
        }

        [Fact]
        public void ThrowFailedLoadStateExceptionArenaState()
        {
            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();
            var agent = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatar = new AvatarState(avatarAddress, agentAddress, 0, _tableSheets, new GameConfigState());
            agent.avatarAddresses.Add(0, avatarAddress);

            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(agentAddress, agent.Serialize())
                .Add(avatarAddress, avatar.Serialize()));

            var action = new WeeklyArenaReward
            {
                AvatarAddress = avatarAddress,
                WeeklyArenaAddress = default,
            };
            var exception = Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                BlockIndex = 1,
            }));

            Assert.Equal(
                $"Failed Load State: {nameof(WeeklyArenaState)}. Address: {default(Address)}", exception.Message);
        }

        [Fact]
        public void ThrowKeyNotFoundException()
        {
            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();
            var agent = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatar = new AvatarState(avatarAddress, agentAddress, 0, _tableSheets, new GameConfigState());
            agent.avatarAddresses.Add(0, avatarAddress);

            var weekly = new WeeklyArenaState(default(Address));
            weekly.End();

            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(default, weekly.Serialize())
                .Add(agentAddress, agent.Serialize())
                .Add(avatarAddress, avatar.Serialize()));

            var action = new WeeklyArenaReward
            {
                AvatarAddress = avatarAddress,
                WeeklyArenaAddress = default,
            };

            var exception = Assert.Throws<KeyNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                BlockIndex = 1,
            }));

            Assert.Equal($"Arena {default(Address)} not contains {avatarAddress}", exception.Message);
        }
    }
}
