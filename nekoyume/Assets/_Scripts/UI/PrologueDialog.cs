using System.Collections;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class PrologueDialog : Widget
    {
        private const float TextInterval = 0.06f;
        private const int FenrirId = 205007;
        private const int FreyaId = 300005;

        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtDialog;
        public RectTransform fenrirPosition;
        public RectTransform freyaPosition;
        private DialogNPC _fenrir;
        private DialogNPC _freya;

        private string _dialogKey;
        private int _dialogIndex;
        private int _dialogNum;
        private string _npc;
        private Coroutine _coroutine = null;
        private string _text;
        private int _callCount = 1;
        protected override WidgetType WidgetType => WidgetType.Popup;

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
            if (_fenrir)
            {
                _fenrir.gameObject.SetActive(false);
                _fenrir = null;
            }
            if (_freya)
            {
                _freya.gameObject.SetActive(false);
                _freya = null;
            }

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

        public IEnumerator CoShowText()
        {
            var text = L10nManager.Localize($"{_dialogKey}{_dialogIndex}");
            if (string.IsNullOrEmpty(text))
                yield break;

            _npc = null;
            _text = ParseText(text);

            // TODO: npc
            if (!string.IsNullOrEmpty(_npc))
            {
                if (_npc == "11")
                {
                    if (_fenrir is null)
                    {
                        var go = Game.Game.instance.Stage.npcFactory.CreateDialogNPC(
                            FenrirId,
                            fenrirPosition.position,
                            LayerType.UI,
                            100);
                        _fenrir = go.GetComponent<DialogNPC>();
                    }

                    _freya?.SpineController.Disappear(0.3f);
                    _fenrir.SpineController.Appear(0.3f);
                }
                else
                {
                    if (_freya is null)
                    {
                        var go = Game.Game.instance.Stage.npcFactory.CreateDialogNPC(
                            FreyaId,
                            freyaPosition.position,
                            LayerType.UI,
                            100);
                        _freya = go.GetComponent<DialogNPC>();
                    }

                    _freya.SpineController.Appear(0.3f);
                    _fenrir?.SpineController.Disappear(0.3f);
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

            bool skipTag = false;
            bool tagClosed = true;
            for (int textIndex = 1; textIndex <= _text.Length; ++textIndex)
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
                        string left = text.Substring(0, openIndex);
                        string right = text.Substring(i + 1);
                        string[] pair = text.Substring(openIndex + 1, i - openIndex - 1).Split(':');
                        string pairKey = pair[0].ToLower();
                        int.TryParse(pair[1], out int pairValue);
                        switch (pairKey)
                        {
                            case "npc":
                                _npc = pair[1];
                                break;
                        }

                        text = $"{left}{right}";
                        i = left.Length - 1;

                        break;
                }
            }

            return text;
        }
    }
}
