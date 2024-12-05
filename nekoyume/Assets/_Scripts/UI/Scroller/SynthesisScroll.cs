#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Nekoyume.Model.EnumType;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class SynthesisScroll : MonoBehaviour
    {
        [SerializeField]
        private Transform cellContainer = null!;

        [SerializeField]
        private GameObject cellTemplate = null!;

        private Dictionary<Grade, SynthesisCell> _cells = new();

        private void Awake()
        {
            ClearSamples();
            InitCells();
        }

        public void UpdateData(List<SynthesizeModel> synthesizeModels)
        {
            foreach (var kvp in _cells)
            {
                var cell = kvp.Value;
                cell.gameObject.SetActive(false);
            }

            foreach (var synthesizeModel in synthesizeModels)
            {
                if (!_cells.TryGetValue(synthesizeModel.Grade, out var cell))
                {
                    NcDebug.LogError($"Failed to get SynthesisCell for {synthesizeModel.Grade}.", this);
                    continue;
                }

                cell.gameObject.SetActive(true);
                cell.UpdateContent(synthesizeModel);
            }
        }

        private void ClearSamples()
        {
            foreach (Transform child in cellContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void InitCells()
        {
            _cells.Clear();
            foreach (Grade grade in Enum.GetValues(typeof(Grade)))
            {
                var cell = Instantiate(cellTemplate, cellContainer);
                if (!cell.TryGetComponent<SynthesisCell>(out var synthesisCell))
                {
                    NcDebug.LogError("Failed to get SynthesisCell component.", this);
                    throw new InvalidDataException();
                }
                synthesisCell.Initialize(grade);
                _cells.Add(grade, synthesisCell);
            }
        }
    }
}
