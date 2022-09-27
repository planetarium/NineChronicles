namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class TransferAssetTest0
    {
        private static readonly Address _sender = new Address(
            new byte[]
            {
                 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
                 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
            }
        );

        private static readonly Address _recipient = new Address(new byte[]
            {
                 0x02, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
                 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
            }
        );

#pragma warning disable CS0618
        // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
        private static readonly Currency _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618

        [Fact]
        public void Constructor_ThrowsMemoLengthOverflowException()
        {
            Assert.Throws<MemoLengthOverflowException>(() =>
                new TransferAsset(_sender, _recipient, _currency * 100, new string(' ', 100)));
        }

        [Fact]
        public void Execute()
        {
            var balance = ImmutableDictionary<(Address, Currency), FungibleAssetValue>.Empty
                .Add((_sender, _currency), _currency * 1000)
                .Add((_recipient, _currency), _currency * 10);
            var prevState = new State(
                balance: balance
            );
            var action = new TransferAsset0(
                sender: _sender,
                recipient: _recipient,
                amount: _currency * 100
            );
            IAccountStateDelta nextState = action.Execute(new ActionContext()
            {
                PreviousStates = prevState,
                Signer = _sender,
                Rehearsal = false,
                BlockIndex = 1,
            });

            Assert.Equal(_currency * 900, nextState.GetBalance(_sender, _currency));
            Assert.Equal(_currency * 110, nextState.GetBalance(_recipient, _currency));
        }

        [Fact]
        public void ExecuteWithInvalidSigner()
        {
            var balance = ImmutableDictionary<(Address, Currency), FungibleAssetValue>.Empty
                .Add((_sender, _currency), _currency * 1000)
                .Add((_recipient, _currency), _currency * 10);
            var prevState = new State(
                balance: balance
            );
            var action = new TransferAsset0(
                sender: _sender,
                recipient: _recipient,
                amount: _currency * 100
            );

            var exc = Assert.Throws<InvalidTransferSignerException>(() =>
            {
                _ = action.Execute(new ActionContext()
                {
                    PreviousStates = prevState,
                    // 송금자가 직접 사인하지 않으면 실패해야 합니다.
                    Signer = _recipient,
                    Rehearsal = false,
                    BlockIndex = 1,
                });
            });

            Assert.Equal(exc.Sender, _sender);
            Assert.Equal(exc.Recipient, _recipient);
            Assert.Equal(exc.TxSigner, _recipient);
        }

        [Fact]
        public void ExecuteWithInvalidRecipient()
        {
            var balance = ImmutableDictionary<(Address, Currency), FungibleAssetValue>.Empty
                .Add((_sender, _currency), _currency * 1000);
            var prevState = new State(
                balance: balance
            );
            // Should not allow TransferAsset with same sender and recipient.
            var action = new TransferAsset0(
                sender: _sender,
                recipient: _sender,
                amount: _currency * 100
            );

            // No exception should be thrown when its index is less then 380000.
            _ = action.Execute(new ActionContext()
            {
                PreviousStates = prevState,
                Signer = _sender,
                Rehearsal = false,
                BlockIndex = 1,
            });

            var exc = Assert.Throws<InvalidTransferRecipientException>(() =>
            {
                _ = action.Execute(new ActionContext()
                {
                    PreviousStates = prevState,
                    Signer = _sender,
                    Rehearsal = false,
                    BlockIndex = 380001,
                });
            });

            Assert.Equal(exc.Sender, _sender);
            Assert.Equal(exc.Recipient, _sender);
        }

        [Fact]
        public void ExecuteWithInsufficientBalance()
        {
            var balance = ImmutableDictionary<(Address, Currency), FungibleAssetValue>.Empty
                .Add((_sender, _currency), _currency * 1000)
                .Add((_recipient, _currency), _currency * 10);
            var prevState = new State(
                balance: balance
            );
            var action = new TransferAsset0(
                sender: _sender,
                recipient: _recipient,
                amount: _currency * 100000
            );

            Assert.Throws<InsufficientBalanceException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = prevState,
                    Signer = _sender,
                    Rehearsal = false,
                    BlockIndex = 1,
                });
            });
        }

        [Fact]
        public void ExecuteWithMinterAsSender()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currencyBySender = Currency.Legacy("NCG", 2, _sender);
