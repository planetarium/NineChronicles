using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Tutorial : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private List<ItemContainer> items;
        private const int ItemCount = 3;
        private int _finishRef;
        private bool _isPlaying;

        public Button NextButton => button;

        public void Play(List<ITutorialData> datas, System.Action callback = null)
        {
            if (!Init())
            {
                return;
            }

            button.onClick.RemoveAllListeners();

            foreach (var data in datas)
            {
                var item = items.FirstOrDefault(x => data.Type == x.Type);
                item?.Item.gameObject.SetActive(true);
                item?.Item.Play(data, () => { PlayEnd(callback); });
            }
        }

        public void Stop(System.Action callback = null)
        {
            if (!Init())
            {
                return;
            }

            foreach (var item in items)
            {
                item.Item.Stop(() => { PlayEnd(callback); });
            }
        }

        private bool Init()
        {
            if (_isPlaying)
            {
                return false;
            }

            _finishRef = 0;
            _isPlaying = true;
            return true;
        }

        private void PlayEnd(System.Action callback)
        {
            _finishRef += 1;
            // Debug.Log($"[PlayEnd] Ref : {_finishRef}");
            if (_finishRef >= ItemCount)
            {
                _isPlaying = false;
                callback?.Invoke();
                // Debug.Log("[PlayEnd] finish");
            }
        }
    }

    [Serializable]
    public class ItemContainer
    {
        [SerializeField] private TutorialIemType type;
        [SerializeField] private TutorialItem item;

        public TutorialIemType Type => type;
        public TutorialItem Item => item;
    }
}
