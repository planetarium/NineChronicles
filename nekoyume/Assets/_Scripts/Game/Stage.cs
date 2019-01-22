using System.Collections;
using DG.Tweening;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Character;
using Nekoyume.Game.Entrance;
using Nekoyume.Game.Factory;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Stage : MonoBehaviour
    {
        private GameObject _background;
        public int Id;

        public string BackgroundName
        {
            get
            {
                if (_background == null)
                    return "";

                return _background.name;
            }
        }

        private void Awake()
        {
            Event.OnRoomEnter.AddListener(OnRoomEnter);
            Event.OnStageEnter.AddListener(OnStageEnter);
            Event.OnPlayerDead.AddListener(OnPlayerDead);
            Event.OnPlayerDead.AddListener(OnPlayerSleep);
            Event.OnPlayerSleep.AddListener(OnPlayerSleep);
        }

        private IEnumerator Start()
        {
            LoadBackground("nest");

            yield return new WaitForEndOfFrame();
            var playerFactory = GetComponent<PlayerFactory>();
            var player = playerFactory.Create();
            if (player != null)
                player.transform.position = new Vector2(-0.8f, 0.46f);
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
            ActionManager.Instance.HackAndSlash(player, Id);
        }

        public void LoadBackground(string prefabName, float fadeTime = 0.0f)
        {
            if (_background != null)
            {
                if (_background.name.Equals(prefabName)) return;
                if (fadeTime > 0.0f)
                {
                    var sprites = _background.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var sprite in sprites)
                    {
                        sprite.sortingOrder += 1;
                        sprite.DOFade(0.0f, fadeTime);
                    }
                }

                Destroy(_background, fadeTime);
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
            StartCoroutine(SleepAsync());
        }

        private IEnumerator SleepAsync()
        {
            var tables = this.GetRootComponent<Tables>();
            Stats statsData;
            if (tables.Stats.TryGetValue(ActionManager.Instance.Avatar.Level, out statsData))
            {
                ActionManager.Instance.Sleep(statsData);
                while (ActionManager.Instance.Avatar.CurrentHP == ActionManager.Instance.Avatar.HPMax)
                    yield return new WaitForSeconds(1.0f);
            }

            OnRoomEnter();
        }
    }
}
