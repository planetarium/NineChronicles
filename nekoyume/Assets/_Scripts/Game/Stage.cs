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
        public int Id { get; private set; }
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
            StartCoroutine(RoomEntering());
        }

        private IEnumerator RoomEntering()
        {
            var blind = _game.Blind;
            var moveWidget = _game.MoveWidget;
            Id = 0;
            blind.Show();
            blind.FadeIn(1.0f);
            yield return new WaitForSeconds(1.0f);
            moveWidget.Show();
            LoadBackground("room");
            var character = _stageManager.ObjectPool.Get<Character>();
            character._Load(_game.Avatar);
            blind.FadeOut(1.0f);
            yield return new WaitForSeconds(1.0f);
            blind.gameObject.SetActive(false);
            Event.OnStageStart.Invoke();
        }

        private void OnStageEnter()
        {
            StartCoroutine(WorldEntering());
        }

        private IEnumerator WorldEntering()
        {
            StartCoroutine(_stageManager.StartStage(_game.Avatar));
            Event.OnStageStart.Invoke();
            yield return null;
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
