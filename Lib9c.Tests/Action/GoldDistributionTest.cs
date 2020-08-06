namespace Lib9c.Tests.Action
{
    using System.IO;
    using Libplanet;
    using Nekoyume.Action;
    using Xunit;

    public class GoldDistributionTest
    {
        public static readonly GoldDistribution[] Fixture =
        {
            new GoldDistribution
            {
                Address = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                AmountPerBlock = 100,
                StartBlock = 1,
                EndBlock = 100000,
            },
            new GoldDistribution
            {
                Address = new Address("Fb90278C67f9b266eA309E6AE8463042f5461449"),
                AmountPerBlock = 3000,
                StartBlock = 3600,
                EndBlock = 13600,
            },
            new GoldDistribution
            {
                Address = new Address("Fb90278C67f9b266eA309E6AE8463042f5461449"),
                AmountPerBlock = 100000000000,
                StartBlock = 2,
                EndBlock = 2,
            },
            new GoldDistribution
            {
                Address = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                AmountPerBlock = 1000000,
                StartBlock = 0,
                EndBlock = 0,
            },
        };

        public static string CreateFixtureCsvFile()
        {
            string csvPath = Path.GetTempFileName();
            using (StreamWriter writer = File.CreateText(csvPath))
            {
                writer.Write(@"Address,AmountPerBlock,StartBlock,EndBlock
F9A15F870701268Bd7bBeA6502eB15F4997f32f9,1000000,0,0
F9A15F870701268Bd7bBeA6502eB15F4997f32f9,100,1,100000
Fb90278C67f9b266eA309E6AE8463042f5461449,3000,3600,13600
Fb90278C67f9b266eA309E6AE8463042f5461449,100000000000,2,2
");
            }

            return csvPath;
        }

        [Fact]
        public void LoadInDescendingEndBlockOrder()
        {
            string fixturePath = CreateFixtureCsvFile();
            GoldDistribution[] records = GoldDistribution.LoadInDescendingEndBlockOrder(fixturePath);
            Assert.Equal(Fixture, records);
        }
    }
}
