using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using GeneratedApiNamespace.ArenaServiceClient;
using ArenaType = GeneratedApiNamespace.ArenaServiceClient.ArenaType;

namespace Nekoyume.UI.Module.Arena.Join
{
    [Serializable]
    public class ArenaJoinSeasonItemData
    {
        public SeasonResponse SeasonData;
        public int? SeasonNumber;
        public List<int> ChampionshipSeasonNumbers;

        public string GetRoundName()
        {
            return SeasonData.ArenaType switch
            {
                ArenaType.OFF_SEASON => "off-season",
                ArenaType.SEASON => $"season #{SeasonNumber}",
                ArenaType.CHAMPIONSHIP => $"championship #{SeasonData.SeasonGroupId}",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public class ArenaJoinSeasonScrollContext
    {
        public int SelectedIndex = -1;
        public Action<int> OnCellClicked;
    }

    public class ArenaJoinSeasonCell :
        FancyCell<ArenaJoinSeasonItemData, ArenaJoinSeasonScrollContext>
    {
        private static class AnimatorHash
        {
            public static readonly int Scroll = Animator.StringToHash("Scroll");
        }

        [SerializeField] private Animator _animator;

        [SerializeField] private ArenaJoinSeasonCellOffseason _offseason;

        [SerializeField] private ArenaJoinSeasonCellSeason _season;

        [SerializeField] private ArenaJoinSeasonCellChampionship _championship;

        [SerializeField] private GameObject _medalCountObject;

        [SerializeField] private GameObject _seasonCountObject;

        private ArenaJoinSeasonItemData _currentData;

#if UNITY_EDITOR
        [ReadOnly]
        public float _currentPosition;
#else
        private float _currentPosition;
#endif

        private void OnEnable()
        {
            UpdatePosition(_currentPosition);
        }

        public override void Initialize()
        {
            base.Initialize();
            _offseason.OnClick += () => Context.OnCellClicked?.Invoke(Index);
            _season.OnClick += () => Context.OnCellClicked?.Invoke(Index);
            _championship.OnClick += () => Context.OnCellClicked?.Invoke(Index);
        }

        public override void UpdateContent(ArenaJoinSeasonItemData itemData)
        {
            _currentData = itemData;
            _medalCountObject.SetActive(false);
            switch (_currentData.SeasonData.ArenaType)
            {
                case ArenaType.OFF_SEASON:
                    _offseason.Show(_currentData, Index == Context.SelectedIndex);
                    _season.Hide();
                    _championship.Hide();
                    _seasonCountObject.SetActive(false);
                    break;
                case ArenaType.SEASON:
                    _offseason.Hide();
                    _season.Show(_currentData, Index == Context.SelectedIndex);
                    _championship.Hide();
                    _seasonCountObject.SetActive(false);
                    break;
                case ArenaType.CHAMPIONSHIP:
                    _offseason.Hide();
                    _season.Hide();
                    _championship.Show(_currentData, Index == Context.SelectedIndex);
                    _seasonCountObject.SetActive(true);
                    break;
            }
        }

        public override void UpdatePosition(float position)
        {
            _currentPosition = position;
            PlayAnimation(_animator, _currentPosition);

            switch (_currentData?.SeasonData.ArenaType)
            {
                case ArenaType.OFF_SEASON:
                    PlayAnimation(_offseason.Animator, _currentPosition);
                    break;
                case ArenaType.SEASON:
                    PlayAnimation(_season.Animator, _currentPosition);
                    break;
                case ArenaType.CHAMPIONSHIP:
                    PlayAnimation(_championship.Animator, _currentPosition);
                    break;
                default:
                    var value = _currentData?.SeasonData.ArenaType.ToString() ?? "null";
                    NcDebug.Log($"{nameof(ArenaJoinSeasonCell)} type: {value}");
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
