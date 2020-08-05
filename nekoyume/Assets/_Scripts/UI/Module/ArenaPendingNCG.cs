using System;
using System.Collections.Generic;
using System.Globalization;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class ArenaPendingNCG : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI arenaText = null;

        [SerializeField]
        private TextMeshProUGUI ncgText = null;

        private readonly List<IDisposable> _disposablesAtOnDisable = new List<IDisposable>();

        private void Awake()
        {
            arenaText.text = L10nManager.Localize("UI_ARENA_FOUNDATION");
        }

        private void OnDisable()
        {
            _disposablesAtOnDisable.DisposeAllAndClear();
        }

        public void Show(bool subscribeSubject = true)
        {
            Show(States.Instance.WeeklyArenaState, subscribeSubject);
        }

        public void Show(WeeklyArenaState weeklyArenaState, bool subscribeSubject = false)
        {
            gameObject.SetActive(true);
            SetNCGText(weeklyArenaState);

            if (!subscribeSubject)
                return;

            WeeklyArenaStateSubject.WeeklyArenaState.Subscribe(SetNCGText)
                .AddTo(_disposablesAtOnDisable);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetNCGText(WeeklyArenaState weeklyArenaState)
        {
            ncgText.text = weeklyArenaState is null
                ? "NULL"
                : weeklyArenaState.Gold.ToString(CultureInfo.InvariantCulture);
        }
    }
}
