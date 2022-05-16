using System;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module.Arena
{
    [Serializable]
    public enum ArenaJoinSeasonType
    {
        Weekly,
        Monthly,
        Quarterly,
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
        private ArenaJoinSeasonCellWeekly _weekly;

        [SerializeField]
        private ArenaJoinSeasonCellMonthly _monthly;

        [SerializeField]
        private ArenaJoinSeasonCellQuarterly _quarterly;

        private ArenaJoinSeasonItemData _currentData;
        private float _currentPosition;

        private void OnEnable() => UpdatePosition(_currentPosition);

        public override void Initialize()
        {
            base.Initialize();
            _weekly.OnClick += () => Context.OnCellClicked?.Invoke(Index);
            _monthly.OnClick += () => Context.OnCellClicked?.Invoke(Index);
            _quarterly.OnClick += () => Context.OnCellClicked?.Invoke(Index);
        }

        public override void UpdateContent(ArenaJoinSeasonItemData itemData)
        {
            _currentData = itemData;
            switch (_currentData.type)
            {
                case ArenaJoinSeasonType.Weekly:
                    _weekly.Show(_currentData, Index == Context.SelectedIndex);
                    _monthly.Hide();
                    _quarterly.Hide();
                    break;
                case ArenaJoinSeasonType.Monthly:
                    _weekly.Hide();
                    _monthly.Show(_currentData, Index == Context.SelectedIndex);
                    _quarterly.Hide();
                    break;
                case ArenaJoinSeasonType.Quarterly:
                    _weekly.Hide();
                    _monthly.Hide();
                    _quarterly.Show(_currentData, Index == Context.SelectedIndex);
                    break;
            }
        }

        public override void UpdatePosition(float position)
        {
            _currentPosition = position;
            PlayAnimation(_animator, _currentPosition);

            switch (_currentData?.type)
            {
                case ArenaJoinSeasonType.Weekly:
                    PlayAnimation(_weekly.Animator, _currentPosition);
                    break;
                case ArenaJoinSeasonType.Monthly:
                    PlayAnimation(_monthly.Animator, _currentPosition);
                    break;
                case ArenaJoinSeasonType.Quarterly:
                    PlayAnimation(_quarterly.Animator, _currentPosition);
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
