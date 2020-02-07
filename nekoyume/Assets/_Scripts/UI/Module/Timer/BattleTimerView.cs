namespace Nekoyume.UI.Module.Timer
{
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
            var text = string.Format(TimerFormat, time, _timeLimit);
            timeText.text = text;
        }
    }
}
