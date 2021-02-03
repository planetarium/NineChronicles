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

#if UNITY_EDITOR
        // FOR TEST ------------------------------------------------------------------------------------------------------ //
        public RectTransform test1;
        public RectTransform test2;

          private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                var list = new List<ITutorialData>()
                {
                    new GuideBackgroundData(true, true, test1),
                    new GuideArrowData(GuideType.Square, test1, false),
                    new GuideDialogData(DialogEmojiType.Idle, DialogCommaType.Next,
                        "bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b> bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>test</b>bold <b>text</b> test <b>bold</b> text <b>",
                        test1.anchoredPosition.y, button)
                };
                Play(list);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                var objectPosition = new Vector2(0, -100);
                var list = new List<ITutorialData>()
                {
                    new GuideBackgroundData(false, true, test1),
                    new GuideArrowData(GuideType.Circle, test1, true),
                    new GuideDialogData(DialogEmojiType.Question, DialogCommaType.End,
                        "Hello! My name is... <delay=0.5>NPC</delay>. Got it, <i>bub</i>?",
                        test1.anchoredPosition.y, button)
                };
                Play(list);
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                var list = new List<ITutorialData>()
                {
                    new GuideBackgroundData(false, true, test2),
                    new GuideArrowData(GuideType.None, test2, false),
                    new GuideDialogData(DialogEmojiType.Reaction, DialogCommaType.Next,
                        "You can <color=#ff0000ff>color</color> tag <color=#00ff00ff>like this</color>.",
                        test2.anchoredPosition.y, button)
                };
                Play(list);
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                var list = new List<ITutorialData>()
                {
                    new GuideBackgroundData(false, true, test2),
                    new GuideArrowData(GuideType.Outline, test2, false),
                    new GuideDialogData(DialogEmojiType.Reaction, DialogCommaType.End,
                        "You can <color=#ff0000ff>color</color> tag <color=#00ff00ff>like this</color>.",
                        test2.anchoredPosition.y, button)
                };
                Play(list);
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                var list = new List<ITutorialData>()
                {
                    new GuideBackgroundData(false, true, test1),
                    new GuideArrowData(GuideType.Outline, test1, false),
                    new GuideDialogData(DialogEmojiType.Reaction, DialogCommaType.Next,
                        "You can <color=#ff0000ff>color</color> tag <color=#00ff00ff>like this</color>.",
                        test1.anchoredPosition.y, button)
                };
                Play(list);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                Stop();
            }
        }
          // ------------------------------------------------------------------------------------------------------ //
#endif

        public void Play(List<ITutorialData> datas, System.Action callback = null)
        {
            if (_isPlaying)
            {
                return;
            }
            Debug.Log("Play");

            _finishRef = 0;
            _isPlaying = true;
            button.onClick.RemoveAllListeners();

            foreach (var data in datas)
            {
                var item = items.FirstOrDefault(x => data.Type == x.Type);
                item?.Item.gameObject.SetActive(true);
                item?.Item.Play(data, () =>
                {
                    PlayEnd(callback);
                });
            }
        }

        public void Stop()
        {
            foreach (var item in items)
            {
                item.Item.Stop();
            }
        }

        private void PlayEnd(System.Action callback)
        {
            _finishRef += 1;
            if (_finishRef >= ItemCount)
            {
                _isPlaying = false;
                button.onClick.RemoveAllListeners();
                callback?.Invoke();
            }
        }
    }

    [Serializable]
    public class ItemContainer
    {
        [SerializeField] private TutorialItemType type;
        [SerializeField] private TutorialItem item;

        public TutorialItemType Type => type;
        public TutorialItem Item => item;
    }
}
