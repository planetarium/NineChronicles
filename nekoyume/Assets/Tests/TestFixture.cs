using System.IO;
using NUnit.Framework;
using Planetarium.Nekoyume.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tests
{
    [SetUpFixture]
    public class TestFixture
    {
        private string _path;
        [OneTimeSetUp]
        public void Init()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "clo_test_fixture.json");
            _path = Path.Combine(Application.streamingAssetsPath, "clo.json");
            if (File.Exists(_path))
                File.Delete(_path);
            File.Copy(path, _path);
            LibplanetEditor.DeleteAllEditor();
            SceneManager.LoadScene("Game");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            File.Delete(_path);
            File.Delete(_path + ".meta");
            LibplanetEditor.DeleteAllEditor();
        }
    }
}
