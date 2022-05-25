using System;
using Nekoyume.UI.Module.Arena.Emblems;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module.Arena.Join
{
    [Serializable]
    public enum ArenaJoinSeasonType
    {
        Offseason = 0,
        Season,
        Championship,
    }

    [Serializable]
    public class ArenaJoinSeasonItemData
    {
        public ArenaJoinSeasonType type;

        public string text;
    }

    public class ArenaJoinSeasonScrollContext
    {
        public int selectedIndex = -1;
        public Action<int> onCellClicked;
    }

    public class ArenaJoinSeasonCell :
        FancyCell<ArenaJoinSeasonItemData, ArenaJoinSeasonScrollContext>
    {
        private static class AnimatorHash
        {
            public static readonly int Scroll = Animator.StringToHash("Scroll");
        }

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private ArenaJoinSeasonCellOffseason _offseason;

        [SerializeField]
        private ArenaJoinSeasonCellSeason _season;

        [SerializeField]
        private ArenaJoinSeasonCellChampionship _championship;

        [SerializeField]
        private GameObject _medalCountObject;

        [SerializeField]
        private GameObject _seasonCountObject;

        [SerializeField]
        private SeasonArenaEmblem[] _seasonArenaEmblems;
        
        private ArenaJoinSeasonItemData _currentData;

#if UNITY_EDITOR
        [ReadOnly]
        public float _currentPosition;
#else
        private float _currentPosition;
#endif

        private void OnEnable() => UpdatePosition(_currentPosition);

        public override void Initialize()
        {
            base.Initialize();
            _offseason.OnClick += () => Context.onCellClicked?.Invoke(Index);
            _season.OnClick += () => Context.onCellClicked?.Invoke(Index);
            _championship.OnClick += () => Context.onCellClicked?.Invoke(Index);
        }

        public override void UpdateContent(ArenaJoinSeasonItemData itemData)
        {
            _currentData = itemData;
            _medalCountObject.SetActive(false);
            _seasonCountObject.SetActive(false);
            switch (_currentData.type)
            {
                case ArenaJoinSeasonType.Offseason:
                    _offseason.Show(_currentData, Index == Context.selectedIndex);
                    _season.Hide();
                    _championship.Hide();
                    break;
                case ArenaJoinSeasonType.Season:
                    _offseason.Hide();
                    _season.Show(_currentData, Index == Context.selectedIndex);
                    _championship.Hide();
                    break;
                case ArenaJoinSeasonType.Championship:
                    _offseason.Hide();
                    _season.Hide();
                    _championship.Show(_currentData, Index == Context.selectedIndex);

                    var hasSeasons = true;
                    if (hasSeasons)
                    {
                        foreach (var seasonArenaEmblem in _seasonArenaEmblems)
                        {
                            seasonArenaEmblem.SetData(1, true);
                        }

                        _seasonCountObject.SetActive(true);
                    }
                    break;
            }
        }

        public override void UpdatePosition(float position)
        {
            _currentPosition = position;
            PlayAnimation(_animator, _currentPosition);

            switch (_currentData?.type)
            {
                case ArenaJoinSeasonType.Offseason:
                    PlayAnimation(_offseason.Animator, _currentPosition);
                    break;
                case ArenaJoinSeasonType.Season:
                    PlayAnimation(_season.Animator, _currentPosition);
                    break;
                case ArenaJoinSeasonType.Championship:
                    PlayAnimation(_championship.Animator, _currentPosition);
                    break;
                default:
                    var value = _currentData?.type.ToString() ?? "null";
                    Debug.Log($"{nameof(ArenaJoinSeasonCell)} type: {value}");
                    break;
            }
        }

        private static void PlayAnimation(Animator animator, float normalizedTime)
        {
            if (animator.isActiveAndEnabled)
            {
                animator.Play(AnimatorHash.Scroll, -1, normalizedTime);
            }

            animator.speed = 0;
        }
    }
}
