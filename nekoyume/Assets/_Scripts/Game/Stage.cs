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
        private ActionCamera _actionCam = null;
        private Model.Avatar _avatar = null;
        private GameObject _background = null;
        private GameObject _characters = null;
        private GameObject _stageManager = null;

        public UI.Blind Blind { get; private set; } = null;

        public UI.Move MoveWidget { get; private set; } = null;

        private void Awake()
        {
            Event.OnUpdateAvatar.AddListener(OnUpdateAvatar);
            Event.OnRoomEnter.AddListener(OnRoomEnter);
            Event.OnStageEnter.AddListener(OnStageEnter);
        }

        private void Start()
        {
            InitCamera();
            InitUI();
            LoadBackground("nest");
        }

        private void InitCamera()
        {
            _actionCam = Camera.main.gameObject.GetComponent<ActionCamera>();
            if (_actionCam == null)
            {
                _actionCam = Camera.main.gameObject.AddComponent<ActionCamera>();
            }
        }

        private void InitUI()
        {
            Blind = UI.Widget.Create<UI.Blind>();
            MoveWidget = UI.Widget.Create<UI.Move>();
            MoveWidget.Close();
        }

        private void OnUpdateAvatar(Model.Avatar avatar)
        {
            this._avatar = avatar;
        }

        private void OnRoomEnter()
        {
            StartCoroutine(RoomEntering());
        }

        private IEnumerator RoomEntering()
        {
            Id = 0;
            Blind.Show();
            Blind.FadeIn(1.0f);
            yield return new WaitForSeconds(1.0f);
            MoveWidget.Show();
            LoadBackground("room");
            LoadCharacter(_avatar);
            Blind.FadeOut(1.0f);
            yield return new WaitForSeconds(1.0f);
            Blind.gameObject.SetActive(false);
            Event.OnStageStart.Invoke();
        }

        private void OnStageEnter()
        {
            StartCoroutine(WorldEntering());
        }

        private IEnumerator WorldEntering()
        {
            if (_stageManager == null)
            {
                _stageManager = new GameObject("StageManager");
                _stageManager.transform.parent = transform;
                _stageManager.AddComponent<StageManager>();
            }

            var manager = _stageManager.GetComponent<StageManager>();
            StartCoroutine(manager.StartStage(_avatar.world_stage));
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

        private void LoadCharacter(Avatar a)
        {
            if (_characters == null)
            {
                _characters = new GameObject("characters");
                _characters.transform.parent = transform;
            }
            var go = Instantiate(Resources.Load<GameObject>("Prefab/Character"), _characters.transform);
            go.name = "Player";
            var character = go.GetComponent<Character>();
            character._Load(a);
        }
    }
}
