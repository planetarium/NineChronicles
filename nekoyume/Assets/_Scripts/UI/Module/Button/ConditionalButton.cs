using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(Button))]
    public class ConditionalButton : MonoBehaviour
    {
        [SerializeField]
        private GameObject normalObject = null;

        [SerializeField]
        private GameObject conditionalObject = null;

        [SerializeField]
        private GameObject disabledObject = null;

        private Button _button = null;

        private Func<bool> _conditionFunc = null;

        public bool Interactable
        {
            get => _button.interactable;
            set
            {
                _button.interactable = value;
                UpdateObjects();
            }
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            UpdateObjects();
        }

        public void SetCondition(Func<bool> conditionFunc)
        {
            _conditionFunc = conditionFunc;
            UpdateObjects();
        }

        public void UpdateObjects()
        {
            if (Interactable)
            {
                var condition = _conditionFunc?.Invoke() ?? false;
                normalObject.SetActive(condition);
                conditionalObject.SetActive(!condition);
            }
            else
            {
                normalObject.SetActive(false);
                conditionalObject.SetActive(false);
            }
            disabledObject.SetActive(!Interactable);
        }
    }
}
