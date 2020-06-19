using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.KeyStore;
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
                "mutation { keyStore { createPrivateKey(passphrase: \"passphrase\") { address } } }");
            var createdPrivateKeyAddress = result.Data.As<Dictionary<string, object>>()["keyStore"]
                .As<Dictionary<string, object>>()["createPrivateKey"]
                .As<Dictionary<string, object>>()["address"].As<string>();

            Assert.Contains(KeyStore.List(), t => t.Item2.Address.ToString() == createdPrivateKeyAddress);
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

            Assert.DoesNotContain(KeyStore.List(), t => t.Item2.Address.ToString() == revokedPrivateKeyAddress);
            Assert.Equal(address.ToString(), revokedPrivateKeyAddress);
        }
    }
}
