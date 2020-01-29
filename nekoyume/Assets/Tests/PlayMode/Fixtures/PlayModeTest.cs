using System;
using System.Collections;
using System.IO;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Game;
using Nekoyume.Helper;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.PlayMode.Fixtures
{
    public abstract class PlayModeTest
    {
        private string _path;
        private string _backup;
        private string _storePath;
        internal MinerFixture miner;

        [UnitySetUp]
        protected IEnumerator CoSetUp()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "clo_test_fixture.json");
            _path = Path.Combine(Application.streamingAssetsPath, "clo.json");
            _backup = Path.Combine(Application.streamingAssetsPath, "clo_copy.json");
            _storePath = $"test_{Guid.NewGuid()}";
            var json = JsonUtility.FromJson<CommandLineOptions>(File.ReadAllText(path));
            json.storagePath = _storePath;
            json.privateKey = ByteUtil.Hex(new PrivateKey().ByteArray);
            var jsonText = JsonUtility.ToJson(json);
            if (File.Exists(_backup))
            {
                File.Delete(_backup);
            }
            if (File.Exists(_path))
            {
                File.Copy(_path, _backup);
                File.Delete(_path);
            }

            File.WriteAllText(_path, jsonText);
            Debug.Log(File.ReadAllText(_path));
            var load = SceneManager.LoadSceneAsync("Game");
            yield return new WaitUntil(() => load.isDone);
            yield return new WaitUntil(() => Game.instance.IsInitialized);
            Game.instance.Init();
        }

        [UnityTearDown]
        protected IEnumerator CoTearDown()
        {
            miner?.TearDown();
            File.Delete(_path);
            File.Delete(_path + ".meta");
            if (File.Exists(_backup))
            {
                File.Move(_backup, _path);
                File.Delete(_backup + ".meta");
            }

            yield return Game.instance.TearDown();
            if (!string.IsNullOrEmpty(_storePath))
            {
                var dir = Directory.GetCurrentDirectory();
                var path = Path.Combine(dir, _storePath);
                Debug.Log($"Delete {path}");
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }

                yield return new WaitWhile(() => Directory.Exists(path));
            }
        }
    }
}
