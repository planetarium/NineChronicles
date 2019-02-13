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
            deletePopUp.SetActive(true);
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
            try
            {
                _avatar = ActionManager.Instance.Avatars[_selectedIndex];
                var btnText = btnLogin.GetComponentInChildren<Text>();
                btnText.text = "게임 시작";
                btnDelete.SetActive(true);
                nameField.gameObject.SetActive(false);
            }
            catch (ArgumentException)
            {
                _avatar = null;
                btnDelete.SetActive(false);
                nameField.gameObject.SetActive(true);
                nameField.text = "";

            }
            var game = GameObject.Find("Game");
            Tables tables = game.GetComponent<Tables>();
            Stats statsData;
            int level = _avatar?.Level ?? 1;
            levelInfo.text = $"LV. {level} {_avatar?.Name}";
            if (!tables.Stats.TryGetValue(level, out statsData))
                return;

            int hp = statsData.Health;
            int hpMax = hp;
            long exp = 0;
            if (_avatar?.Level > 1)
            {
                hp = _avatar.CurrentHP;
                exp = _avatar.EXP;
                hpMax = _avatar.HPMax;
            }
            textHp.text = $"{hp}/{hpMax}";
            textExp.text = $"{exp}/{statsData.Exp}";
            float hpValue = hp / (float) hpMax;
            hpBar.fillRect.gameObject.SetActive(hpValue > 0.0f);
            hpValue = Mathf.Min(Mathf.Max(hpValue, 0.1f), 1.0f);
            hpBar.value = hpValue;

            float expValue = exp / (float) statsData.Exp;
            expBar.fillRect.gameObject.SetActive(expValue > 0.0f);
            expValue = Mathf.Min(Mathf.Max(expValue, 0.1f), 1.0f);
            expBar.value = expValue;

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
            deletePopUp.gameObject.SetActive(false);
            Init(_selectedIndex);
        }
    }
}
