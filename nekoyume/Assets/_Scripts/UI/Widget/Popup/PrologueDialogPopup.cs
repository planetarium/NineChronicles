using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class PrologueDialogPopup : PopupWidget
    {
        private const float TextInterval = 0.06f;

        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtDialog;

        private string _dialogKey;
        private int _dialogIndex;
        private int _dialogNum;
        private string _npc;
        private Coroutine _coroutine = null;
        private string _text;
        private int _callCount = 1;

        [SerializeField]
        private SkeletonGraphic freya;

        [SerializeField]
        private SkeletonGraphic fenrir;

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = null;
            SubmitWidget = Skip;
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            _dialogKey = $"DIALOG_0_{_callCount}_";
            _dialogIndex = 0;
            _dialogNum = L10nManager.LocalizedCount(_dialogKey);
            _coroutine = StartCoroutine(CoShowText());
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            freya.DOFade(0, 0);
            fenrir.DOFade(0, 0);
            base.Close(ignoreCloseAnimation);
            _callCount++;
        }

        public void Skip()
        {
            if (!CanHandleInputEvent)
            {
                return;
            }

            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
                txtDialog.text = _text;
                return;
            }

            _dialogIndex++;

            if (_dialogIndex >= _dialogNum)
            {
                Close();
                return;
            }

            _coroutine = StartCoroutine(CoShowText());
        }

        private IEnumerator CoShowText()
        {
            var text = L10nManager.Localize($"{_dialogKey}{_dialogIndex}");
            if (string.IsNullOrEmpty(text))
                yield break;

            _npc = null;
            _text = ParseText(text);

            if (!string.IsNullOrEmpty(_npc))
            {
                if (_npc == "11")
                {
                    freya.DOFade(0, 0.3f);
                    fenrir.DOFade(1, 0.3f);
                }
                else
                {
                    freya.DOFade(1, 0.3f);
                    fenrir.DOFade(0, 0.3f);
                }
                string localizedName;
                try
                {
                    localizedName = L10nManager.Localize($"NPC_{_npc}_NAME");
                }
                catch (KeyNotFoundException)
                {
                    localizedName = "???";
                }
                txtName.text = localizedName;
            }

            var skipTag = false;
            var tagClosed = true;
            for (var textIndex = 1; textIndex <= _text.Length; ++textIndex)
            {
                if (_text.Length > textIndex)
                {
                    if (_text[textIndex] == '<')
                    {
                        skipTag = true;
                        tagClosed = false;
                    }
                    else if (skipTag && _text[textIndex] == '>')
                    {
                        skipTag = false;
                        continue;
                    }

                    if (!tagClosed && _text[textIndex] == '/')
                        tagClosed = true;
                }

                if (skipTag)
                    continue;

                if (tagClosed)
                    txtDialog.text = $"{_text.Substring(0, textIndex)}";
                else
                    txtDialog.text = $"{_text.Substring(0, textIndex)}</color>";

                AudioController.instance.PlaySfx(AudioController.SfxCode.Typing, 0.1f);

                yield return new WaitForSeconds(TextInterval);
            }

            _coroutine = null;
        }

        private string ParseText(string text)
        {
            var opened = false;
            var openIndex = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var s = text[i];
                switch (s)
                {
                    case '[':
                        if (opened)
                        {
                            continue;
                        }

                        opened = true;
                        openIndex = i;
                        break;
                    case ']':
                        if (!opened)
                        {
                            continue;
                        }

                        opened = false;
                        var left = text.Substring(0, openIndex);
                        var right = text.Substring(i + 1);
                        var pair = text.Substring(openIndex + 1, i - openIndex - 1)
                            .Split(':', '：')
                            .Select(value => value.Trim())
                            .ToArray();
                        var pairKey = pair[0].ToLower();
                        int.TryParse(pair[1], out _);
                        _npc = pairKey switch
                        {
                            "npc" => pair[1],
                            _ => _npc
                        };

                        text = $"{left}{right}";
                        i = left.Length - 1;

                        break;
                }
            }

            return text;
        }
    }
}
