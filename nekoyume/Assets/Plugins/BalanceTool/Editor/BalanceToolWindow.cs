using System;
using BalanceTool.Runtime;
using BalanceTool.Runtime.Util.Lib9c.Tests.Util;
using Lib9c.DevExtensions;
using Libplanet;
using Libplanet.Action;
using UnityEditor;
using UnityEngine;

namespace BalanceTool.Editor
{
    public class BalanceToolWindow : EditorWindow
    {
        private const int WaveCountDefault = 3;

        private IAccountStateDelta _prevStates;
        private IRandom _random;
        private Address _agentAddr;
        private int _avatarIndex;

        // inputs.
        private string _playDataCsv;
        private Vector2 _playDataCsvScrollPos;
        private int _globalPlayCount;
        private int _waveCount;

        // outputs.
        private string _output;
        private Vector2 _outputScrollPos;

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
                _,
                _prevStates) = InitializeUtil.InitializeStates(
                avatarIndex: _avatarIndex);
            _random = new RandomImpl();
            _waveCount = WaveCountDefault;
        }

        private void OnGUI()
        {
            GUILayout.Label("Inputs", EditorStyles.boldLabel);
            GUILayout.Label("Play Data Csv");
            _playDataCsvScrollPos = EditorGUILayout.BeginScrollView(_playDataCsvScrollPos);
            _playDataCsv = EditorGUI.TextArea(
                GetRect(minLineCount: 3),
                _playDataCsv);
            EditorGUILayout.EndScrollView();
            _globalPlayCount = int.TryParse(
                EditorGUILayout.TextField("Global Play Count", _globalPlayCount.ToString()),
                out var globalPlayCount)
                ? globalPlayCount
                : _globalPlayCount;
            _waveCount = int.TryParse(
                EditorGUILayout.TextField("Wave Count", _waveCount.ToString()),
                out var waveCount)
                ? waveCount
                : _waveCount;
            if (GUILayout.Button("Calculate"))
            {
                try
                {
                    var playDataList = _globalPlayCount > 0
                        ? HackAndSlashCalculator.ConvertToPlayDataList(
                            _playDataCsv,
                            globalPlayCount: _globalPlayCount)
                        : HackAndSlashCalculator.ConvertToPlayDataList(
                            _playDataCsv);
                    var playDataListWithResult = HackAndSlashCalculator.Calculate(
                        _prevStates,
                        _random,
                        0,
                        _agentAddr,
                        _avatarIndex,
                        playDataList);
                    _output = HackAndSlashCalculator.ConvertToCsv(
                        playDataListWithResult,
                        waveCount: _waveCount);
                }
                catch (Exception e)
                {
                    _output = e.Message + Environment.NewLine + e.StackTrace;
                    Debug.LogException(e);
                }
            }

            GUILayout.Label("Outputs", EditorStyles.boldLabel);
            _outputScrollPos = EditorGUILayout.BeginScrollView(_outputScrollPos);
            EditorGUI.TextArea(
                GetRect(minLineCount: 3),
                _output);
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
    }
}
