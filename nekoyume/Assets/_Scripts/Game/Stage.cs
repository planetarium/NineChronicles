using Nekoyume.Game.Character;
using Nekoyume.Game.Entrance;
using Nekoyume.Move;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Stage : MonoBehaviour
    {
        public int Id = 0;
        private GameObject _background = null;

        private void Awake()
        {
            Event.OnRoomEnter.AddListener(OnRoomEnter);
            Event.OnStageEnter.AddListener(OnStageEnter);
            Event.OnPlayerDead.AddListener(OnPlayerDead);
            Event.OnPlayerSleep.AddListener(OnPlayerSleep);
        }

        private void Start()
        {
            LoadBackground("nest");
        }

        private void OnRoomEnter()
        {
            gameObject.AddComponent<RoomEntering>();
        }

        private void OnStageEnter()
        {
            gameObject.AddComponent<WorldEntering>();
        }

        private void OnPlayerDead()
        {
            var player = GetComponentInChildren<Player>();
            MoveManager.Instance.HackAndSlash(player, Id);
            player.gameObject.SetActive(false);
        }

        public void LoadBackground(string prefabName)
        {
            if (_background != null)
            {
                if (_background.name.Equals(prefabName))
                {
                    return;
                }
                Destroy(_background);
                _background = null;
            }
            var resName = $"Prefab/Background/{prefabName}";
            var prefab = Resources.Load<GameObject>(resName);
            if (prefab != null)
            {
                _background = Instantiate(prefab, transform);
                _background.name = prefabName;
            }
        }

        private void OnPlayerSleep()
        {
            var tables = this.GetRootComponent<Data.Tables>();
            Data.Table.Stats statsData;
            if (tables.Stats.TryGetValue(MoveManager.Instance.Avatar.level, out statsData))
            {
                MoveManager.Instance.Sleep(statsData);
            }
            OnRoomEnter();
        }
    }
}
