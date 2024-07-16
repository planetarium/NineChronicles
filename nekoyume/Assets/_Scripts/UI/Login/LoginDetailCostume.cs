using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = UnityEngine.UI.Toggle;

namespace Nekoyume.UI
{
    public class LoginDetailCostume : MonoBehaviour
    {
        [SerializeField] private GameObject costumePaletteContainer;
        [SerializeField] private GameObject costumeTabsContainer;

        [SerializeField] private Toggle earToggle;
        [SerializeField] private Toggle tailToggle;
        [SerializeField] private Toggle hairToggle;
        [SerializeField] private Toggle eyeToggle;

        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private GridLayoutGroup gridLayoutGroup;
        [SerializeField] private ColorSelectView colorSelectViewPrefab;

        [SerializeField] private CostumeColorScriptableObject costumeColorScriptableObject;

        private readonly List<ColorSelectView> _views = new();
        private ColorSelectView _selectedView;
        private ItemSubType _currentSubType;

        private readonly Dictionary<ItemSubType, int> _index = new()
        {
            { ItemSubType.EarCostume, 0 },
            { ItemSubType.TailCostume, 0 },
            { ItemSubType.HairCostume, 0 },
            { ItemSubType.EyeCostume, 0 }
        };

        private void Awake()
        {
            earToggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn)
                {
                    return;
                }

                UpdateTab(ItemSubType.EarCostume);
            });

            tailToggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn)
                {
                    return;
                }

                UpdateTab(ItemSubType.TailCostume);
            });

            hairToggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn)
                {
                    return;
                }

                UpdateTab(ItemSubType.HairCostume);
            });

            eyeToggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn)
                {
                    return;
                }

                UpdateTab(ItemSubType.EyeCostume);
            });
        }

        private void InitTab()
        {
            _views.Clear();
            _currentSubType = default;
            earToggle.isOn = false;
            foreach (Transform oldCell in gridLayoutGroup.transform)
            {
                Destroy(oldCell.gameObject);
            }

            var cell = gridLayoutGroup.cellSize;
            var spacing = gridLayoutGroup.spacing;
            var rect = gridLayoutGroup.GetComponent<RectTransform>().rect;
            var column = Util.GetGridItemCount(cell.x, spacing.x, rect.width);
            var row = Util.GetGridItemCount(cell.y, spacing.y, rect.height);

            var sum = column * row;
            for (var i = 0; i < sum; i++)
            {
                var view = Instantiate(colorSelectViewPrefab, gridLayoutGroup.transform);
                view.gameObject.SetActive(false);
                _views.Add(view);
            }
        }

        private void UpdateTab(ItemSubType itemSubType)
        {
            if (_currentSubType == itemSubType)
            {
                return;
            }

            _currentSubType = itemSubType;
            var colorData = costumeColorScriptableObject.colorSelectList
                .First(x => x.itemSubType == _currentSubType).colorSelect;

            for (var i = 0; i < _views.Count; i++)
            {
                var view = _views[i];
                if (i < colorData.Count)
                {
                    var j = i;
                    view.gameObject.SetActive(true);
                    view.Set(colorData[i], () => SetSelectedView(j));
                }
                else
                {
                    view.gameObject.SetActive(false);
                }
            }

            SetSelectedView(_index[_currentSubType], false);
            titleText.text = _currentSubType.GetLocalizedString();
        }

        private void SetSelectedView(int index, bool updateCostume = true)
        {
            if (_selectedView != null)
            {
                _selectedView.Selected = false;
                _selectedView = null;
            }

            var selectedView = _views[index];
            if (selectedView == _selectedView)
            {
                return;
            }

            selectedView.Selected = true;
            _selectedView = selectedView;

            if (updateCostume)
            {
                UpdateCostume(index);
            }
        }

        private void UpdateCostume(int index)
        {
            var player = Game.Game.instance.Stage.SelectedPlayer;
            if (player is null)
            {
                throw new NullReferenceException(nameof(player));
            }

            var currentSubType = _currentSubType;
            var colorData = costumeColorScriptableObject.colorSelectList
                .First(x => x.itemSubType == currentSubType).colorSelect;

            index = Math.Clamp(index, 0, colorData.Count - 1);
            if (_index[currentSubType] == index)
            {
                return;
            }

            _index[currentSubType] = index;
            var costumeIndex = _index[currentSubType];
            switch (currentSubType)
            {
                case ItemSubType.HairCostume:
                    player.UpdateHairByCustomizeIndex(costumeIndex);
                    break;
                case ItemSubType.EyeCostume:
                    player.UpdateEyeByCustomizeIndex(costumeIndex);
                    break;
                case ItemSubType.EarCostume:
                    player.UpdateEarByCustomizeIndex(costumeIndex);
                    break;
                case ItemSubType.TailCostume:
                    player.UpdateTailByCustomizeIndex(costumeIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentSubType), currentSubType, null);
            }
        }

        public void Show()
        {
            _index[ItemSubType.EarCostume] = 0;
            _index[ItemSubType.TailCostume] = 0;
            _index[ItemSubType.HairCostume] = 0;
            _index[ItemSubType.EyeCostume] = 0;

            var earIndex = _index[ItemSubType.EarCostume];
            var tailIndex = _index[ItemSubType.TailCostume];

            earToggle.isOn = true;

            var player = Game.Game.instance.Stage.SelectedPlayer;
            if (player is null)
            {
                throw new NullReferenceException(nameof(player));
            }

            player.UpdateEarByCustomizeIndex(earIndex);
            player.UpdateTailByCustomizeIndex(tailIndex);
        }

        public void SetActive(bool active)
        {
            costumePaletteContainer.SetActive(active);
            costumeTabsContainer.SetActive(active);

            if (active)
            {
                InitTab();
            }
        }

        public (int ear, int tail, int hair, int eye) GetCostumeId()
        {
            var earIndex = _index[ItemSubType.EarCostume];
            var tailIndex = _index[ItemSubType.TailCostume];
            var hairIndex = _index[ItemSubType.HairCostume];
            var eyeIndex = _index[ItemSubType.EyeCostume];

            return (earIndex, tailIndex, hairIndex, eyeIndex);
        }
    }
}
