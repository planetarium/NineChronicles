using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class WorldBossTickets : MonoBehaviour
    {
        [SerializeField]
        private Image iconImage;

        public Image IconImage => iconImage;

        [SerializeField]
        private Slider slider;

        [SerializeField]
        private TextMeshProUGUI fillText;

        [SerializeField]
        private TextMeshProUGUI timespanText;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }
    }
}
