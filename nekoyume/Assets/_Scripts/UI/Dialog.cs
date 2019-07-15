using System.Collections;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.Helper;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Dialog : Widget
    {
        public float textInterval = 0.06f;
        public Color itemTextColor;

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
        private string _itemTextColor;

        public static string GetPlayerPrefsKeyOfCurrentAvatarState(int dialogId)
        {
            var addr = States.Instance.currentAvatarState.Value.address.ToString();
            return $"DIALOG_{addr}_{dialogId}";
        }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            _itemTextColor = $"#{ColorHelper.ColorToHexRGBA(itemTextColor)}";
        }

        #endregion

        public void Show(int dialogId)
        {
            _playerPrefsKey = GetPlayerPrefsKeyOfCurrentAvatarState(dialogId);
//            if (PlayerPrefs.GetInt(_playerPrefsKey, 0) > 0)
//                return;

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
                string localizedName;
                try
                {
                    localizedName = LocalizationManager.Localize($"CHARACTER_{_characterId}_NAME");
                }
                catch (KeyNotFoundException e)
                {
                    localizedName = characterData.characterName;
                }

                var res = Resources.Load<Sprite>($"Images/character_{characterData.characterResource}");
                imgCharacter.sprite = res;
                imgCharacter.enabled = imgCharacter.sprite != null;
                txtContainer.text = localizedName;
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

                yield return new WaitForSeconds(textInterval);
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
                        int pairValue = int.Parse(pair[1]);
                        Debug.LogWarning($"{pair[0]}:{pairValue}");
                        switch (pair[0])
                        {
                            case "character":
                                _characterId = pairValue;

                                break;
                            case "item":
                                if (Data.Tables.instance.Item.TryGetValue(pairValue, out var itemData))
                                {
                                    var localizedItemName = LocalizationManager.Localize($"ITEM_{itemData.id}_NAME");
                                    
                                    left = $"{left}<color={_itemTextColor}>{localizedItemName}</color>";
                                }

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
