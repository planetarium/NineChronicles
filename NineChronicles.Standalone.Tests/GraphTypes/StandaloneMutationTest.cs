using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using Xunit;

namespace NineChronicles.Standalone.Tests.GraphTypes
{
    public class StandaloneMutationTest : GraphQLTestBase
    {
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
    }
}
