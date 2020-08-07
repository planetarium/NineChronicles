using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using GraphQL;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Xunit;
using Xunit.Abstractions;

namespace NineChronicles.Standalone.Tests.GraphTypes
{
    public class StandaloneMutationTest : GraphQLTestBase
    {
        public StandaloneMutationTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task CreatePrivateKey()
        {
            // FIXME: passphrase로 "passphrase" 대신 랜덤 문자열을 사용하면 좋을 것 같습니다.
            var result = await ExecuteQueryAsync(
                "mutation { keyStore { createPrivateKey(passphrase: \"passphrase\") { publicKey { address } } } }");
            var createdPrivateKeyAddress = result.Data.As<Dictionary<string, object>>()["keyStore"]
                .As<Dictionary<string, object>>()["createPrivateKey"]
                .As<Dictionary<string, object>>()["publicKey"]
                .As<Dictionary<string, object>>()["address"].As<string>();

            Assert.Contains(KeyStore.List(),
                t => t.Item2.Address.ToString() == createdPrivateKeyAddress);
        }

        [Fact]
        public async Task CreatePrivateKeyWithGivenPrivateKey()
        {
            // FIXME: passphrase로 "passphrase" 대신 랜덤 문자열을 사용하면 좋을 것 같습니다.
            var privateKey = new PrivateKey();
            var privateKeyHex = ByteUtil.Hex(privateKey.ByteArray);
            var result = await ExecuteQueryAsync(
                $"mutation {{ keyStore {{ createPrivateKey(passphrase: \"passphrase\", privateKey: \"{privateKeyHex}\") {{ hex publicKey {{ address }} }} }} }}");
            var privateKeyResult = result.Data.As<Dictionary<string, object>>()["keyStore"]
                .As<Dictionary<string, object>>()["createPrivateKey"]
                .As<Dictionary<string, object>>();
            var createdPrivateKeyHex = privateKeyResult
                .As<Dictionary<string, object>>()["hex"].As<string>();
            var createdPrivateKeyAddress = privateKeyResult
                .As<Dictionary<string, object>>()["publicKey"]
                .As<Dictionary<string, object>>()["address"].As<string>();

            Assert.Equal(privateKey, new PrivateKey(ByteUtil.ParseHex(createdPrivateKeyHex)));
            Assert.Contains(KeyStore.List(),
                t => t.Item2.Address.ToString() == createdPrivateKeyAddress);
        }

        [Fact]
        public async Task RevokePrivateKey()
        {
            var privateKey = new PrivateKey();
            var passphrase = "";

            var protectedPrivateKey = ProtectedPrivateKey.Protect(privateKey, passphrase);
            KeyStore.Add(protectedPrivateKey);

            var address = privateKey.ToAddress();

            var result = await ExecuteQueryAsync(
                $"mutation {{ keyStore {{ revokePrivateKey(address: \"{address.ToHex()}\") {{ address }} }} }}");
            var revokedPrivateKeyAddress = result.Data.As<Dictionary<string, object>>()["keyStore"]
                .As<Dictionary<string, object>>()["revokePrivateKey"]
                .As<Dictionary<string, object>>()["address"].As<string>();

            Assert.DoesNotContain(KeyStore.List(),
                t => t.Item2.Address.ToString() == revokedPrivateKeyAddress);
            Assert.Equal(address.ToString(), revokedPrivateKeyAddress);
        }

        [Fact]
        public async Task ActivateAccount()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var activateAccounts = new[] { adminAddress }.ToImmutableHashSet();

            Block<PolymorphicAction<ActionBase>> genesis =
                BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock(
                    new PolymorphicAction<ActionBase>[]
                    {
                        new InitializeStates()
                        {
                            RankingState = new RankingState(),
                            ShopState = new ShopState(),
                            TableSheetsState = new TableSheetsState(),
                            GameConfigState = new GameConfigState(),
                            RedeemCodeState = new RedeemCodeState(Bencodex.Types.Dictionary.Empty
                                .Add("address", RedeemCodeState.Address.Serialize())
                                .Add("map", Bencodex.Types.Dictionary.Empty)
                            ),
                            AdminAddressState = new AdminState(adminAddress, 1500000),
                            ActivatedAccountsState = new ActivatedAccountsState(activateAccounts),
                            GoldCurrencyState = new GoldCurrencyState(new Currency("NCG", minter: null)),
                            GoldDistributions = new GoldDistribution[0],
                        },
                    }
                );

            var apvPrivateKey = new PrivateKey();
            var apv = AppProtocolVersion.Sign(apvPrivateKey, 0);
            var userPrivateKey = new PrivateKey();
            var properties = new LibplanetNodeServiceProperties<PolymorphicAction<ActionBase>>
            {
                Host = System.Net.IPAddress.Loopback.ToString(),
                AppProtocolVersion = apv,
                GenesisBlock = genesis,
                StorePath = null,
                StoreStatesCacheSize = 2,
                PrivateKey = userPrivateKey,
                Port = null,
                MinimumDifficulty = 4096,
                NoMiner = true,
                Render = false,
                Peers = ImmutableHashSet<Peer>.Empty,
                TrustedAppProtocolVersionSigners = null,
            };

            var service = new NineChroniclesNodeService(properties, null);
            service.PrivateKey = userPrivateKey;
            StandaloneContextFx.NineChroniclesNodeService = service;
            StandaloneContextFx.BlockChain = service.Swarm.BlockChain;

            var blockChain = StandaloneContextFx.BlockChain;

            var nonce = new byte[] {0x00, 0x01, 0x02, 0x03};
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);
            PolymorphicAction<ActionBase> action = new CreatePendingActivation(pendingActivation);
            blockChain.MakeTransaction(adminPrivateKey, new[] {action});
            await blockChain.MineBlock(adminAddress);

            var encodedActivationKey = activationKey.Encode();
            var queryResult = await ExecuteQueryAsync(
                $"mutation {{ activationStatus {{ activateAccount(encodedActivationKey: \"{encodedActivationKey}\") }} }}");
            await blockChain.MineBlock(adminAddress);

            var result = (bool)queryResult.Data
                .As<Dictionary<string, object>>()["activationStatus"]
                .As<Dictionary<string, object>>()["activateAccount"];
            Assert.True(result);

            var state = (Bencodex.Types.Dictionary)blockChain.GetState(
                ActivatedAccountsState.Address);
            var activatedAccountsState = new ActivatedAccountsState(state);
            var userAddress = userPrivateKey.ToAddress();
            Assert.True(activatedAccountsState.Accounts.Contains(userAddress));
        }
    }
}
