using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        private UI.Blind blind = null;
        private UI.Move moveWidget = null;
        private Model.Avatar avatar = null;
        private GameObject background = null;
        private ActionCamera actionCam = null;
        private ObjectPool objectPool = null;
        private MonsterSpawner monsterSpawner = null;


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
            actionCam = Camera.main.gameObject.GetComponent<ActionCamera>();
            objectPool = GetComponent<ObjectPool>();
            monsterSpawner = GetComponent<MonsterSpawner>();
        }

        private void InitUI()
        {
            blind = UI.Widget.Create<UI.Blind>();
            moveWidget = UI.Widget.Create<UI.Move>();
            moveWidget.Close();
        }

        private void OnUpdateAvatar(Model.Avatar avatar)
        {
            this.avatar = avatar;
        }

        private void OnRoomEnter()
        {
            StartCoroutine(RoomEntering());
        }

        private IEnumerator RoomEntering()
        {
            Id = 0;
            blind.Show();
            blind.FadeIn(1.0f);
            yield return new WaitForSeconds(1.0f);
            moveWidget.Show();
            LoadBackground("room");

            var character = objectPool.Get<Character>();
            character._Load(character.gameObject, avatar.class_);

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
            Data.Table.Stage data;
            var tables = this.GetRootComponent<Data.Tables>();
            if (tables.Stage.TryGetValue(avatar.world_stage, out data))
            {
                Id = avatar.world_stage;

                blind.Show();
                blind.FadeIn(1.0f);
                yield return new WaitForSeconds(1.0f);

                moveWidget.Show();
                objectPool.ReleaseAll();
                LoadBackground(data.Background);

                var character = objectPool.Get<Character>();
                character._Load(character.gameObject, avatar.class_);

                blind.FadeOut(1.0f);
                yield return new WaitForSeconds(1.0f);
                blind.gameObject.SetActive(false);
                Event.OnStageStart.Invoke();

                monsterSpawner.Play(Id, data.MonsterPower);
            }
        }

        private void LoadBackground(string prefabName)
        {
            if (background != null)
            {
                if (background.name.Equals(prefabName))
                {
                    return;
                }
                Destroy(background);
                background = null;
            }
            var resName = $"Prefab/Background/{prefabName}";
            var prefab = Resources.Load<GameObject>(resName);
            if (prefab != null)
            {
                background = Instantiate(prefab, transform);
                background.name = prefabName;
            }
            var camPosition = actionCam.transform.position;
            camPosition.x = 0;
            actionCam.transform.position = camPosition;
        }
    }
}
