using System;
using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Nekoyume.UI.Model
{
    using UniRx;

    public class RuneListItemView : MonoBehaviour
    {
        [SerializeField]
        private GameObject rune;

        [SerializeField]
        private GameObject locked;

        [SerializeField]
        private GameObject equipped;

        [SerializeField]
        private GameObject notification;

        [SerializeField]
        private GameObject loading;

        [SerializeField]
        private GameObject select;

        [SerializeField]
        private Image runeImage;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private Button button;

        private readonly List<IDisposable> _disposables = new();

        public void Reset()
        {
            locked.SetActive(true);
            rune.SetActive(false);
            equipped.SetActive(false);
            notification.SetActive(false);
            loading.SetActive(false);
            select.SetActive(false);
            levelText.text = string.Empty;
        }

        public void Set(RuneItem item, RuneListScroll.ContextModel context)
        {
            rune.SetActive(item.Level > 0);
            locked.SetActive(item.Level == 0);

            if (RuneFrontHelper.TryGetRuneIcon(item.Row.Id, out var icon))
            {
                runeImage.sprite = icon;
            }
            levelText.text = item.Level.ToString();

            var hasNotification = item.HasNotification;
            if (item.IsMaxLevel)
            {
                hasNotification = false;
            }
            notification.SetActive(hasNotification);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => context.OnClick.OnNext(item));

            _disposables.DisposeAllAndClear();
            item.IsSelected.Subscribe(b => select.SetActive(b)).AddTo(_disposables);
            item.IsSelected.Value = false;
        }
    }
}
