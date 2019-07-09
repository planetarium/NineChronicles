using NUnit.Framework;
using Nekoyume.BlockChain;
using UnityEngine;
using System.IO;

namespace Tests
{
    public class CommandLineOptionsTest
    {
        private static string jsonFixturePath = $"{Application.dataPath}/EditorTests/JSONFixture";
        [Test]
        public void EmptyJson()
        {
            var opt = AgentController.GetOptions(Path.Combine(jsonFixturePath, "clo_empty.json"));
            Assert.Null(opt.Port);
            Assert.Null(opt.Host);
            Assert.IsFalse(opt.NoMiner);
            Assert.IsEmpty(opt.Peers);
            Assert.Null(opt.PrivateKey);
        }

        [Test]
        public void P2PSeed() 
        {
            var opt = AgentController.GetOptions(Path.Combine(jsonFixturePath, "clo_seed.json"));
            Assert.AreEqual(5555, opt.Port);
            Assert.AreEqual("test.planetariumhq.com", opt.Host);
            Assert.IsFalse(opt.NoMiner);
            Assert.IsEmpty(opt.Peers);
            Assert.AreEqual("abcdefg", opt.PrivateKey);
        }

        [Test]
        public void P2PNoMiner()
        {
            var opt = AgentController.GetOptions(Path.Combine(jsonFixturePath, "clo_nominer.json"));
            Assert.Null(opt.Port);
            Assert.Null(opt.Host);
            Assert.IsTrue(opt.NoMiner);
            Assert.AreEqual(opt.Peers, new[] { "02ed49dbe0f2c34d9dff8335d6dd9097f7a3ef17dfb5f048382eebc7f451a50aa1,nekoalpha-tester0.koreacentral.cloudapp.azure.com,58598" });
            Assert.AreEqual("abcdefg", opt.PrivateKey);
        }
    }
}
