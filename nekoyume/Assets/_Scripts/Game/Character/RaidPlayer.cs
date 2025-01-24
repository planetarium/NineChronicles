using System.Collections.Generic;
using Libplanet.Crypto;
using UnityEngine;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.UI;

namespace Nekoyume.Game.Character
{
    using System.Linq;

    public class RaidPlayer : RaidCharacter
    {
        [SerializeField]
        private CharacterAppearance appearance;

        private readonly List<Costume> _costumes = new();
        private readonly List<Equipment> _equipments = new();

        public Pet Pet => appearance.Pet;

        protected override void Awake()
        {
            Animator = new PlayerAnimator(this)
            {
                TimeScale = AnimatorTimeScale
            };

            base.Awake();
        }

        public void Init(Address avatarAddress, ArenaPlayerDigest digest, RaidCharacter target)
        {
            Init(target);

            appearance.Set(digest, avatarAddress, Animator, _hudContainer, TableSheets.Instance);
            _attackTime = SpineAnimationHelper.GetAnimationDuration(appearance, "Attack");
            _criticalAttackTime = SpineAnimationHelper.GetAnimationDuration(appearance, "CriticalAttack");
            _target = target;
            Id = new System.Guid();
            _costumes.Clear();
            _costumes.AddRange(digest.Costumes);
            _equipments.Clear();
            _equipments.AddRange(digest.Equipments);
        }

        protected override void ShowCutscene()
        {
            base.ShowCutscene();

            if (_costumes.Exists(x => x.ItemSubType == ItemSubType.FullCostume))
            {
                return;
            }

            var armor = _equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
            AreaAttackCutscene.Show(armor?.Id ?? GameConfig.DefaultAvatarArmorId);
        }

        protected override void OnDeadEnd()
        {
            base.OnDeadEnd();
            gameObject.SetActive(false);
        }
    }
}
