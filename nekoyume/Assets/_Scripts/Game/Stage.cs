using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Stage : MonoBehaviour
    {
        public int Id;
        private GameObject _background = null;
        private ActionCamera _actionCam = null;
        private StageManager _stageManager = null;

        private void Awake()
        {
            Event.OnRoomEnter.AddListener(OnRoomEnter);
            Event.OnStageEnter.AddListener(OnStageEnter);
        }

        private void Start()
        {
            InitComponents();
            LoadBackground("nest");
        }

        private void InitComponents()
        {
            _actionCam = Camera.main.gameObject.GetComponent<ActionCamera>();
            _stageManager = gameObject.GetComponent<StageManager>();
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
