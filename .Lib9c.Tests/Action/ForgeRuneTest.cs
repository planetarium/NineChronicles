namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class ForgeRuneTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private readonly Currency _goldCurrency;

        public ForgeRuneTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
            _goldCurrency = Currency.Legacy("NCG", 2, null);
        }

        [Fact]
        public void Execute()
        {
            var blockIndex = _tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(_agentAddress, new AgentState(_agentAddress).Serialize());

            foreach (var (key, value) in _sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.First().Value.Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarState.address, runeId);
            var runeState = new RuneState(runeId);
            state = state.SetState(runeStateAddress, runeState.Serialize());

            var costSheet = state.GetSheet<RuneCostSheet>();
            if (!costSheet.TryGetValue(runeId, out var costRow))
            {
                throw new RuneCostNotFoundException($"[{nameof(Execute)}] ");
            }

            if (!costRow.TryGetCost(runeState.Level, out var cost))
            {
                throw new RuneCostDataNotFoundException($"[{nameof(Execute)}] ");
            }

            var runeSheet = state.GetSheet<RuneSheet>();
            if (!runeSheet.TryGetValue(cost.RuneStoneId, out var runeRow))
            {
                throw new RuneNotFoundException($"[{nameof(Execute)}] ");
            }

            var ncgCurrency = state.GetGoldCurrency();
            var crystalCurrency = CrystalCalculator.CRYSTAL;
            var runeCurrency = Currency.Legacy(runeRow.Ticker, 0, minters: null);

            state = state.MintAsset(_agentAddress, cost.NcgQuantity * ncgCurrency);
            state = state.MintAsset(_agentAddress, cost.CrystalQuantity * crystalCurrency);
            state = state.MintAsset(avatarState.address, cost.RuneStoneQuantity * runeCurrency);

            var action = new ForgeRune()
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
            };
            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(0),
                Rehearsal = false,
                Signer = _agentAddress,
            };

            var nextState = action.Execute(ctx);
            if (!nextState.TryGetState(runeStateAddress, out List nextRuneRawState))
            {
                throw new Exception();
            }

            var nextRunState = new RuneState(nextRuneRawState);
            Assert.Equal(runeState.Level + 1, nextRunState.Level);
            var nextNcgBalance = nextState.GetBalance(_agentAddress, ncgCurrency);
            Assert.Equal("0", nextNcgBalance.GetQuantityString());
            var nextCrystalBalance = nextState.GetBalance(_agentAddress, crystalCurrency);
            Assert.Equal("0", nextCrystalBalance.GetQuantityString());
            var nextRuneBalance = nextState.GetBalance(avatarState.address, runeCurrency);
            Assert.Equal("0", nextRuneBalance.GetQuantityString());
        }

        [Fact]
        public void Execute_Ex()
        {
            var blockIndex = _tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(_agentAddress, new AgentState(_agentAddress).Serialize());

            foreach (var (key, value) in _sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.First().Value.Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarState.address, runeId);
            var runeState = new RuneState(runeId);
            state = state.SetState(runeStateAddress, runeState.Serialize());

            var costSheet = state.GetSheet<RuneCostSheet>();
            const string csv =
                @"id,rune_level,rune_stone_id,rune_stone_quantity,crystal_quantity,ncg_quantity,level_up_success_rate
        250010001,1,1001,10,100,100,1000
        ";
            costSheet.Set(csv);
            state = state.SetState(Addresses.TableSheet.Derive("RuneCostSheet"), costSheet.Serialize());
            if (!costSheet.TryGetValue(runeId, out var costRow))
            {
                throw new RuneCostNotFoundException($"[{nameof(Execute)}] ");
            }

            if (!costRow.TryGetCost(runeState.Level, out var cost))
            {
                throw new RuneCostDataNotFoundException($"[{nameof(Execute)}] ");
            }

            var runeSheet = state.GetSheet<RuneSheet>();
            if (!runeSheet.TryGetValue(cost.RuneStoneId, out var runeRow))
            {
                throw new RuneNotFoundException($"[{nameof(Execute)}] ");
            }

            var ncgCurrency = state.GetGoldCurrency();
            var crystalCurrency = CrystalCalculator.CRYSTAL;
            var runeCurrency = Currency.Legacy(runeRow.Ticker, 0, minters: null);

            var ncgBal = cost.NcgQuantity * ncgCurrency;
            var crystalBal = cost.CrystalQuantity * crystalCurrency;
            var runeBal = cost.RuneStoneQuantity * runeCurrency;

            state = state.MintAsset(_agentAddress, ncgBal);
            state = state.MintAsset(_agentAddress, crystalBal);
            state = state.MintAsset(avatarState.address, runeBal);

            var action = new ForgeRune()
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
            };
            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(0),
                Rehearsal = false,
                Signer = _agentAddress,
            };

            var nextState = action.Execute(ctx);
            if (!nextState.TryGetState(runeStateAddress, out List nextRuneRawState))
            {
                throw new Exception();
            }

            var nextRunState = new RuneState(nextRuneRawState);
            Assert.Equal(runeState.Level, nextRunState.Level);

            var nextNcgBal = nextState.GetBalance(_agentAddress, ncgCurrency);
            Assert.NotEqual(ncgBal, nextNcgBal);
            Assert.Equal("0", nextNcgBal.GetQuantityString());

            var nextCrystalBal = nextState.GetBalance(_agentAddress, ncgCurrency);
            Assert.NotEqual(crystalBal, nextCrystalBal);
            Assert.Equal("0", nextCrystalBal.GetQuantityString());

            var nextRuneBal = nextState.GetBalance(_agentAddress, ncgCurrency);
            Assert.NotEqual(runeBal, nextRuneBal);
            Assert.Equal("0", nextRuneBal.GetQuantityString());
        }

        [Fact]
        public void Execute_RuneCostNotFoundException()
        {
            var blockIndex = _tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(_agentAddress, new AgentState(_agentAddress).Serialize());

            foreach (var (key, value) in _sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.First().Value.Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarState.address, runeId);
            var runeState = new RuneState(128381293);
            state = state.SetState(runeStateAddress, runeState.Serialize());
            var action = new ForgeRune()
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
            };

            Assert.Throws<RuneCostNotFoundException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_RuneCostDataNotFoundException()
        {
            var blockIndex = _tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(_agentAddress, new AgentState(_agentAddress).Serialize());

            foreach (var (key, value) in _sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.First().Value.Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarState.address, runeId);
            var runeState = new RuneState(runeId);
            var costSheet = state.GetSheet<RuneCostSheet>();
            if (!costSheet.TryGetValue(runeId, out var costRow))
            {
                throw new RuneCostNotFoundException($"[{nameof(Execute)}] ");
            }

            for (var i = 0; i < costRow.Cost.Count + 1; i++)
            {
                runeState.LevelUp();
            }

            state = state.SetState(runeStateAddress, runeState.Serialize());

            var action = new ForgeRune()
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
            };

            Assert.Throws<RuneCostDataNotFoundException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_RuneNotFoundException()
        {
            var blockIndex = _tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(_agentAddress, new AgentState(_agentAddress).Serialize());

            foreach (var (key, value) in _sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.First().Value.Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarState.address, runeId);
            var runeState = new RuneState(runeId);
            state = state.SetState(runeStateAddress, runeState.Serialize());

            var costSheet = state.GetSheet<RuneSheet>();
            const string csv =
                @"id,ticker
9999,RUNE_FENRIR1
        ";
            costSheet.Set(csv);
            state = state.SetState(Addresses.TableSheet.Derive("RuneSheet"), costSheet.Serialize());

            var action = new ForgeRune()
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
            };

            Assert.Throws<RuneNotFoundException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }

        [Theory]
        [InlineData(false, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        public void Execute_NotEnoughFungibleAssetValueException(bool ncg, bool crystal, bool rune)
        {
            var blockIndex = _tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(_agentAddress, new AgentState(_agentAddress).Serialize());

            foreach (var (key, value) in _sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            var runeListSheet = state.GetSheet<RuneListSheet>();
            var runeId = runeListSheet.First().Value.Id;
            var runeStateAddress = RuneState.DeriveAddress(avatarState.address, runeId);
            var runeState = new RuneState(runeId);
            state = state.SetState(runeStateAddress, runeState.Serialize());

            var costSheet = state.GetSheet<RuneCostSheet>();
            if (!costSheet.TryGetValue(runeId, out var costRow))
            {
                throw new RuneCostNotFoundException($"[{nameof(Execute)}] ");
            }

            if (!costRow.TryGetCost(runeState.Level, out var cost))
            {
                throw new RuneCostDataNotFoundException($"[{nameof(Execute)}] ");
            }

            var runeSheet = state.GetSheet<RuneSheet>();
            if (!runeSheet.TryGetValue(cost.RuneStoneId, out var runeRow))
            {
                throw new RuneNotFoundException($"[{nameof(Execute)}] ");
            }

            var ncgCurrency = state.GetGoldCurrency();
            var crystalCurrency = CrystalCalculator.CRYSTAL;
            var runeCurrency = Currency.Legacy(runeRow.Ticker, 0, minters: null);

            if (ncg)
            {
                state = state.MintAsset(_agentAddress, cost.NcgQuantity * ncgCurrency);
            }

            if (crystal)
            {
                state = state.MintAsset(_agentAddress, cost.CrystalQuantity * crystalCurrency);
            }

            if (rune)
            {
                state = state.MintAsset(avatarState.address, cost.RuneStoneQuantity * runeCurrency);
            }

            var action = new ForgeRune()
            {
                AvatarAddress = avatarState.address,
                RuneId = runeId,
            };
            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(0),
                Rehearsal = false,
                Signer = _agentAddress,
            };

            Assert.Throws<NotEnoughFungibleAssetValueException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                    BlockIndex = blockIndex,
                }));
        }
    }
}
