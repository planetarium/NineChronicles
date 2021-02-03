using Nekoyume.Pattern;
using Nekoyume.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nekoyume.UI
{
    public class TutorialDispenser : MonoSingleton<TutorialDispenser>
    {
        private const string JsonDataPath = "Tutorial/TutorialData";

        private static List<TutorialModel> SharedViewModels =>
            _sharedViewModelsCache ?? (_sharedViewModelsCache = LoadTutorialData());

        private static List<TutorialModel> _sharedViewModelsCache = null;

        private TutorialModel _currentViewModel = null;


        private int _currentId = 0;

        private int _currentIndex = -1;

        [Serializable]
        public class JsonModel
        {
            public TutorialModel[] tutorialModels;
        }

        [Serializable]
        public class TutorialModel
        {
            public int id;
            public TutorialPageType pageType;
            public PageModel[] pages;
        }

        [Serializable]
        public class PageModel
        {
            public int presetId;
            public string contentL10nKey;
            public DialogEmojiType dialogEmojiType;
            public GuideType guideType;
            public TutorialActionType actionType;
            public TutorialTargetType targetType;
        }

        private static List<TutorialModel> LoadTutorialData()
        {
            var json = Resources.Load<TextAsset>(JsonDataPath)?.text;
            if (!string.IsNullOrEmpty(json))
            {
                var jsonModel = JsonUtility.FromJson<JsonModel>(json);
                return jsonModel.tutorialModels.ToList();
            }

            var sb = new StringBuilder($"[{nameof(Tutorial)}]");
            sb.Append($" {nameof(LoadTutorialData)}()");
            sb.Append($" Failed to load resource at {JsonDataPath}");
            Debug.LogError(sb.ToString());
            return null;
        }

        private void SetTutorialId(int id)
        {
            _currentId = id;
            _currentViewModel = SharedViewModels.Find(x => x.id.Equals(id));
            if (_currentViewModel is null)
            {
                throw new KeyNotFoundException($"Tutorial Key not found! Key : {id}");
            }
        }

        public PageModel GetNextTutorialData()
        {
            if (_currentViewModel is null)
            {
                Debug.LogWarning("Tutorial view model not set. Please set current tutorial view model first.");
            }

            ++_currentIndex;
            Debug.LogWarning(_currentIndex);
            Debug.LogWarning(_currentViewModel.pages.Length);
            if (_currentViewModel.pages.Length <= _currentIndex)
            {
                return null;
            }

            var page = _currentViewModel.pages[_currentIndex];
            return page;
        }

        public void Play(int id)
        {
            if (_currentId != id)
            {
                _currentIndex = -1;
                SetTutorialId(id);
            }

            if (_currentViewModel.pages.Length <= _currentIndex + 1)
            {
                return;
            }

            _currentIndex++;
            SaveTutorialProgress();
            Debug.LogError($"tutorial play id : {_currentId}, index : {_currentIndex}");
        }

        public int GetTutorialProgress()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            var prefsKey = $"TUTORIAL_PROGRESS_{avatarAddress}";
            var progress = PlayerPrefs.GetInt(prefsKey, 0);
            return progress;
        }

        private void SaveTutorialProgress()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            var prefsKey = $"TUTORIAL_PROGRESS_{avatarAddress}";
            PlayerPrefs.SetInt(prefsKey, _currentId);
        }
    }
}