#pragma warning restore CS0618
            var balance = ImmutableDictionary<(Address, Currency), FungibleAssetValue>.Empty
                .Add((_sender, currencyBySender), _currency * 1000)
                .Add((_recipient, currencyBySender), _currency * 10);
            var prevState = new State(
                balance: balance
            );
            var action = new TransferAsset0(
                sender: _sender,
                recipient: _recipient,
                amount: currencyBySender * 100
            );
            var ex = Assert.Throws<InvalidTransferMinterException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = prevState,
                    Signer = _sender,
                    Rehearsal = false,
                    BlockIndex = 1,
                });
            });

            Assert.Equal(new[] { _sender }, ex.Minters);
            Assert.Equal(_sender, ex.Sender);
            Assert.Equal(_recipient, ex.Recipient);
        }

        [Fact]
        public void ExecuteWithMinterAsRecipient()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currencyByRecipient = Currency.Legacy("NCG", 2, _sender);
#pragma warning restore CS0618
            var balance = ImmutableDictionary<(Address, Currency), FungibleAssetValue>.Empty
                .Add((_sender, currencyByRecipient), _currency * 1000)
                .Add((_recipient, currencyByRecipient), _currency * 10);
            var prevState = new State(
                balance: balance
            );
            var action = new TransferAsset0(
                sender: _sender,
                recipient: _recipient,
                amount: currencyByRecipient * 100
            );
            var ex = Assert.Throws<InvalidTransferMinterException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = prevState,
                    Signer = _sender,
                    Rehearsal = false,
                    BlockIndex = 1,
                });
            });

            Assert.Equal(new[] { _sender }, ex.Minters);
            Assert.Equal(_sender, ex.Sender);
            Assert.Equal(_recipient, ex.Recipient);
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new TransferAsset0(
                sender: _sender,
                recipient: _recipient,
                amount: _currency * 100
            );

            IAccountStateDelta nextState = action.Execute(new ActionContext()
            {
                PreviousStates = new State(ImmutableDictionary<Address, IValue>.Empty),
                Signer = default,
                Rehearsal = true,
                BlockIndex = 1,
            });

            Assert.Equal(
                ImmutableHashSet.Create(
                    _sender,
                    _recipient
                ),
                nextState.UpdatedFungibleAssets.Keys
            );
            Assert.Equal(
                new[] { _currency },
                nextState.UpdatedFungibleAssets.Values.SelectMany(v => v).ToImmutableHashSet());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Nine Chronicles")]
        public void PlainValue(string memo)
        {
            var action = new TransferAsset0(_sender, _recipient, _currency * 100, memo);
            Dictionary plainValue = (Dictionary)action.PlainValue;

            Assert.Equal(_sender, plainValue["sender"].ToAddress());
            Assert.Equal(_recipient, plainValue["recipient"].ToAddress());
            Assert.Equal(_currency * 100, plainValue["amount"].ToFungibleAssetValue());
            if (!(memo is null))
            {
                Assert.Equal(memo, plainValue["memo"].ToDotnetString());
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Nine Chronicles")]
        public void LoadPlainValue(string memo)
        {
            IEnumerable<KeyValuePair<IKey, IValue>> pairs = new[]
            {
                new KeyValuePair<IKey, IValue>((Text)"sender", _sender.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"recipient", _recipient.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"amount", (_currency * 100).Serialize()),
            };
            if (!(memo is null))
            {
                pairs = pairs.Append(new KeyValuePair<IKey, IValue>((Text)"memo", memo.Serialize()));
            }

            var plainValue = new Dictionary(pairs);
            var action = new TransferAsset0();
            action.LoadPlainValue(plainValue);

            Assert.Equal(_sender, action.Sender);
            Assert.Equal(_recipient, action.Recipient);
            Assert.Equal(_currency * 100, action.Amount);
            Assert.Equal(memo, action.Memo);
        }

        [Fact]
        public void LoadPlainValue_ThrowsMemoLengthOverflowException()
        {
            var action = new TransferAsset0();
            var plainValue = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>((Text)"sender", _sender.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"recipient", _recipient.Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"amount", (_currency * 100).Serialize()),
                new KeyValuePair<IKey, IValue>((Text)"memo", new string(' ', 81).Serialize()),
            });

            Assert.Throws<MemoLengthOverflowException>(() => action.LoadPlainValue(plainValue));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Nine Chronicles")]
        public void SerializeWithDotnetAPI(string memo)
        {
            var formatter = new BinaryFormatter();
            var action = new TransferAsset0(_sender, _recipient, _currency * 100, memo);

            using var ms = new MemoryStream();
            formatter.Serialize(ms, action);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (TransferAsset0)formatter.Deserialize(ms);

            Assert.Equal(_sender, deserialized.Sender);
            Assert.Equal(_recipient, deserialized.Recipient);
            Assert.Equal(_currency * 100, deserialized.Amount);
            Assert.Equal(memo, deserialized.Memo);
        }
    }
}
