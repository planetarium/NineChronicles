namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using Nekoyume.Model;
    using Priority_Queue;
    using Xunit;

    public class SimplePriorityQueueTest
    {
        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void DeterministicFirstDequeue(int loopCount, decimal spd)
        {
            List<int> results = new List<int>();
            for (int i = 0; i < loopCount; i++)
            {
                SimplePriorityQueue<int, decimal> queue = new SimplePriorityQueue<int, decimal>();
                queue.Enqueue(0, spd);
                queue.Enqueue(1, spd);
                queue.Enqueue(2, spd);

                Assert.True(queue.TryDequeue(out int index));
                results.Add(index);
            }

            for (int i = 0; i < results.Count - 1; i++)
            {
                Assert.Equal(results[i], results[i + 1]);
            }
        }

        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void DeterministicIterateDequeueAndEnqueue(int loopCount, decimal spd)
        {
            List<int> results1 = new List<int>();
            List<int> results2 = new List<int>();
            for (int i = 0; i < 2; i++)
            {
                List<int> targetResults = i == 0
                    ? results1
                    : results2;

                SimplePriorityQueue<int, decimal> queue = new SimplePriorityQueue<int, decimal>();
                queue.Enqueue(0, spd);
                queue.Enqueue(1, spd);
                queue.Enqueue(2, spd);

                for (int j = 0; j < loopCount; j++)
                {
                    Assert.True(queue.TryDequeue(out int index));
                    targetResults.Add(index);

                    queue.Enqueue(index, spd);
                }
            }

            Assert.Equal(results1, results2);
        }

        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void DeterministicIterateDequeueAndEnqueueWithMultiplier(int loopCount, decimal spd)
        {
            List<int> results1 = new List<int>();
            List<int> results2 = new List<int>();
            const decimal multiplier = 0.6m;
            for (int i = 0; i < 2; i++)
            {
                List<int> targetResults = i == 0
                    ? results1
                    : results2;

                SimplePriorityQueue<int, decimal> queue = new SimplePriorityQueue<int, decimal>();
                queue.Enqueue(0, spd * multiplier);
                queue.Enqueue(1, spd * multiplier);
                queue.Enqueue(2, spd * multiplier);

                for (int j = 0; j < loopCount; j++)
                {
                    Assert.True(queue.TryDequeue(out int index));
                    targetResults.Add(index);

                    queue.Enqueue(index, spd * multiplier);
                }
            }

            Assert.Equal(results1, results2);
        }

        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void DeterministicIterateDequeueAndEnqueueWithDivisor(int loopCount, decimal spd)
        {
            List<int> results1 = new List<int>();
            List<int> results2 = new List<int>();
            const decimal divisor = 987654321m;
            for (int i = 0; i < 2; i++)
            {
                List<int> targetResults = i == 0
                    ? results1
                    : results2;

                SimplePriorityQueue<int, decimal> queue = new SimplePriorityQueue<int, decimal>();
                queue.Enqueue(0, spd / divisor);
                queue.Enqueue(1, spd / divisor);
                queue.Enqueue(2, spd / divisor);

                for (int j = 0; j < loopCount; j++)
                {
                    Assert.True(queue.TryDequeue(out int index));
                    targetResults.Add(index);

                    queue.Enqueue(index, spd / divisor);
                }
            }

            Assert.Equal(results1, results2);
        }

        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void DeterministicIterateDequeueAndUpdateAndEnqueue(int loopCount, decimal spd)
        {
            List<int> results1 = new List<int>();
            List<int> results2 = new List<int>();
            for (int i = 0; i < 2; i++)
            {
                List<int> targetResults = i == 0
                    ? results1
                    : results2;

                SimplePriorityQueue<int, decimal> queue = new SimplePriorityQueue<int, decimal>();
                queue.Enqueue(0, spd);
                queue.Enqueue(1, spd);
                queue.Enqueue(2, spd);

                for (int j = 0; j < loopCount; j++)
                {
                    Assert.True(queue.TryDequeue(out int index));
                    targetResults.Add(index);

                    foreach (int otherIndex in queue)
                    {
                        decimal priority = queue.GetPriority(otherIndex);
                        queue.UpdatePriority(otherIndex, priority);
                    }

                    queue.Enqueue(index, spd);
                }
            }

            Assert.Equal(results1, results2);
        }

        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void GuidDeterministicIterateDequeueAndUpdateAndEnqueue(int loopCount, decimal spd)
        {
            List<Guid> results1 = new List<Guid>();
            List<Guid> results2 = new List<Guid>();
            Guid[] guids = new[]
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
            };

            for (int i = 0; i < 2; i++)
            {
                List<Guid> targetResults = i == 0
                    ? results1
                    : results2;

                SimplePriorityQueue<Guid, decimal> queue = new SimplePriorityQueue<Guid, decimal>();
                for (int j = 0; j < guids.Length; j++)
                {
                    queue.Enqueue(guids[j], spd);
                }

                for (int j = 0; j < loopCount; j++)
                {
                    Assert.True(queue.TryDequeue(out Guid guid));
                    targetResults.Add(guid);

                    foreach (Guid otherGuid in queue)
                    {
                        decimal priority = queue.GetPriority(otherGuid);
                        queue.UpdatePriority(otherGuid, priority);
                    }

                    queue.Enqueue(guid, spd);
                }
            }

            Assert.Equal(results1, results2);
        }

        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void PlayerDeterministicIterateDequeueAndUpdateAndEnqueue(int loopCount, decimal spd)
        {
            var sheets = TableSheetsImporter.ImportSheets();
            TableSheets tableSheets = new TableSheets(sheets);
            Player[] players = new[]
            {
                new Player(
                    1,
                    tableSheets.CharacterSheet,
                    tableSheets.CharacterLevelSheet,
                    tableSheets.EquipmentItemSetEffectSheet),
                new Player(
                    2,
                    tableSheets.CharacterSheet,
                    tableSheets.CharacterLevelSheet,
                    tableSheets.EquipmentItemSetEffectSheet),
                new Player(
                    3,
                    tableSheets.CharacterSheet,
                    tableSheets.CharacterLevelSheet,
                    tableSheets.EquipmentItemSetEffectSheet),
            };

            List<Player> results1 = new List<Player>();
            List<Player> results2 = new List<Player>();
            for (int i = 0; i < 2; i++)
            {
                List<Player> targetResults = i == 0
                    ? results1
                    : results2;

                SimplePriorityQueue<Player, decimal> queue = new SimplePriorityQueue<Player, decimal>();
                for (int j = 0; j < players.Length; j++)
                {
                    queue.Enqueue(players[j], spd);
                }

                for (int j = 0; j < loopCount; j++)
                {
                    Assert.True(queue.TryDequeue(out Player player));
                    targetResults.Add(player);

                    foreach (Player otherPlayer in queue)
                    {
                        decimal priority = queue.GetPriority(otherPlayer);
                        queue.UpdatePriority(otherPlayer, priority);
                    }

                    queue.Enqueue(player, spd);
                }
            }

            Assert.Equal(results1, results2);
        }
    }
}
