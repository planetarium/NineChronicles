namespace Lib9c.Tests.Action
{
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class StakeTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Currency _currency;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly TableSheets _tableSheets;
        private readonly Address _signerAddress;

        public StakeTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new State();

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

            _currency = new Currency("NCG", 2, minters: null);
            _goldCurrencyState = new GoldCurrencyState(_currency);

            _signerAddress = new PrivateKey().ToAddress();
            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .MintAsset(_signerAddress, _currency * 100);
        }

        [Fact]
        public void Execute_Throws_WhenNotEnoughBalance()
        {
            var action = new Stake(200);
            Assert.Throws<NotEnoughFungibleAssetValueException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousStates = _initialState,
                    Signer = _signerAddress,
                    BlockIndex = 100,
                }));
        }

        [Fact]
        public void Execute_Throws_WhenCancelOrUpdateWhileLockup()
        {
            var action = new Stake(50);
            var states = action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            // Cancel
            var updateAction = new Stake(0);
            Assert.Throws<RequiredBlockIndexException>(() => updateAction.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = 1,
            }));

            // Less
            updateAction = new Stake(10);
            Assert.Throws<RequiredBlockIndexException>(() => updateAction.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = 1,
            }));

            // More
            updateAction = new Stake(100);
            Assert.Throws<RequiredBlockIndexException>(() => updateAction.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = 1,
            }));
        }

        [Fact]
        public void Execute()
        {
            var action = new Stake(100);
            var states = action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            Assert.Equal(_currency * 0, states.GetBalance(_signerAddress, _currency));
            Assert.Equal(
                _currency * 100,
                states.GetBalance(StakeState.DeriveAddress(_signerAddress), _currency));

            states.TryGetStakeState(_signerAddress, out StakeState stakeState);
            Assert.Equal(0, stakeState.StartedBlockIndex);
            Assert.Equal(0 + StakeState.LockupInterval, stakeState.CancellableBlockIndex);
            Assert.Equal(0, stakeState.ReceivedBlockIndex);
            Assert.Equal(_currency * 100, states.GetBalance(stakeState.address, _currency));
            Assert.Equal(_currency * 0, states.GetBalance(_signerAddress, _currency));

            var achievements = stakeState.Achievements;
            Assert.False(achievements.Check(0, 0));
            Assert.False(achievements.Check(0, 1));
            Assert.True(achievements.Check(1, 0));

            var cancelAction = new Stake(0);
            states = cancelAction.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = StakeState.LockupInterval,
            });

            Assert.Equal(Null.Value, states.GetState(stakeState.address));
            Assert.Equal(_currency * 0, states.GetBalance(stakeState.address, _currency));
            Assert.Equal(_currency * 100, states.GetBalance(_signerAddress, _currency));
        }

        [Fact]
        public void Serialization()
        {
            var action = new Stake(100);
            var deserialized = new Stake();
            deserialized.LoadPlainValue(action.PlainValue);

            Assert.Equal(action.Amount, deserialized.Amount);
        }
    }
}
