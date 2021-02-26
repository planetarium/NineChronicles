using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Timer
{
    public class TimerView : MonoBehaviour
    {
        [SerializeField]
        protected TextMeshProUGUI timeText;

        private const double TimerInterval = 1.0f;
        private readonly WaitForSeconds WaitInterval = new WaitForSeconds((float) TimerInterval);

        private Coroutine _timerCoroutine = null;

        protected double _time;
        
        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Show(int seconds)
        {
            _time = seconds;
            PostShow();
        }
        
        public virtual void Show(int minutes, int seconds)
        {
            _time = minutes * 60 + seconds;
            PostShow();
        }
        
        public virtual void Show(int hours, int minutes, int seconds)
        {
            _time = hours * 60 * 60 + minutes * 60 + seconds;
            PostShow();
        }
        
        public virtual void Show(TimeSpan timeSpan)
        {
            _time = timeSpan.TotalSeconds;
            PostShow();
        }

        private void PostShow()
        {
            gameObject.SetActive(true);
            _timerCoroutine = StartCoroutine(CoUpdate());
        }

        public virtual void Close(bool stopTimer = true)
        {
            gameObject.SetActive(false);
            if (stopTimer && !(_timerCoroutine is null))
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }   
        }

        private IEnumerator CoUpdate()
        {
            while (true)
            {
                UpdateInternal();
                yield return WaitInterval;
            }
        }

        protected virtual void UpdateInternal()
        {
            UpdateTimeText();
            _time -= TimerInterval;
        }
        
        private void UpdateTimeText()
        {
            timeText.text = _time.ToString("N0");
        }
    }
}
