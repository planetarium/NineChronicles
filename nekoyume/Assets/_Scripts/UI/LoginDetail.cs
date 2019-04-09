using System;
using System.IO;
using DG.Tweening;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using Nekoyume.Model;
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
        private Nekoyume.Model.Avatar _avatar;

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
            AudioController.PlayClick();
        }

        public void DeleteClick()
        {
            deletePopUp.GetComponent<Widget>().Show();
            deletePopUp.transform.SetAsLastSibling();
            AudioController.PlayClick();
        }

        public void BackClick()
        {
            Game.Event.OnNestEnter.Invoke();
            var login = Find<Login>();
            Close();
            login.Show();
            AudioController.PlayClick();
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
            AudioController.PlayClick();
        }

        private void Init(int index)
        {
            _selectedIndex = index;
            var active = true;
            _avatar = ActionManager.Instance.Avatars[_selectedIndex];
            if (ReferenceEquals(_avatar, null))
            {
                active = false;
                _avatar = CreateNovice.CreateAvatar("");
                nameField.text = "";
            }

            // create new or login
            nameField.gameObject.SetActive(!active);
            btnDelete.SetActive(active);
            profileImage.SetActive(active);

            var player = _avatar.ToPlayer();
            levelInfo.text = $"LV. {player.level}";
            nameInfo.text = $"{_avatar.Name}";

            SetInformation(player);
            menuCreate.SetActive(!active);
            menuSelect.SetActive(active);
            Show();
        }

        private void SetInformation(Player player)
        {
            var hp = player.hp;
            var hpMax = player.hp;
            var exp = player.exp;
            var expMax = player.expMax;

            //hp, exp
            textHp.text = $"{hp}/{hpMax}";
            textExp.text = $"{exp}/{expMax}";

            //percentage
            var hpPercentage = hp / (float) hpMax;
            var expPercentage = exp / (float) expMax;

            foreach (
                var tuple in new[]
                {
                    new Tuple<Slider, float>(hpBar, hpPercentage),
                    new Tuple<Slider, float>(expBar, expPercentage)
                }
            )
            {
                var slide = tuple.Item1;
                var percentage = tuple.Item2;
                slide.fillRect.gameObject.SetActive(percentage > 0.0f);
                percentage = Mathf.Min(Mathf.Max(percentage, 0.1f), 1.0f);
                slide.value = 0.0f;
                slide.DOValue(percentage, 2.0f).SetEase(Ease.OutCubic);
            }
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

        private void OnDidAvatarLoaded(object sender, Nekoyume.Model.Avatar a)
        {
            var w = Find<LoadingScreen>();
            if (!ReferenceEquals(w, null))
            {
                w.Close();
            }
        }
    }
}
