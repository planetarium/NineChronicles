#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Bencodex.Types;
using CsvHelper;
using Lib9cCommonTool.Runtime;
using Libplanet.Crypto;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using StateViewer.Runtime;
using UnityEditor;
using UnityEngine;
using Material = Nekoyume.Model.Item.Material;
using Nekoyume;

namespace StateViewer.Editor.Features
{
    [Serializable]
    public class SetInventoryFeature : IStateViewerFeature
    {
        private const string ItemDataCsvDefault =
            @"item_id,count,level,is_tradable
200000,2,,
302001,2,,
302002,2,,true
10100000,2,,
10110000,2,10,
40100000,1,,
40100001,2,,";

        private readonly StateViewerWindow _editorWindow;

        private string _itemDataCsv = ItemDataCsvDefault;
        private Vector2 _itemDataCsvScrollPos;
        private Inventory? _inventory;
        private IValue _inventoryValue;
        private string _addrStr;

        [SerializeField]
        private StateTreeView stateTreeView;

        [SerializeField]
        private Vector2 stateTreeViewScrollPosition;

        public SetInventoryFeature(StateViewerWindow editorWindow)
        {
            _editorWindow = editorWindow;
            _inventoryValue = Null.Value;
            _addrStr = string.Empty;
            stateTreeView = new StateTreeView(
                tableSheets: editorWindow.GetTableSheets()!,
                contentKind: ContentKind.Inventory,
                visibleHeaderContents: (false, new[] { "ValueKind", "Add", "Remove" }));
            ClearAll();
        }

        private void ClearAll()
        {
            _inventoryValue = Null.Value;
            _addrStr = string.Empty;
            stateTreeView.ClearData();
            stateTreeViewScrollPosition = Vector2.zero;
        }

        public void OnGUI()
        {
            GUILayout.Label("Inputs", EditorStyles.boldLabel);
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            _itemDataCsvScrollPos = GUILayout.BeginScrollView(
                _itemDataCsvScrollPos,
                false,
                true);
            _itemDataCsv = EditorGUILayout.TextArea(
                _itemDataCsv,
                GUILayout.ExpandHeight(true));

            GUILayout.EndScrollView();
            var tableSheets = _editorWindow.GetTableSheets();
            EditorGUI.BeginDisabledGroup(
                string.IsNullOrEmpty(_itemDataCsv) ||
                tableSheets is null);
            if (GUILayout.Button("Create Inventory") &&
                !string.IsNullOrEmpty(_itemDataCsv) &&
                tableSheets is { })
            {
                _inventory = CreateInventory(_itemDataCsv, tableSheets);
                _inventoryValue = _inventory.Serialize();
                stateTreeView.SetData(
                    accountAddr: null,
                    addr: null,
                    data: _inventoryValue);
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            GUILayout.Label("Inventory State", EditorStyles.boldLabel);
            DrawStateTreeView();
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            _addrStr = EditorGUILayout.TextField("Address", _addrStr);
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            DrawHorizontalLine();
            var stateProxy = _editorWindow.GetStateProxy(drawHelpBox: true);
            EditorGUI.BeginDisabledGroup(stateProxy is null ||
                                         _inventoryValue.Kind == ValueKind.Null ||
                                         string.IsNullOrEmpty(_addrStr));
            if (GUILayout.Button("Set") &&
                stateProxy is { } &&
                _inventoryValue.Kind != ValueKind.Null &&
                !string.IsNullOrEmpty(_addrStr))
            {
                Address accountAddr, addr;
                try
                {
                    accountAddr = Addresses.Inventory;
                    addr = new Address(_addrStr);
                }
                catch (Exception e)
                {
                    NcDebug.LogException(e);
                    return;
                }

                var stateList = new List<(Address accountAddr, Address addr, IValue value)>
                {
                    (accountAddr, addr, _inventoryValue),
                };
                ActionManager.Instance?.ManipulateState(stateList, null);
            }

            EditorGUI.EndDisabledGroup();
        }

        private void DrawStateTreeView()
        {
            stateTreeViewScrollPosition = GUILayout.BeginScrollView(
                stateTreeViewScrollPosition,
                false,
                true);
            stateTreeView.OnGUI(GetRect(
                minLineCount: 5,
                maxHeight: _editorWindow.position.height));
            GUILayout.EndScrollView();
        }

        private static void DrawHorizontalLine()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private static Rect GetRect(
            float? minWidth = null,
            int? minLineCount = null,
            float? maxHeight = null)
        {
            var minHeight = minLineCount.HasValue
                ? EditorGUIUtility.singleLineHeight * minLineCount.Value +
                  EditorGUIUtility.standardVerticalSpacing * (minLineCount.Value - 1)
                : EditorGUIUtility.singleLineHeight;
            maxHeight ??= minHeight;

            return GUILayoutUtility.GetRect(
                minWidth ?? 10f,
                10f,
                minHeight,
                maxHeight.Value,
                GUILayout.ExpandWidth(true));
        }

        private static Inventory CreateInventory(string itemDataCsv, TableSheets tableSheets)
        {
            var inventory = new Inventory();
            var itemSheet = tableSheets.ItemSheet;
            using var reader = new StringReader(itemDataCsv);
            using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
            csvReader.Read();
            csvReader.ReadHeader();
            while (csvReader.Read())
            {
                if (!csvReader.TryGetField<int>("item_id", out var itemId))
                {
                    NcDebug.LogWarning("item_id column is not found.");
                    continue;
                }

                if (!csvReader.TryGetField<int>("count", out var count))
                {
                    count = 1;
                }

                if (!csvReader.TryGetField<int>("level", out var level))
                {
                    level = 0;
                }

                if (!csvReader.TryGetField<bool>("is_tradable", out var isTradable))
                {
                    isTradable = false;
                }

                if (!itemSheet.TryGetValue(itemId, out var itemRow))
                {
                    NcDebug.LogWarning($"{nameof(itemId)}({itemId}) does not exist.");
                    continue;
                }

                switch (itemRow.ItemType)
                {
                    case ItemType.Consumable:
                        for (var i = 0; i < count; i++)
                        {
                            var consumable = new Consumable(
                                (ConsumableItemSheet.Row)itemRow,
                                Guid.NewGuid(),
                                0);
                            inventory.AddItem(consumable);
                        }

                        break;
                    case ItemType.Costume:
                        for (var i = 0; i < count; i++)
                        {
                            var costume = new Costume(
                                (CostumeItemSheet.Row)itemRow,
                                Guid.NewGuid());
                            inventory.AddItem(costume);
                        }

                        break;
                    case ItemType.Equipment:
                        for (var i = 0; i < count; i++)
                        {
                            var equipment = BlacksmithMaster.CraftEquipment(
                                itemId,
                                level: level,
                                tableSheets: tableSheets);
                            inventory.AddItem(equipment);
                        }

                        break;
                    case ItemType.Material:
                        var material = isTradable
                            ? new TradableMaterial((MaterialItemSheet.Row)itemRow)
                            : new Material((MaterialItemSheet.Row)itemRow);
                        inventory.AddItem(material, count);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return inventory;
        }
    }
}
