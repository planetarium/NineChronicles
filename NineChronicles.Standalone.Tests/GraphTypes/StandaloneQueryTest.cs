using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Xunit;

namespace NineChronicles.Standalone.Tests.GraphTypes
{
    public class StandaloneQueryTest : GraphQLTestBase
    {
        [Fact]
        public async Task GetState()
        {
            var codec = new Codec();
            var miner = new Address();

            const int repeat = 10;
            foreach (long index in Enumerable.Range(1, repeat))
            {
                await BlockChain.MineBlock(miner);

                var result = await ExecuteQueryAsync($"query {{ state(address: \"{miner.ToHex()}\") }}");

                var data = (Dictionary<string, object>) result.Data;
                var state = (Integer)codec.Decode(ByteUtil.ParseHex((string) data["state"]));

                // TestRewardGold에서 miner에게 1 gold 씩 주므로 block index와 같을 것입니다.
                Assert.Equal((Integer)index, state);
            }
        }
    }
}
