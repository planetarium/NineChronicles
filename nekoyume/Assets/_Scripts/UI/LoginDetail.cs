using DG.Tweening;
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
        public GameObject btnCreate;
        public InputField nameField;
        public GameObject deletePopUp;
        public Text textHp;
        public Text textExp;
        public Slider hpBar;
        public Slider expBar;
        public Text levelInfo;
        public Text nameInfo;
        public GameObject statusDetail;
        public GameObject character;
        public GameObject profileImage;
        public GameObject menuSelect;
        public GameObject menuCreate;
        private int _selectedIndex;
        private Model.Avatar _avatar;

        private void Awake()
        {
            deletePopUp.SetActive(false);
            btnDelete.SetActive(false);
            nameField.gameObject.SetActive(false);
            Game.Event.OnLoginDetail.AddListener(Init);
        }

        private void OnEnable()
        {
            ActionManager.Instance.DidAvatarLoaded += OnDidAvatarLoaded;
        }

        private void OnDisable()
        {
            ActionManager.Instance.DidAvatarLoaded -= OnDidAvatarLoaded;
        }

        public void LoginClick()
        {
            btnLogin.SetActive(false);
            nameField.gameObject.SetActive(false);
            ActionManager.Instance.Init(_selectedIndex);
            ActionManager.Instance.StartSync();
        }

        public void DeleteClick()
        {
            deletePopUp.GetComponent<Widget>().Show();
            deletePopUp.transform.SetAsLastSibling();
        }

        public void BackClick()
        {
            Game.Event.OnNestEnter.Invoke();
            var login = Find<Login>();
            Close();
            login.Init();
            login.Show();
        }

        public void CreateClick()
        {
            var w = Find<LoadingScreen>();
            if (!ReferenceEquals(w, null))
            {
                w.Show();   
            }
            
            btnCreate.SetActive(false);
            ActionManager.Instance.Init(_selectedIndex);
            var nickName = nameField.text;
            ActionManager.Instance.CreateNovice(nickName);
            ActionManager.Instance.StartSync();
        }

        private void Init(int index)
        {
            _selectedIndex = index;
            int level = 1;
            var active = true;
            var imagePath = "character_0003/character_01";
            try
            {
                _avatar = ActionManager.Instance.Avatars[_selectedIndex];
                level = _avatar.Level;
            }
            catch (Exception e)
            {
                if (e is ArgumentOutOfRangeException || e is NullReferenceException)
                {
                    active = false;
                    _avatar = null;
                    nameField.text = "";
                    imagePath = "character_02";
                }
                else
                {
                    throw;
                }
            }
            nameField.gameObject.SetActive(!active);
            btnDelete.SetActive(active);
            profileImage.SetActive(active);
            var game = GameObject.Find("Game");
            Tables tables = game.GetComponent<Tables>();
            Stats statsData;
            levelInfo.text = $"LV. {level}";
            nameInfo.text = $"{_avatar?.Name}";
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
            hpBar.value = 0.0f;// hpPercentage;
            hpBar.DOValue(hpPercentage, 2.0f).SetEase(Ease.OutCubic);

            float expPercentage = exp / (float) statsData.Exp;
            expBar.fillRect.gameObject.SetActive(expPercentage > 0.0f);
            expPercentage = Mathf.Min(Mathf.Max(expPercentage, 0.1f), 1.0f);
            expBar.value = 0.0f;// expPercentage;
            expBar.DOValue(expPercentage, 3.0f).SetEase(Ease.OutCubic);

            var statusDetailScript = statusDetail.GetComponent<StatusDetail>();
            statusDetailScript.Init(statsData);
            menuCreate.SetActive(!active);
            menuSelect.SetActive(active);
            var image = character.GetComponent<Image>();
            var sprite = Resources.Load<Sprite>($"avatar/{imagePath}");
            if (sprite != null)
            {
                image.sprite = sprite;
            }            
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

        private void OnDidAvatarLoaded(object sender, Model.Avatar a)
        {
            var w = Find<LoadingScreen>();
            if (!ReferenceEquals(w, null))
            {
                w.Close();
            }
        }
    }
}
