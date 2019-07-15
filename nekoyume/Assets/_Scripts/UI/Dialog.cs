using System.Collections;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Dialog : Widget
    {
        public float textInterval = 0.06f;
        public string itemColor = "blue";

        public Text txtContainer;
        public Text txtName;
        public Text txtDialog;
        public Image imgCharacter;

        private string _playerPrefsKey;
        private string _dialogKey;
        private int _dialogIndex;
        private int _dialogNum;
        private int _characterId;
        private Coroutine _coroutine = null;
        private string _text;

        public void Show(int dialogId)
        {
            var addr = States.Instance.currentAvatarState.Value.address.ToString();
            _playerPrefsKey = $"DIALOG_{addr}_{dialogId}";
            if (PlayerPrefs.GetInt(_playerPrefsKey, 0) > 0)
                return;

            base.Show();

            _dialogKey = $"DIALOG_{dialogId}_{1}_";
            _dialogIndex = 0;
            _dialogNum = LocalizationManager.LocalizedCount(_dialogKey);
            
            _coroutine = StartCoroutine(CoShowText());
        }

        public void Skip()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
                txtDialog.text = _text;
                return;
            }
            _dialogIndex++;
            if (_dialogIndex == _dialogNum)
            {
                PlayerPrefs.SetInt(_playerPrefsKey, 1);
                Close();
                return;
            }
            _coroutine = StartCoroutine(CoShowText());
        }

        public IEnumerator CoShowText()
        {
            string text = LocalizationManager.Localize($"{_dialogKey}{_dialogIndex}");
            if (string.IsNullOrEmpty(text))
                yield break;

            _text = ParseText(text);

            if (Data.Tables.instance.Character.TryGetValue(_characterId, out var characterData))
            {
                var res = Resources.Load<Sprite>($"Images/character_{characterData.characterResource}");
                imgCharacter.sprite = res;
                imgCharacter.enabled = imgCharacter.sprite != null;
                txtContainer.text = characterData.characterName;
                txtName.text = characterData.characterName;
            }

            bool skipTag =  false;
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

                yield return new WaitForSeconds(textInterval);
            }
            _coroutine = null;
        }

        private string ParseText(string text)
        {
            int open = 0;
            for (int i = 0; i < text.Length; ++i)
            {
                var s = text[i];
                switch (s)
                {
                    case '[':
                        open = i;
                        break;
                    case ']':
                        string left = text.Substring(0, open);
                        string right = text.Substring(i + 1);
                        string[] pair = text.Substring(open + 1, i - open - 1).Split(':');
                        int pairValue = int.Parse(pair[1]);
                        if (pair[0] == "character")
                            _characterId = pairValue;
                        else if (pair[0] == "item")
                        {
                            if (Data.Tables.instance.Item.TryGetValue(pairValue, out var itemData))
                            {
                                left = $"{left}<color={itemColor}>{itemData.name}</color>";
                            }
                        }
                        text = $"{left}{right}";
                        break;
                }
            }
            return text;
        }
    }
}
