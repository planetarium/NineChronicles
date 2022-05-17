using System;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module.Arena
{
    [Serializable]
    public enum ArenaJoinSeasonType
    {
        Preseason = 0,
        Season,
        Championship,
    }

    [Serializable]
    public class ArenaJoinSeasonItemData
    {
        public ArenaJoinSeasonType type;

        // NOTE: or int index;
        public string name;
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

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private ArenaJoinSeasonCellPreseason _preseason;

        [SerializeField]
        private ArenaJoinSeasonCellSeason _season;

        [SerializeField]
        private ArenaJoinSeasonCellChampionship _championship;

        private ArenaJoinSeasonItemData _currentData;
        private float _currentPosition;

        private void OnEnable() => UpdatePosition(_currentPosition);

        public override void UpdateContent(ArenaJoinSeasonItemData itemData)
        {
            _currentData = itemData;
            switch (_currentData.type)
            {
                case ArenaJoinSeasonType.Preseason:
                    _preseason.Show(_currentData, Index == Context.SelectedIndex);
                    _season.Hide();
                    _championship.Hide();
                    break;
                case ArenaJoinSeasonType.Season:
                    _preseason.Hide();
                    _season.Show(_currentData, Index == Context.SelectedIndex);
                    _championship.Hide();
                    break;
                case ArenaJoinSeasonType.Championship:
                    _preseason.Hide();
                    _season.Hide();
                    _championship.Show(_currentData, Index == Context.SelectedIndex);
                    break;
            }
        }

        public override void UpdatePosition(float position)
        {
            _currentPosition = position;
            PlayAnimation(_animator, _currentPosition);

            switch (_currentData?.type)
            {
                case ArenaJoinSeasonType.Preseason:
                    PlayAnimation(_preseason.Animator, _currentPosition);
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
