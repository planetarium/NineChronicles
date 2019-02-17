using System;
using System.IO;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class LoginDetail : Widget
    {
        public GameObject btnLogin;
        public GameObject btnDelete;
        public InputField nameField;
        public GameObject deletePopUp;
        public Text textHp;
        public Text textExp;
        public Slider hpBar;
        public Slider expBar;
        public Text text;
        public Text levelInfo;
        public GameObject statusDetail;
        public GameObject character;
        private int _selectedIndex;
        private Model.Avatar _avatar;

        private void Awake()
        {
            deletePopUp.SetActive(false);
            btnDelete.SetActive(false);
            nameField.gameObject.SetActive(false);
            Game.Event.OnLoginDetail.AddListener(Init);
        }

        public void LoginClick()
        {
            btnLogin.SetActive(false);
            nameField.gameObject.SetActive(false);
            text.text = "Connecting...";
            ActionManager.Instance.Init(_selectedIndex);
            if (_avatar == null)
            {
                var nickName = nameField.text;
                ActionManager.Instance.CreateNovice(nickName);
            }
            ActionManager.Instance.StartSync();
        }

        public void DeleteClick()
        {
            deletePopUp.GetComponent<Widget>().Show();
            deletePopUp.transform.SetAsLastSibling();
        }

        public void BackClick()
        {
            var login = Find<Login>();
            Close();
            login.Init();
            login.Show();
        }

        private void Init(int index)
        {
            _selectedIndex = index;
            int level = 1;
            try
            {
                _avatar = ActionManager.Instance.Avatars[_selectedIndex];
                level = _avatar.Level;
                nameField.gameObject.SetActive(false);
                btnDelete.SetActive(true);
            }
            catch (ArgumentException)
            {
                _avatar = null;
                btnDelete.SetActive(false);
                nameField.gameObject.SetActive(true);
                nameField.text = "";
                var image = character.GetComponent<Image>();
                var sprite = Resources.Load<Sprite>($"avatar/character_02");
                if (sprite != null)
                {
                    image.sprite = sprite;
                }
                var btnText = btnLogin.GetComponentInChildren<Text>();
                btnText.text = "캐릭터 생성";
            }
            var game = GameObject.Find("Game");
            Tables tables = game.GetComponent<Tables>();
            Stats statsData;
            levelInfo.text = $"LV. {level} {_avatar?.Name}";
            if (!tables.Stats.TryGetValue(level, out statsData))
                return;

            int hp = statsData.Health;
            int hpMax = hp;
            long exp = 0;
            if (level > 1)
            {
                hp = _avatar.CurrentHP;
                exp = _avatar.EXP;
                hpMax = _avatar.HPMax;
            }
            textHp.text = $"{hp}/{hpMax}";
            textExp.text = $"{exp}/{statsData.Exp}";
            float hpPercentage = hp / (float) hpMax;
            hpBar.fillRect.gameObject.SetActive(hpPercentage > 0.0f);
            hpPercentage = Mathf.Min(Mathf.Max(hpPercentage, 0.1f), 1.0f);
            hpBar.value = hpPercentage;

            float expPercentage = exp / (float) statsData.Exp;
            expBar.fillRect.gameObject.SetActive(expPercentage > 0.0f);
            expPercentage = Mathf.Min(Mathf.Max(expPercentage, 0.1f), 1.0f);
            expBar.value = expPercentage;

            var statusDetailScript = statusDetail.GetComponent<StatusDetail>();
            statusDetailScript.Init(statsData);
            Show();
        }

        public void DeleteCharacter()
        {
            var prefsKey = string.Format(ActionManager.PrivateKeyFormat, _selectedIndex);
            string privateKey = PlayerPrefs.GetString(prefsKey, "");
            PlayerPrefs.DeleteKey(prefsKey);
            Debug.Log($"Delete {prefsKey}: {privateKey}");
            var fileName = string.Format(ActionManager.AvatarFileFormat, _selectedIndex);
            string datPath = Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(datPath))
                File.Delete(datPath);
            PlayerPrefs.Save();
            deletePopUp.GetComponent<Widget>().Close();
            Init(_selectedIndex);
        }
    }
}
