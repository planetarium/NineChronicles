using System.IO;
using NUnit.Framework;
using Planetarium.Nekoyume.Editor;
using UnityEngine;

namespace Tests.PlayMode
{
    [SetUpFixture]
    public class TestFixture
    {
        private string _path;
        private string _backup;
        [OneTimeSetUp]
        public void Init()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "clo_test_fixture.json");
            _path = Path.Combine(Application.streamingAssetsPath, "clo.json");
            _backup = Path.Combine(Application.streamingAssetsPath, "clo_copy.json");
            if (File.Exists(_backup))
            {
                File.Delete(_backup);
            }
            if (File.Exists(_path))
            {
                File.Copy(_path, _backup);
                File.Delete(_path);
            }
            File.Copy(path, _path);
            LibplanetEditor.DeleteAllEditor();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            File.Delete(_path);
            File.Delete(_path + ".meta");
            if (File.Exists(_backup))
            {
                File.Move(_backup, _path);
                File.Delete(_backup + ".meta");
            }
            LibplanetEditor.DeleteAllEditor();
        }
    }
}
