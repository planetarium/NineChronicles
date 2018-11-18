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
        private UI.Blind _blind = null;
        private UI.Move _moveWidget = null;
        private Model.Avatar _avatar = null;
        private GameObject _background = null;
        private ActionCamera _actionCam = null;
        private ObjectPool _objectPool = null;
        private MonsterSpawner _monsterSpawner = null;


        private void Awake()
        {
            Event.OnUpdateAvatar.AddListener(OnUpdateAvatar);
            Event.OnRoomEnter.AddListener(OnRoomEnter);
            Event.OnStageEnter.AddListener(OnStageEnter);
        }

        private void Start()
        {
            InitComponents();
            InitUI();
            LoadBackground("nest");
        }

        private void InitComponents()
        {
            _actionCam = Camera.main.gameObject.GetComponent<ActionCamera>();
            _objectPool = GetComponent<ObjectPool>();
            _monsterSpawner = GetComponent<MonsterSpawner>();
        }

        private void InitUI()
        {
            _blind = UI.Widget.Create<UI.Blind>();
            _moveWidget = UI.Widget.Create<UI.Move>();
            _moveWidget.Close();
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
            _blind.Show();
            _blind.FadeIn(1.0f);
            yield return new WaitForSeconds(1.0f);
            _moveWidget.Show();
            LoadBackground("room");

            var character = _objectPool.Get<Character>();
            character._Load(_avatar);

            _blind.FadeOut(1.0f);
            yield return new WaitForSeconds(1.0f);
            _blind.gameObject.SetActive(false);
            Event.OnStageStart.Invoke();
        }

        private void OnStageEnter()
        {
            StartCoroutine(WorldEntering());
        }

        private IEnumerator WorldEntering()
        {
            Data.Table.Stage data;
            var tables = this.GetRootComponent<Data.Tables>();
            if (tables.Stage.TryGetValue(_avatar.world_stage, out data))
            {
                Id = _avatar.world_stage;

                _blind.Show();
                _blind.FadeIn(1.0f);
                yield return new WaitForSeconds(1.0f);

                _moveWidget.Show();
                _objectPool.ReleaseAll();
                LoadBackground(data.Background);

                var character = _objectPool.Get<Character>();
                character._Load(_avatar);

                _blind.FadeOut(1.0f);
                yield return new WaitForSeconds(1.0f);
                _blind.gameObject.SetActive(false);
                Event.OnStageStart.Invoke();

                _monsterSpawner.Play(Id, data.MonsterPower);
            }
        }

        private void LoadBackground(string prefabName)
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
