using System;
using BalanceTool.Runtime.Util;
using Libplanet.Action.State;
using Libplanet.Crypto;
using UnityEditor;
using UnityEngine;

namespace BalanceTool.Editor
{
    public class BalanceToolWindow : EditorWindow
    {
        private const string PlayDataCsvHeader =
            "world_id,stage_id,avatar_level,equipment_00_id,equipment_00_level,equipment_01_id,equipment_01_level,equipment_02_id,equipment_02_level,equipment_03_id,equipment_03_level,equipment_04_id,equipment_04_level,equipment_05_id,equipment_05_level,food_00_id,food_00_count,food_01_id,food_01_count,food_02_id,food_02_count,food_03_id,food_03_count,costume_00_id,costume_01_id,costume_02_id,costume_03_id,costume_04_id,costume_05_id,rune_00_id,rune_00_level,rune_01_id,rune_01_level,rune_02_id,rune_02_level,rune_03_id,rune_03_level,crystal_random_buff_id,play_count";

        private IWorld _prevStates;
        private Address _agentAddr;
        private int _avatarIndex;

        // inputs.
        [SerializeField]
        private string playDataCsv = PlayDataCsvHeader;

        [SerializeField]
        private int globalPlayCount = 0;

        // outputs.
        [SerializeField]
        private string output = string.Empty;

        private Vector2 _playDataCsvScrollPos;
        private Vector2 _outputScrollPos;
        private bool _workingInCalculate;

        [MenuItem("Tools/Lib9c/Balance Tool")]
        public static void ShowWindow() =>
            GetWindow<BalanceToolWindow>("Balance Tool", true).Show();

        private void OnEnable()
        {
            minSize = new Vector2(300f, 300f);
            _avatarIndex = 0;
            (
                _,
                _agentAddr,
                _,
                _prevStates) = InitializeUtil.InitializeStates(
                avatarIndex: _avatarIndex);

            _playDataCsvScrollPos = Vector2.zero;
            _outputScrollPos = Vector2.zero;
            _workingInCalculate = false;
        }

        private void OnGUI()
        {
            GUILayout.Label("Inputs", EditorStyles.boldLabel);
            GUILayout.Label("Play Data Csv");
            _playDataCsvScrollPos = EditorGUILayout.BeginScrollView(_playDataCsvScrollPos);
            playDataCsv = EditorGUI.TextArea(
                GetRect(minLineCount: 3),
                playDataCsv);
            EditorGUILayout.EndScrollView();
            globalPlayCount = int.TryParse(
                EditorGUILayout.TextField("Global Play Count", globalPlayCount.ToString()),
                out var gpc)
                ? gpc
                : globalPlayCount;

            EditorGUI.BeginDisabledGroup(_workingInCalculate);
            if (GUILayout.Button("Calculate"))
            {
                Calculate();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.Label("Outputs", EditorStyles.boldLabel);
            _outputScrollPos = EditorGUILayout.BeginScrollView(_outputScrollPos);
            EditorGUI.TextArea(
                GetRect(minLineCount: 3),
                output);
            EditorGUILayout.EndScrollView();
        }

        private Rect GetRect(int? minLineCount = null)
        {
            var minHeight = minLineCount.HasValue
                ? EditorGUIUtility.singleLineHeight * minLineCount.Value +
                  EditorGUIUtility.standardVerticalSpacing * (minLineCount.Value - 1)
                : EditorGUIUtility.singleLineHeight;

            return GUILayoutUtility.GetRect(
                1f,
                1f,
                minHeight,
                position.height,
                GUILayout.ExpandWidth(true));
        }

        private async void Calculate()
        {
            _workingInCalculate = true;
            try
            {
                var playDataList = globalPlayCount > 0
                    ? HackAndSlashCalculator.ConvertToPlayDataList(
                        playDataCsv,
                        globalPlayCount: globalPlayCount)
                    : HackAndSlashCalculator.ConvertToPlayDataList(
                        playDataCsv);
                var playDataListWithResult = await HackAndSlashCalculator.CalculateAsync(
                    _prevStates,
                    randomSeed: 0, // null,
                    0,
                    _agentAddr,
                    _avatarIndex,
                    playDataList);
                output = HackAndSlashCalculator.ConvertToCsv(playDataListWithResult);
                _workingInCalculate = false;
            }
            catch (Exception e)
            {
                output = e.Message + Environment.NewLine + e.StackTrace;
                Debug.LogException(e);
                _workingInCalculate = false;
            }
        }
    }
}
