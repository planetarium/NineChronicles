using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Libplanet;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class DialogPopup : PopupWidget
    {
        public float textInterval = 0.06f;
        public Color itemTextColor;
        private const string TimerFormat = "({0})";

        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtDialog;
        public Image imgCharacter;
        [SerializeField]
        private float time = default;
        [SerializeField]
        private TextMeshProUGUI textArrow = null;
        [SerializeField]
        private TextMeshProUGUI textTimer = null;

        private string _playerPrefsKey;
        private string _dialogKey;
        private int _dialogIndex;
        private int _dialogNum;
        private int _characterId;
        private string _npc;
        private Coroutine _coroutine = null;
        private Coroutine _timerCoroutine = null;
        private string _text;
        private string _itemTextColor;
        private Dictionary<int, DialogEffect> _effects = new Dictionary<int, DialogEffect>();
        private System.Action _onDialogCompleted;

        public static bool TryGetPlayerPrefsKeyOfCurrentAvatarState(int dialogId, out string key)
        {
            if (States.Instance.CurrentAvatarState is null)
            {
                key = default;
                return false;
            }

            key = GetPlayerPrefsKey(States.Instance.CurrentAvatarState.address, dialogId);
            return true;
        }

        public static string GetPlayerPrefsKey(Address address, int dialogId)
        {
            var addr = address.ToString();
            return $"DIALOG_{addr}_{dialogId}";
        }

        public static void DeleteDialogPlayerPrefsOfCurrentAvatarState()
        {
            if (States.Instance.CurrentAvatarState is null)
            {
                return;
            }

            DeleteDialogPlayerPrefs(States.Instance.CurrentAvatarState.address);
        }

        public static void DeleteDialogPlayerPrefs(Address address)
        {
            var index = 1;
            while (true)
            {
                var key = GetPlayerPrefsKey(address, index);
                if (!PlayerPrefs.HasKey(key))
                {
                    break;
                }

                PlayerPrefs.DeleteKey(key);
                index++;
            }
        }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            _itemTextColor = $"#{ColorHelper.ColorToHexRGBA(itemTextColor)}";

            CloseWidget = null;
            SubmitWidget = Skip;
        }

        protected override void OnDisable()
        {
            _coroutine = null;
            base.OnDisable();
        }

        #endregion

        public void Show(int dialogId, System.Action onDialogCompleted = null)
        {
            _onDialogCompleted = onDialogCompleted;

            if (!TryGetPlayerPrefsKeyOfCurrentAvatarState(dialogId, out _playerPrefsKey) ||
                PlayerPrefs.GetInt(_playerPrefsKey, 0) > 0)
            {
                onDialogCompleted?.Invoke();
                return;
            }

            _dialogKey = $"DIALOG_{dialogId}_{1}_";
            _dialogIndex = 0;
            _dialogNum = L10nManager.LocalizedCount(_dialogKey);
            if (_dialogNum <= 0)
            {
                onDialogCompleted?.Invoke();
                return;
            }

            base.Show();
            _coroutine = StartCoroutine(CoShowText());
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
                _timerCoroutine = StartCoroutine(CoTimer(time));
                return;
            }

            _dialogIndex++;

            if (_dialogIndex >= _dialogNum)
            {
                PlayerPrefs.SetInt(_playerPrefsKey, 1);
                _onDialogCompleted?.Invoke();
                StopTimer();
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

            _characterId = 0;
            _npc = null;
            _effects.Clear();
            _text = ParseText(text);
            StopTimer();

            if (Game.Game.instance.TableSheets.CharacterSheet.TryGetValue(_characterId, out var characterData))
            {
                var localizedName = L10nManager.LocalizeCharacterName(_characterId);
                var sprite = SpriteHelper.GetDialogPortrait(_characterId.ToString(), false);
                imgCharacter.overrideSprite = sprite;
                imgCharacter.SetNativeSize();
                imgCharacter.enabled = imgCharacter.sprite != null;
                txtName.text = localizedName;
            }

            // TODO: npc
            if (!string.IsNullOrEmpty(_npc))
            {
                string localizedName;
                try
                {
                    localizedName = L10nManager.Localize($"NPC_{_npc}_NAME");
                }
                catch (KeyNotFoundException)
                {
                    localizedName = "???";
                }

                var sprite = SpriteHelper.GetDialogPortrait(_npc);
                imgCharacter.overrideSprite = sprite;
                imgCharacter.SetNativeSize();
                imgCharacter.enabled = imgCharacter.overrideSprite != null;
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

                if (_effects.TryGetValue(textIndex, out var effect))
                {
                    effect.Execute(this);
                }

                yield return new WaitForSeconds(textInterval);
            }
            _timerCoroutine = StartCoroutine(CoTimer(time));

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
                        string[] pair = text.Substring(openIndex + 1, i - openIndex - 1)
                            .Split(':', '：')
                            .Select(value => value.Trim())
                            .ToArray();
                        string pairKey = pair[0].ToLower();
                        int.TryParse(pair[1], out var pairValue);
                        switch (pairKey)
                        {
                            case "character":
                                _characterId = pairValue;

                                break;
                            case "npc":
                                _npc = pair[1];

                                break;
                            case "item":
                                if (Game.Game.instance.TableSheets.ItemSheet.TryGetValue(pairValue, out var itemData))
                                {
                                    var localizedItemName = itemData.GetLocalizedName(false);
                                    left = $"{left}<color={_itemTextColor}>{localizedItemName}</color>";
                                }

                                break;
                            case "shake_vertical":
                                // TODO: 좀더 좋은 방법
                                var values = pair[1].Split('|');
                                _effects.Add(left.Length - 1, new DialogEffectShake()
                                {
                                    value = new Vector3(0.0f, -int.Parse(values[0])),
                                    duration = int.Parse(values[1]),
                                    loops = int.Parse(values[2]),
                                });

                                break;
                        }

                        text = $"{left}{right}";
                        i = left.Length - 1;

                        break;
                }
            }

            return text;
        }

        private IEnumerator CoTimer(float timer)
        {
            textArrow.gameObject.SetActive(false);
            textTimer.text = string.Format(TimerFormat, timer.ToString(CultureInfo.InvariantCulture));
            textTimer.gameObject.SetActive(true);
            var prevFlooredTime = Mathf.Round(timer);
            yield return new WaitForSeconds(1f);
            while (timer >= .3f)
            {
                // 텍스트 업데이트 횟수를 줄이기 위해 소숫점을 내림해
                // 정수부만 체크 후 텍스트 업데이트 여부를 결정합니다.
                var flooredTime = Mathf.Floor(timer);
                if (flooredTime < prevFlooredTime)
                {
                    prevFlooredTime = flooredTime;
                    textTimer.text = string.Format(TimerFormat, flooredTime.ToString(CultureInfo.InvariantCulture));
                }

                timer -= Time.deltaTime;
                yield return null;
            }

            Skip();
        }

        private void StopTimer()
        {
            textArrow.gameObject.SetActive(true);
            textTimer.gameObject.SetActive(false);
            if (!(_timerCoroutine is null))
            {
                StopCoroutine(_timerCoroutine);
            }
        }
    }
}
