using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class LobbyCharacter : Character
    {
        private const float AnimatorTimeScale = 1.2f;
        private HudContainer _hudContainer;

        [SerializeField]
        private CharacterAppearance appearance;

        private void Awake()
        {
            Animator = new PlayerAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = AnimatorTimeScale;
            Animator.Idle();
        }

        public void Set(
            AvatarState avatarState,
            List<Equipment> equipments,
            List<Costume> costumes,
            System.Action onFinish = null)
        {
            _hudContainer ??= Widget.Create<HudContainer>(true);
            _hudContainer.transform.localPosition = Vector3.left * 200000;
            appearance.Set(
                avatarState.address,
                Animator,
                _hudContainer,
                costumes,
                equipments,
                avatarState.ear,
                avatarState.lens,
                avatarState.hair,
                avatarState.tail,
                onFinish: onFinish);

            var title = costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.Title);
            Widget.Find<LobbyMenu>().UpdateTitle(title);

            var items = new List<Guid>();
            items.AddRange(costumes.Select(x => x.ItemId));
            items.AddRange(equipments.Select(x => x.ItemId));
            avatarState.EquipItems(items);

            var status = Widget.Find<Status>();
            status.UpdateForLobby(avatarState, equipments, costumes);
        }

        public void Touch()
        {
            Animator.Touch();
        }

        public void TouchPet()
        {
            appearance.Pet.Animator.Play(PetAnimation.Type.Interaction);
        }

        public void EnterLobby()
        {
            var status = Widget.Find<Status>();
            status.Close(true);
            appearance.Pet.SetSpineObject();
            StartCoroutine(CoPlayAnimation());
        }

        private IEnumerator CoPlayAnimation()
        {
            Animator.Run();
            yield return new WaitForSeconds(1.0f);
            Animator.Idle();
        }
    }
}
