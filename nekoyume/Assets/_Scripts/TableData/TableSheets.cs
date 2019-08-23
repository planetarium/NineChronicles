using System.Collections;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Nekoyume.TableData
{
    public class TableSheets
    {
        public readonly ReactiveProperty<float> loadProgress = new ReactiveProperty<float>();

        public BackgroundSheet BackgroundSheet { get; private set; }
        public WorldSheet WorldSheet { get; private set; }
        public WorldChapterSheet WorldChapterSheet { get; private set; }
        public StageSheet StageSheet { get; private set; }
        public StageRewardSheet StageRewardSheet { get; private set; }
        public CharacterSheet CharacterSheet { get; private set; }
        public LevelSheet LevelSheet { get; private set; }
        public SkillSheet SkillSheet { get; private set; }

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
                case nameof(TableData.BackgroundSheet):
                    BackgroundSheet = new BackgroundSheet();
                    BackgroundSheet.Set(csv);
                    break;
                case nameof(TableData.WorldSheet):
                    WorldSheet = new WorldSheet();
                    WorldSheet.Set(csv);
                    break;
                case nameof(TableData.WorldChapterSheet):
                    WorldChapterSheet = new WorldChapterSheet();
                    WorldChapterSheet.Set(csv);
                    break;
                case nameof(TableData.StageSheet):
                    StageSheet = new StageSheet();
                    StageSheet.Set(csv);
                    break;
                case nameof(TableData.StageRewardSheet):
                    StageRewardSheet = new StageRewardSheet();
                    StageRewardSheet.Set(csv);
                    break;
                case nameof(TableData.CharacterSheet):
                    CharacterSheet = new CharacterSheet();
                    CharacterSheet.Set(csv);
                    break;
                case nameof(TableData.LevelSheet):
                    LevelSheet = new LevelSheet();
                    LevelSheet.Set(csv);
                    break;
                case nameof(TableData.SkillSheet):
                    SkillSheet = new SkillSheet();
                    SkillSheet.Set(csv);
                    break;
                default:
                    throw new InvalidDataException($"Not found {name} class in namespace `TableData`");
            }
        }
    }
}
