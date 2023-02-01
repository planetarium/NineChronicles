using System;
using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.L10n;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class RuneStoneItem : MonoBehaviour
    {
        [SerializeField]
        private Image icon;

        [SerializeField]
        private TextMeshProUGUI countText;

        [SerializeField]
        private GameObject dimmed;

        [SerializeField]
        private TouchHandler touchHandler;

        [SerializeField]
        private bool showMessage = true;

        private readonly List<IDisposable> _disposables = new();

        public void Set(RuneScriptableObject.RuneData data, int count)
        {
            _disposables.DisposeAllAndClear();

            icon.sprite = data.icon;
            countText.text = $"{count:#,0}";
            dimmed.SetActive(count <= 0);
            touchHandler.OnClick
                .Subscribe(_ =>
                {
                    if (showMessage)
                    {
                        ShowMaterialNavigatorPopup(data);
                    }
                })
                .AddTo(_disposables);
        }

        private void ShowMaterialNavigatorPopup(RuneScriptableObject.RuneData data)
        {
            var popup = Widget.Find<MaterialNavigationPopup>();
            string name, count, content, buttonText;
            System.Action callback;
            name = L10nManager.Localize($"ITEM_NAME_{data.id}");
            count = States.Instance.RuneStoneBalance[data.id].GetQuantityString();
            content = L10nManager.Localize($"ITEM_DESCRIPTION_{data.id}");
            buttonText = L10nManager.Localize("UI_RUNE");
            callback = () =>
            {
                var rune = Widget.Find<Rune>();
                rune.CloseWithOtherWidgets();
                rune.Show(data.id, true);
            };

            popup.Show(callback, data.icon, name, count, content, buttonText);
        }
    }
}
