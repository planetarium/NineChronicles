using System;
using NUnit.Framework;
using UnityEngine;
using System.IO;
using Nekoyume.Helper;
using Nekoyume.L10n;
using UnityEngine.TestTools;

namespace Tests.EditMode
{
    public class CommandLineOptionsTest
    {
        private static string jsonFixturePath = $"{Application.dataPath}/Tests/EditMode/Fixtures";
        [Test]
        public void EmptyJson()
        {
            var opt = CommandLineOptions.Load(Path.Combine(jsonFixturePath, "clo_empty.json"));
            Assert.Null(opt.Port);
            Assert.Null(opt.Host);
            Assert.IsFalse(opt.NoMiner);
            Assert.IsEmpty(opt.Peers);
            Assert.Null(opt.PrivateKey);
            Assert.Null(opt.StoragePath);
        }

        [Test]
        public void P2PSeed()
        {
            var opt = CommandLineOptions.Load(Path.Combine(jsonFixturePath, "clo_seed.json"));
            Assert.AreEqual(5555, opt.Port);
            Assert.AreEqual("test.planetariumhq.com", opt.Host);
            Assert.IsFalse(opt.NoMiner);
            Assert.IsEmpty(opt.Peers);
            Assert.AreEqual("abcdefg", opt.PrivateKey);
            Assert.AreEqual(@"C:\Data", opt.StoragePath);
        }

        [Test]
        public void P2PNoMiner()
        {
            var opt = CommandLineOptions.Load(Path.Combine(jsonFixturePath, "clo_nominer.json"));
            Assert.Null(opt.Port);
            Assert.Null(opt.Host);
            Assert.IsTrue(opt.NoMiner);
            Assert.AreEqual(opt.Peers, new[] { "02ed49dbe0f2c34d9dff8335d6dd9097f7a3ef17dfb5f048382eebc7f451a50aa1,nekoalpha-tester0.koreacentral.cloudapp.azure.com,58598" });
            Assert.AreEqual("abcdefg", opt.PrivateKey);
            Assert.Null(opt.StoragePath);
        }

        [Test]
        public void Language()
        {
            Console.WriteLine(LanguageType.Korean);
            var opt = CommandLineOptions.Load(Path.Combine(jsonFixturePath, "clo_language.json"));
            Assert.AreEqual(LanguageType.Korean, LanguageTypeMapper.ISO396(opt.Language));
        }
    }
}
