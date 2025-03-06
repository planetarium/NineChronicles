using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Bencodex;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Model.State;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Editor
{
    public class CompareSheetsEditor : EditorWindow
    {
        private string[] _planets = new []
        {
            "0x000000000000",
            "0x000000000001",
            "0x000000000003",
            "0x100000000000",
            "0x100000000001",
            "0x100000000003",
        };

        private string[] _nodeUrls = new[]
        {
            "https://odin-rpc-1.nine-chronicles.com/graphql/explorer",
            "https://heimdall-rpc-1.nine-chronicles.com/graphql/explorer",
            "https://thor-rpc-1.nine-chronicles.com/graphql/explorer",
            "https://odin-internal-rpc-1.nine-chronicles.com/graphql/explorer",
            "https://heimdall-internal-rpc-1.nine-chronicles.com/graphql/explorer",
            "https://thor-internal-rpc-1.nine-chronicles.com/graphql/explorer",
        };

        [MenuItem("Tools/CompareSheets")]
        public static void ShowWindow()
        {
            GetWindow<CompareSheetsEditor>("CompareSheets Editor");
        }

        private int _selectedPlanetIndex = 0;
        private string _r2Url = "https://sheets.planetarium.dev";
        private IDictionary<string, string> _csvDict;
        private IDictionary<string, Address> _addressMap;
        private IDictionary<Address, IValue> _stateSheets;
        private List<string> _sheetNames;
        private Codec _codec = new Codec();
        private string _stateRootHash;

        private void OnGUI()
        {
            GUILayout.Label("Select a Planet ID", EditorStyles.boldLabel);

            _selectedPlanetIndex = EditorGUILayout.Popup("Planet ID", _selectedPlanetIndex, _planets);

            if (GUILayout.Button("Compare sheets"))
            {
                CompareSheetsAsync(_selectedPlanetIndex).Forget();
            }

            if (GUILayout.Button("Open diff directory"))
            {
                var directoryPath = Path.Combine(Application.dataPath, $"Editor/CompareSheets/{_planets[_selectedPlanetIndex]}");
                EditorUtility.RevealInFinder(directoryPath);
            }
        }

        private void LoadSheets()
        {
            var container = Resources
                .Load<AddressableAssetsContainer>(Game.AddressableAssetsContainerPath);

            var csvAssets = container.tableCsvAssets;
            _sheetNames = csvAssets.Select(x => x.name).ToList();
            Debug.Log($"Load AddressableAssets table name");
        }

        private async UniTaskVoid CompareSheetsAsync(int index)
        {
            LoadSheets();
            await GetStateRootHashRequest(_nodeUrls[index]);
            await GetStateSheets(index);
            await DownloadSheets();
            CompareSheets();
        }

        private async UniTask GetStateSheets(int index)
        {
            _addressMap = _sheetNames.ToDictionary(
                asset => asset,
                asset => Addresses.TableSheet.Derive(asset));
            var addresses = _sheetNames.Select(x => Addresses.TableSheet.Derive(x)).ToList();

            var nodeUrl = _nodeUrls[index];
            var sheets = await GetSheetsRequest(nodeUrl);
            _stateSheets = new Dictionary<Address, IValue>();
            for (int i = 0; i < sheets.Count; i++)
            {
                var raw = sheets[i];
                var address = addresses[i];
                var b = Convert.FromBase64String(raw);
                var value = _codec.Decode(b);
                _stateSheets[address] = value;
            }
            Debug.Log("GetStateSheets complete.");
        }

        private async UniTask DownloadSheets()
        {
            try
            {
                var planetId = _planets[_selectedPlanetIndex];
                var downloadedSheets = new ConcurrentDictionary<string, string>();
                const int maxRetries = 3;
                const float delayBetweenRetries = 2.0f;

                // Create a cancellation token source for the entire operation
                using var cts = new CancellationTokenSource();

                // Create a list to hold all download tasks
                var downloadTasks = _sheetNames.Select(async sheetName =>
                {
                    var csvName = $"{sheetName}.csv";
                    var sheetUrl = $"{_r2Url}/{planetId}/{csvName}";
                    bool success = false;

                    // Retry request if network request failed.
                    for (int attempt = 0;
                        attempt < maxRetries && !success && !cts.Token.IsCancellationRequested;
                        attempt++)
                    {
                        using (UnityWebRequest request = UnityWebRequest.Get(sheetUrl))
                        {
                            try
                            {
                                // UnityWebRequest를 UniTask로 변환
                                var operation = request.SendWebRequest();
                                while (!operation.isDone && !cts.Token.IsCancellationRequested)
                                {
                                    await UniTask.Yield();
                                }

                                if (cts.Token.IsCancellationRequested)
                                {
                                    throw new OperationCanceledException();
                                }

                                if (request.result == UnityWebRequest.Result.Success)
                                {
                                    var sheetData = request.downloadHandler.text;
                                    downloadedSheets.TryAdd(sheetName, sheetData);
                                    success = true;
                                }
                                else
                                {
                                    var errorMessage =
                                        $"Failed to download sheet {csvName} (Attempt {attempt + 1}): {request.error}";
                                    Debug.LogError(errorMessage);
                                    if (attempt < maxRetries - 1)
                                    {
                                        await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenRetries),
                                            cancellationToken: cts.Token);
                                    }
                                    else
                                    {
                                        await UniTask.SwitchToMainThread();
                                    }
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception e)
                            {
                                var errorMessage = $"Error downloading sheet {csvName}: {e.Message}";
                                Debug.LogError(errorMessage);
                                await UniTask.SwitchToMainThread();
                            }
                        }
                    }
                });
                await UniTask.WhenAll(downloadTasks);
                _csvDict = downloadedSheets;
                Debug.Log("DownloadSheets complete");
            }
            catch
            {
                // ignored
            }
        }

        private void CompareSheets()
        {
            var directoryPath = Path.Combine(Application.dataPath, $"Editor/CompareSheets/{_planets[_selectedPlanetIndex]}");
            foreach (var sheetName in _sheetNames)
            {
                var sheetAddress = _addressMap[sheetName];
                var value = _stateSheets[sheetAddress];
                string chainSheet = null;
                if (value is Text t)
                {
                    chainSheet = t.ToDotnetString();
                    chainSheet = chainSheet.Replace("\r", "").Trim();
                }

                string r2Sheet = _csvDict[sheetName].Replace("\r", "").Trim();
                bool isEqual = r2Sheet.Equals(chainSheet);
                if (!isEqual)
                {
                    Debug.LogError($"[CompareSheets]{sheetName} not equal");
                    Debug.LogError($"[CompareSheets]{sheetName} chain:\n {chainSheet}");
                    Debug.LogError($"[CompareSheets]{sheetName} r2:\n {r2Sheet}");
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    var chainFilePath = Path.Combine(directoryPath, $"{sheetName}_chain.csv");
                    var r2FilePath = Path.Combine(directoryPath, $"{sheetName}_r2.csv");
                    File.WriteAllText(chainFilePath, chainSheet);
                    File.WriteAllText(r2FilePath, r2Sheet);
                }
            }
        }

        private async UniTask GetStateRootHashRequest(string url)
        {
            var client = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());
            var query =
                "query {\n  blockQuery {\n    blocks(desc: true, limit: 1) {\n      stateRootHash\n    }\n  }\n}";
            var request = new GraphQLHttpRequest(query);
            var result = await client.SendQueryAsync<BlockResponse>(request);
            _stateRootHash = result.Data.blockQuery.blocks.First().stateRootHash;
            Debug.Log($"[GetStateRootHash]{_stateRootHash}");
        }

        private async UniTask<List<string>> GetSheetsRequest(string url)
        {
            var client = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());
            var query =
                "query($addresses: [Address!]!, $offsetStateRootHash: HashDigest_SHA256) {\n  stateQuery {\n    states(addresses: $addresses, offsetStateRootHash: $offsetStateRootHash)\n  }\n}";
            var request = new GraphQLHttpRequest(query, new
            {
                addresses = _addressMap.Values,
                offsetStateRootHash= _stateRootHash,
            });
            var result = await client.SendQueryAsync<SheetsResponse>(request);
            return result.Data.stateQuery.states;
        }

        public class Block
        {
            public string stateRootHash { get; set; }
        }

        public class BlockQuery
        {
            public List<Block> blocks { get; set; }
        }

        public class BlockResponse
        {
            public BlockQuery blockQuery { get; set; }
        }

        public class StateQuery
        {
            public List<string> states { get; set; }
        }

        public class SheetsResponse
        {
            public StateQuery stateQuery { get; set; }
        }
    }
}
