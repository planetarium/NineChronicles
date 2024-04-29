using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using UnityEngine;

namespace Nekoyume.UI
{
    public class BattleTutorialController
    {
        private const string BattleTutorialDataPath = "Tutorial/Data/BattleTutorial";
        public class BattleTutorialModel
        {
            public int Id { get; set; }
            public int Stage { get; set; }
            public int ClearedWave { get; set; }
            public string L10NKey { get; set; }
            public int NextId { get; set; }
        }

        private readonly Dictionary<int, BattleTutorialModel> _modelDict = new();

        #region Initializing

        public BattleTutorialController()
        {
            var rawData = Resources.Load<TextAsset>(BattleTutorialDataPath).text;
            using var reader = new StringReader(rawData);
            using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
            csvReader.Read();
            csvReader.ReadHeader();
            while (csvReader.Read())
            {
                if (!csvReader.TryGetField<int>(0, out var id))
                {
                    NcDebug.LogWarning("id column is not found.");
                    continue;
                }

                if (!csvReader.TryGetField<int>(1, out var stage))
                {
                    NcDebug.LogWarning("stage column is not found.");
                    continue;
                }

                if (!csvReader.TryGetField<int>(2, out var wave))
                {
                    NcDebug.LogWarning("wave column is not found.");
                    continue;
                }

                if (!csvReader.TryGetField<string>(3, out var l10NKey))
                {
                    NcDebug.LogWarning("l10nKey column is not found.");
                    continue;
                }

                if (!csvReader.TryGetField<int>(4, out var nextId))
                {
                    NcDebug.LogWarning("nextId column is not found.");
                }

                _modelDict.Add(id, new BattleTutorialModel
                {
                    Id = id,
                    Stage = stage,
                    ClearedWave = wave,
                    L10NKey = l10NKey,
                    NextId = nextId
                });
            }

            foreach (var id in
                     _modelDict.Keys)
            {
                NcDebug.Log($"[BattleTutorialController]: contains {id}");
            }
        }

        #endregion

        public BattleTutorialModel GetBattleTutorialModel(int id)
        {
            return _modelDict[id];
        }

        public bool TryGetBattleTutorialModel(int id, out BattleTutorialModel model)
        {
            return _modelDict.TryGetValue(id, out model);
        }

        public List<BattleTutorialModel> GetModelListByStage(int stageId)
        {
            return _modelDict.Values.Where(model => model.Stage == stageId).ToList();
        }
    }
}
