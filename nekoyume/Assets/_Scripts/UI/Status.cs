using System;
using Nekoyume.Move;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class Status : Widget
    {
        public Text TextLevelName;
        public Text TextStage;
        public Text TextExp;
        public Slider ExpBar;

        private int _pendingExp = 0;
        private int _currentExp = 0;
        private long _expMax = 0;

        private void Awake()
        {
            Game.Event.OnEnemyDead.AddListener(OnEnemyDead);
        }

        private void OnEnemyDead(Game.Character.Enemy enemy)
        {
            _pendingExp += enemy.RewardExp;
            UpdateExp();
        }

        public void UpdatePlayer(GameObject playerObj)
        {
            Show();

            _pendingExp = 0;

            Model.Avatar avatar = MoveManager.Instance.Avatar;
            Game.Character.Player player = playerObj.GetComponent<Game.Character.Player>();
            _currentExp = avatar.exp;
            _expMax = player.EXPMax;

            if (avatar != null)
            {
                TextLevelName.text = $"LV. {avatar.level} {avatar.name}";  
                TextStage.text = $"STAGE {avatar.world_stage}";
            }

            UpdateExp();
        }

        private void UpdateExp()
        {
            if (_pendingExp > 0)
            {
                TextExp.text = $"{_currentExp} / {_expMax} + ({_pendingExp})";
            }
            else
            {
                TextExp.text = $"{_currentExp} / {_expMax}";
            }
        }
    }
}
