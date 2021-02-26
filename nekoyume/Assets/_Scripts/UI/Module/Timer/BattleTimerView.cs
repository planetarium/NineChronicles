using System;

namespace Nekoyume.UI.Module.Timer
{
    // FIXME: `TimerView`와의 관계에 개선이 필요해 보인다.
    public class BattleTimerView : TimerView
    {
        private const string TimerFormat = "{0:N0}/{1:N0}";
        protected double _timeLimit;

        protected virtual void Awake()
        {
            Game.Event.OnPlayerTurnEnd.AddListener(SetTimer);
        }

        public override void Show()
        {
            SetTimer(0);
            base.Show();
        }

        public virtual void Show(double timeLimit)
        {
            _timeLimit = timeLimit;
            SetTimer(0);
            base.Show();
        }

        protected virtual void SetTimer(int time)
        {
            time = (int) Math.Min(time, _timeLimit);
            var text = string.Format(TimerFormat, time, _timeLimit);
            timeText.text = text;
        }
    }
}
