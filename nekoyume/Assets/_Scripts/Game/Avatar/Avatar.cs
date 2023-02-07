// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using BTAI;
// using Nekoyume.EnumType;
// using Nekoyume.Game.Controller;
// using Nekoyume.Model.Character;
// using Nekoyume.Model.Item;
// using Nekoyume.Model.State;
// using Nekoyume.UI;
// using Spine.Unity;
// using UnityEngine;
//
// namespace Nekoyume.Game.Avatar
// {
//     public class Avatar : MonoBehaviour
//     {
//         [SerializeField]
//         private AvatarSpineController spineController;
//
//         [SerializeField]
//         private Animator animator;
//
//         private const float AnimatorTimeScale = 1.2f;
//         private HudContainer _hudContainer;
//         private AvatarAnimator _avatarAnimator;
//
//         public Guid Id {get; protected set; }
//         public SizeType SizeType { get; protected set; }
//         protected Vector3 HUDOffset => _avatarAnimator.HudBone.GetWorldPosition(transform);
//         protected bool AttackEndCalled { get; set; }
//         protected System.Action ActionPoint;
//
//         private void Awake()
//         {
//             _avatarAnimator = new AvatarAnimator(
//                 animator,
//                 spineController.MeshRenderers,
//                 spineController.GetSkeletonAnimation(AvatarPartsType.body));
//             // animator.speed = AnimatorTimeScale;
//             _avatarAnimator.OnEvent.Subscribe(OnAnimatorEvent);
//             _avatarAnimator.Idle();
//         }
//
//         private void OnAnimatorEvent(string eventName)
//         {
//             switch (eventName)
//             {
//                 case "AttackStart":
//                 case "attackStart":
//                     AudioController.PlaySwing();
//                     break;
//                 case "AttackPoint":
//                 case "attackPoint":
//                     AttackEndCalled = true;
//                     ActionPoint?.Invoke();
//                     ActionPoint = null;
//                     break;
//                 case "Footstep":
//                 case "footstep":
//                     AudioController.PlayFootStep();
//                     break;
//             }
//         }
//
// #if UNITY_EDITOR
//         private void Update()
//         {
//             if (Input.GetKeyDown(KeyCode.Z))
//             {
//                 spineController.UpdateWeapon(10110000);
//             }
//             else if (Input.GetKeyDown(KeyCode.X))
//             {
//                 spineController.UpdateBody(10220000, 1);
//             }
//             else if (Input.GetKeyDown(KeyCode.C))
//             {
//                 spineController.UpdateBody(10224000, 0);
//
//             }
//             else if (Input.GetKeyDown(KeyCode.V))
//             {
//                 _avatarAnimator.Run();
//             }
//             else if (Input.GetKeyDown(KeyCode.B))
//             {
//                 _avatarAnimator.Skill(2);
//             }
//         }
// #endif
//     }
// }
