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
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class TransferAssetTest
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

        private static readonly Currency _currency = new Currency("NCG", default(Address?));

        [Fact]
        public void Execute()
        {
            var balance = ImmutableDictionary<(Address, Currency), BigInteger>.Empty
                .Add((_sender, _currency), 1000)
                .Add((_recipient, _currency), 10);
            var prevState = new State(
                balance: balance
            );
            var action = new TransferAsset(
                sender: _sender,
                recipient: _recipient,
                amount: 100,
                currency: _currency
            );
            IAccountStateDelta nextState = action.Execute(new ActionContext()
            {
                PreviousStates = prevState,
                Signer = _sender,
                Rehearsal = false,
                BlockIndex = 1,
            });

            Assert.Equal(900, nextState.GetBalance(_sender, _currency));
            Assert.Equal(110, nextState.GetBalance(_recipient, _currency));
        }

        [Fact]
        public void ExecuteWithInvalidSigner()
        {
            var balance = ImmutableDictionary<(Address, Currency), BigInteger>.Empty
                .Add((_sender, _currency), 1000)
                .Add((_recipient, _currency), 10);
            var prevState = new State(
                balance: balance
            );
            var action = new TransferAsset(
                sender: _sender,
                recipient: _recipient,
                amount: 100,
                currency: _currency
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
        public void ExecuteWithInsufficientBalance()
        {
            var balance = ImmutableDictionary<(Address, Currency), BigInteger>.Empty
                .Add((_sender, _currency), 1000)
                .Add((_recipient, _currency), 10);
            var prevState = new State(
                balance: balance
            );
            var action = new TransferAsset(
                sender: _sender,
                recipient: _recipient,
                amount: 100000,
                currency: _currency
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
        public void Rehearsal()
        {
            var action = new TransferAsset(
                sender: _sender,
                recipient: _recipient,
                amount: 100,
                currency: _currency
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

        [Fact]
        public void PlainValue()
        {
            var action = new TransferAsset(_sender, _recipient, 100, _currency);
            Dictionary plainValue = (Dictionary)action.PlainValue;

            Assert.Equal(_sender, plainValue["sender"].ToAddress());
            Assert.Equal(_recipient, plainValue["recipient"].ToAddress());
            Assert.Equal(_currency, CurrencyExtensions.Deserialize((Dictionary)plainValue["currency"]));
            Assert.Equal(100, plainValue["amount"].ToBigInteger());
        }

        [Fact]
        public void LoadPlainValue()
        {
            var plainValue = new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"sender", _sender.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text)"recipient", _recipient.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text)"currency", _currency.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text)"amount", new BigInteger(100).Serialize()),
                }
            );
            var action = new TransferAsset();
            action.LoadPlainValue(plainValue);

            Assert.Equal(_sender, action.Sender);
            Assert.Equal(_recipient, action.Recipient);
            Assert.Equal(_currency, action.Currency);
            Assert.Equal(100, action.Amount);
        }

        [Fact]
        public void SerializeWithDotnetAPI()
        {
            var formatter = new BinaryFormatter();
            var action = new TransferAsset(_sender, _recipient, 100, _currency);

            using var ms = new MemoryStream();
            formatter.Serialize(ms, action);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (TransferAsset)formatter.Deserialize(ms);

            Assert.Equal(_sender, deserialized.Sender);
            Assert.Equal(_recipient, deserialized.Recipient);
            Assert.Equal(_currency, deserialized.Currency);
            Assert.Equal(100, deserialized.Amount);
        }
    }
}
