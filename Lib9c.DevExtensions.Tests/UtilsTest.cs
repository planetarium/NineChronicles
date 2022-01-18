using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Libplanet;
using Xunit;

namespace Lib9c.DevExtensions.Tests
{
    public class UtilsTest
    {
        [Fact]
        public void CreateActivationKeyTest()
        {
            uint countOfKeys = 10;
            Utils.CreateActivationKey(
                out var pendingActivationState,
                out var activationKeys,
                countOfKeys);

            Assert.Equal(countOfKeys, (uint)activationKeys.Count);
            Assert.Equal(activationKeys.Count, pendingActivationState.Count);

            var addresses = pendingActivationState.Select(p => p.address).ToImmutableHashSet();
            foreach (var key in activationKeys)
            {
                Assert.Contains(key.PendingAddress, addresses);
            }
        }

        [Fact]
        public void ImportSheetTest()
        {
            IDictionary<string, string> sheets = Utils.ImportSheets();

            string enhancement = Assert.Contains("EnhancementCostSheet", sheets);
            string gameConfig = Assert.Contains("GameConfigSheet", sheets);

            Assert.Contains("id,item_sub_type,grade,level,cost", enhancement);
            Assert.Contains("key,value", gameConfig);
        }

        [Fact]
        public void GetAuthorizedMinersState()
        {
            var json = @" {
                 ""validUntil"": 1500000,
                 ""interval"": 50,
                 ""miners"": [
                     ""0000000000000000000000000000000000000001"",
                     ""0000000000000000000000000000000000000002"",
                     ""0000000000000000000000000000000000000003"",
                     ""0000000000000000000000000000000000000004""
                 ] }";
            var configPath = Path.GetTempFileName();
            File.WriteAllText(configPath, json);

            var authorizedMinerState = Utils.GetAuthorizedMinersState(configPath);
            Assert.Equal(50, authorizedMinerState.Interval);
            Assert.Equal(1500000, authorizedMinerState.ValidUntil);
            Assert.Equal(
                new[]
                {
                    new Address("0000000000000000000000000000000000000001"),
                    new Address("0000000000000000000000000000000000000002"),
                    new Address("0000000000000000000000000000000000000003"),
                    new Address("0000000000000000000000000000000000000004"),
                }.ToImmutableHashSet(),
                authorizedMinerState.Miners);
        }

        [Fact]
        public void GetAdminState()
        {
            var json = @" {
                 ""validUntil"": 1500000,
                 ""adminAddress"": ""0000000000000000000000000000000000000001""
                 }";
            var configPath = Path.GetTempFileName();
            File.WriteAllText(configPath, json);

            var adminState = Utils.GetAdminState(configPath);
            Assert.Equal(1500000, adminState.ValidUntil);
            Assert.Equal(
                new Address("0000000000000000000000000000000000000001"),
                adminState.AdminAddress);
        }

        [Fact]
        public void GetActivatedAccounts()
        {
            var json = @"{""accounts"": [
                     ""0000000000000000000000000000000000000001"",
                     ""0000000000000000000000000000000000000002"",
                     ""0000000000000000000000000000000000000003"",
                     ""0000000000000000000000000000000000000004"",
                     ""0000000000000000000000000000000000000005""
                 ]}";
            var configPath = Path.GetTempFileName();
            File.WriteAllText(configPath, json);

            ImmutableHashSet<Address> activatedAccounts = Utils.GetActivatedAccounts(configPath);
            Assert.Equal(5, activatedAccounts.Count);
            Assert.Contains(new Address("0000000000000000000000000000000000000001"), activatedAccounts);
            Assert.Contains(new Address("0000000000000000000000000000000000000002"), activatedAccounts);
            Assert.Contains(new Address("0000000000000000000000000000000000000003"), activatedAccounts);
            Assert.Contains(new Address("0000000000000000000000000000000000000004"), activatedAccounts);
            Assert.Contains(new Address("0000000000000000000000000000000000000005"), activatedAccounts);
        }
    }
}
