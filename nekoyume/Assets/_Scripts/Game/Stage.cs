using System.Collections.Generic;
using Nekoyume.Game.Entrance;
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
        }

        private void Start()
        {
            LoadBackground("nest");
        }

        private void OnRoomEnter()
        {
            gameObject.AddComponent<RoomEntering>();
        }

        public void OnStageEnter()
        {
            gameObject.AddComponent<WorldEntering>();
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
    }
}
