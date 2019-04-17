using System;
using System.IO;
using DG.Tweening;
using Nekoyume.Action;
using Nekoyume.Game;
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
        public GameObject profileImage;
        public GameObject menuSelect;
        public GameObject menuCreate;
        public GameObject statusInfo;
        public GameObject grid;
        public GameObject optionGrid;
        public GameObject optionRow;
        private int _selectedIndex;
        private Nekoyume.Model.Avatar _avatar;

        protected override void Awake()
        {
            base.Awake();

            deletePopUp.SetActive(false);
            btnDelete.SetActive(false);
            nameField.gameObject.SetActive(false);
            Game.Event.OnLoginDetail.AddListener(Init);
            ActionManager.instance.InitAgent();
        }

        private void OnEnable()
        {
            ActionManager.instance.DidAvatarLoaded += OnDidAvatarLoaded;
        }

        private void OnDisable()
        {
            ActionManager.instance.DidAvatarLoaded -= OnDidAvatarLoaded;
        }

        public void LoginClick()
        {
            btnLogin.SetActive(false);
            nameField.gameObject.SetActive(false);
            ActionManager.instance.InitAvatar(_selectedIndex);
            ActionManager.instance.StartSync();
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
            Close();
            Game.Event.OnNestEnter.Invoke();
            var login = Find<Login>();
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
            ActionManager.instance.InitAvatar(_selectedIndex);
            var nickName = nameField.text;
            ActionManager.instance.CreateNovice(nickName);
            ActionManager.instance.StartSync();
            AudioController.PlayClick();
        }

        private void Init(int index)
        {
            _selectedIndex = index;
            var active = true;
            _avatar = ActionManager.instance.Avatars[_selectedIndex];
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
            var hp = player.currentHP;
            var hpMax = player.currentHP;
            var exp = player.exp;
            var expMax = player.expMax;

            //hp, exp
            textHp.text = $"{hp}/{hpMax}";
            textExp.text = $"{exp}/{expMax}";

            //percentage
            var hpPercentage = hp / (float) hpMax;
            var expPercentage = player.expNeed / (float) expMax;

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

            // status info
            var fields = player.GetType().GetFields();
            foreach (var field in fields)
            {
                if (field.IsDefined(typeof(InformationFieldAttribute), true))
                {
                    GameObject row = Instantiate(statusInfo, grid.transform);
                    var info = row.GetComponent<StatusInfo>();
                    info.Set(field.Name, field.GetValue(player), player.GetAdditionalStatus(field.Name));
                }
            }

            //option info
            foreach (var option in player.GetOptions())
            {
                GameObject row = Instantiate(optionRow, optionGrid.transform);
                var text = row.GetComponent<Text>();
                text.text = option;
                row.SetActive(true);
            }
        }

        public override void Close()
        {
            Clear();
            base.Close();
        }

        private void Clear()
        {
            foreach (Transform child in grid.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in optionGrid.transform)
            {
                Destroy(child.gameObject);
            }

            deletePopUp.GetComponent<Widget>().Close();
        }

        public void DeleteCharacter()
        {
            //Delete key, avatar
            var prefsKey = string.Format(ActionManager.PrivateKeyFormat, _selectedIndex);
            string privateKey = PlayerPrefs.GetString(prefsKey, "");
            PlayerPrefs.DeleteKey(prefsKey);
            Debug.Log($"Delete {prefsKey}: {privateKey}");
            var fileName = string.Format(ActionManager.AvatarFileFormat, _selectedIndex);
            string datPath = Path.Combine(Application.persistentDataPath, fileName);
            if (File.Exists(datPath))
                File.Delete(datPath);
            PlayerPrefs.Save();

            Clear();

            //Reset player
            var go = GameObject.Find("Stage");
            var stage = go.GetComponent<Stage>();
            var player = stage.selectedPlayer;
            player.Init(new Player());

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
