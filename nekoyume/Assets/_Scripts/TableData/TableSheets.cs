using System.Collections;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Nekoyume.TableData
{
    public class TableSheets
    {
        public readonly ReactiveProperty<float> loadProgress = new ReactiveProperty<float>();

        public Background Background { get; private set; }

        public IEnumerator CoInitialize()
        {
            loadProgress.Value = 0f;
            var loadLocationOperation = Addressables.LoadResourceLocationsAsync("TableCSV");
            yield return loadLocationOperation;
            var locations = loadLocationOperation.Result;
            var loadTaskCount = locations.Count;
            var loadedTaskCount = 0;
            foreach (var location in locations)
            {
                var loadAssetOperation = Addressables.LoadAssetAsync<TextAsset>(location);
                yield return loadAssetOperation;
                var asset = loadAssetOperation.Result;
                SetToSheet(asset.name, asset.text);
                loadedTaskCount++;
                loadProgress.Value = (float) loadedTaskCount / loadTaskCount;
            }

            loadProgress.Value = 1f;
        }

        private void SetToSheet(string name, string csv)
        {
            switch (name)
            {
                case nameof(TableData.Background):
                    Background = new Background();
                    Background.Set(csv);
                    break;
                default:
                    throw new InvalidDataException($"Not found {name} class in namespace `TableData`");
            }
        }
    }
}
