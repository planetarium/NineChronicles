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
        private ActionCamera actionCam = null;
        private UI.Blind blind = null;
        private UI.Move moveWidget = null;
        private GameObject background = null;
        private GameObject characters = null;


        private void Awake()
        {
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
            actionCam = Camera.main.gameObject.GetComponent<ActionCamera>();
            if (actionCam == null)
            {
                actionCam = Camera.main.gameObject.AddComponent<ActionCamera>();
            }
        }

        private void InitUI()
        {
            blind = UI.Widget.Create<UI.Blind>();
            moveWidget = UI.Widget.Create<UI.Move>();
            moveWidget.Close();
        }

        private void OnRoomEnter(Model.Avatar avatar)
        {
            LoadBackground("room");
            LoadCharacter(avatar.class_);
        }

        private void OnStageEnter(Model.Avatar avatar)
        {
            string stageType = PlayerPrefs.GetString(nameof(StageType), StageType.Room.ToString());
            string method = $"{stageType}Entering";
            StartCoroutine(method, avatar);
        }

        private IEnumerator RoomEntering(Model.Avatar avatar)
        {
            PlayerPrefs.SetString(nameof(StageType), StageType.Room.ToString());
            Id = 0;
            blind.Show();
            blind.FadeIn(1.0f);
            yield return new WaitForSeconds(1.0f);
            moveWidget.Show();
            LoadBackground("room");
            LoadCharacter(avatar.class_);
            blind.FadeOut(1.0f);
            yield return new WaitForSeconds(1.0f);
            blind.Close();
            Event.OnStageStart.Invoke();
        }

        private IEnumerator WorldEntering(Model.Avatar avatar)
        {
            Data.Table.Stage data;
            var tables = this.GetRootComponent<Data.Tables>();
            if (tables.Stage.TryGetValue(avatar.main_stage, out data))
            {
                PlayerPrefs.SetString(nameof(StageType), StageType.World.ToString());
                Id = avatar.main_stage;
                blind.FadeIn(1.0f);
                yield return new WaitForSeconds(1.0f);
                moveWidget.Show();
                LoadBackground(data.Background);
                // TODO: Load characters
                blind.FadeOut(1.0f);
                yield return new WaitForSeconds(1.0f);
                Event.OnStageStart.Invoke();
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

        private void LoadCharacter(string avatarClass)
        {
            if (characters == null)
            {
                characters = new GameObject("characters");
                characters.transform.parent = transform;
            }
            var go = Instantiate(Resources.Load<GameObject>("Prefab/Character"), characters.transform);
            var character = go.GetComponent<Character>();
            character._Load(go, avatarClass);
        }

        private void LoadMonster(int monsterCode)
        {
            if (characters == null)
            {
                characters = new GameObject("characters");
                characters.transform.parent = transform;
            }
            var go = Instantiate(Resources.Load<GameObject>("Prefab/Character"), characters.transform);
            var character = go.GetComponent<Character>();
            character._Load(go, monsterCode.ToString());
        }
    }
}
