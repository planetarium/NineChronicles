using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.Game
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Animator))]
    public class ButtonAnimationEventListener : MonoBehaviour
    {
        private Button _button = null;
        private Animator _animator = null;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _animator = GetComponent<Animator>();
            _button.onClick.AddListener(SubscribeOnClick);
        }

        public Dictionary<string, System.Action> CallbackMap { get; private set; }
            = new Dictionary<string, System.Action>();

        public bool TryAddCallback(string key, System.Action action)
        {
            if (CallbackMap.ContainsKey(key))
            {
                return false;
            }

            CallbackMap[key] = action;
            return true;
        }

        public void InvokeCallback(string key)
        {
            if (!CallbackMap.ContainsKey(key))
            {
                return;
            }

            CallbackMap[key].Invoke();
        }

        protected virtual void SubscribeOnClick()
        {

        }
    }
}
