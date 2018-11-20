using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Avatar = Nekoyume.Model.Avatar;

namespace Nekoyume.Game
{
    public enum StageType
    {
        Room,
        World,
    }

    public class Stage : MonoBehaviour
    {
        internal int Id;
        private GameObject _background = null;
        private ActionCamera _actionCam = null;
        private StageManager _stageManager = null;
        private Game _game;

        private void Awake()
        {
            Event.OnUpdateAvatar.AddListener(OnUpdateAvatar);
            Event.OnRoomEnter.AddListener(OnRoomEnter);
            Event.OnStageEnter.AddListener(OnStageEnter);
            _stageManager = gameObject.AddComponent<StageManager>();
            _game = this.GetRootComponent<Game>();
        }

        private void Start()
        {
            InitComponents();
            LoadBackground("nest");
        }

        private void InitComponents()
        {
            _actionCam = Camera.main.gameObject.GetComponent<ActionCamera>();
        }

        private void OnUpdateAvatar(Model.Avatar avatar)
        {
            _game.Avatar = avatar;
        }

        private void OnRoomEnter()
        {
            StartCoroutine(_stageManager.RoomEntering(this));
        }

        public void OnStageEnter()
        {
            StartCoroutine(_stageManager.WorldEntering(this));
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
            var camPosition = _actionCam.transform.position;
            camPosition.x = 0;
            _actionCam.transform.position = camPosition;
        }
    }
}