// using System.Collections.Generic;
// using System.Reactive.Subjects;
// using DG.Tweening;
// using Nekoyume.Game.Character;
// using Spine;
// using Spine.Unity;
// using UnityEngine;
//
// namespace Nekoyume.Game.Avatar
// {
//     public class AvatarAnimator
//     {
//         private readonly Animator _animator;
//         private Sequence _colorTweenSequence;
//         private static readonly int FillPhase = Shader.PropertyToID("_FillPhase");
//         private static readonly int PrologueSpeed = Animator.StringToHash("PrologueSpeed");
//         private readonly int _baseLayerIndex;
//         private const float ColorTweenFrom = 0f;
//         private const float ColorTweenTo = 0.6f;
//         private const float ColorTweenDuration = 0.1f;
//
//         private readonly List<MeshRenderer> _meshRenderers;
//
//         public Bone HudBone { get; }
//         public Subject<string> OnEvent { get; } = new();
//
//         public AvatarAnimator(
//             Animator animator,
//             List<MeshRenderer> meshRenderers,
//             SkeletonAnimation bodySkeletonAnimation)
//         {
//             _animator = animator;
//             _meshRenderers = meshRenderers;
//             _baseLayerIndex = animator.GetLayerIndex("Base Layer");
//             HudBone = bodySkeletonAnimation.Skeleton.FindBone("HUD");
//         }
//
//         public void Dispose()
//         {
//             OnEvent?.Dispose();
//         }
//
//         public bool IsIdle()
//         {
//             return _animator.GetCurrentAnimatorStateInfo(_baseLayerIndex).IsName(nameof(CharacterAnimation.Type.Idle));
//         }
//
//         public void Standing()
//         {
//             _animator.SetFloat(PrologueSpeed, 0.1f);
//
//             if (_animator.GetBool(nameof(CharacterAnimation.Type.Standing)))
//             {
//                 return;
//             }
//
//             _animator.Play(nameof(CharacterAnimation.Type.Standing), _baseLayerIndex, 0f);
//             _animator.SetBool(nameof(CharacterAnimation.Type.Standing), true);
//         }
//
//         public void StandingToIdle()
//         {
//             _animator.SetBool(nameof(CharacterAnimation.Type.Standing), false);
//         }
//
//         public void Idle()
//         {
//             _animator.Play(nameof(CharacterAnimation.Type.Idle), _baseLayerIndex, 0f);
//             _animator.SetBool(nameof(CharacterAnimation.Type.Standing), false);
//             _animator.SetBool(nameof(CharacterAnimation.Type.Run), false);
//         }
//
//         public void Touch()
//         {
//             _animator.Play(nameof(CharacterAnimation.Type.Touch), _baseLayerIndex, 0f);
//         }
//
//         public void Run()
//         {
//             if (_animator.GetBool(nameof(CharacterAnimation.Type.Run)))
//             {
//                 return;
//             }
//
//             _animator.Play(nameof(CharacterAnimation.Type.Run), _baseLayerIndex, 0f);
//             _animator.SetBool(nameof(CharacterAnimation.Type.Run), true);
//         }
//
//         public void Skill(int animationId = 1)
//         {
//             var animation = animationId == 1 ? CharacterAnimation.Type.Skill_01 : CharacterAnimation.Type.Skill_02;
//             _animator.Play(animation.ToString(), _baseLayerIndex, 0f);
//         }
//
//         public void StopRun()
//         {
//             _animator.SetBool(nameof(CharacterAnimation.Type.Run), false);
//         }
//
//          public void Attack()
//         {
//             _animator.Play(nameof(CharacterAnimation.Type.Attack), _baseLayerIndex, 0f);
//         }
//
//         public void Cast()
//         {
//             _animator.Play(nameof(CharacterAnimation.Type.Casting), _baseLayerIndex, 0f);
//         }
//
//         public void CastAttack()
//         {
//             _animator.Play(nameof(CharacterAnimation.Type.CastingAttack), _baseLayerIndex, 0f);
//         }
//
//         public void CriticalAttack()
//         {
//             _animator.Play(nameof(CharacterAnimation.Type.CriticalAttack), _baseLayerIndex, 0f);
//         }
//
//         public void Hit()
//         {
//             if (!_animator.GetCurrentAnimatorStateInfo(_baseLayerIndex)
//                     .IsName(nameof(CharacterAnimation.Type.Idle)))
//             {
//                 return;
//             }
//
//             _animator.Play(nameof(CharacterAnimation.Type.Hit), _baseLayerIndex, 0f);
//         }
//
//         public void Win(int score = 3)
//         {
//             var animationType = CharacterAnimation.Type.Win;
//
//             switch (score)
//             {
//                 case 2:
//                     animationType = CharacterAnimation.Type.Win_02;
//                     break;
//                 case 3:
//                     animationType = CharacterAnimation.Type.Win_03;
//                     break;
//             }
//
//             _animator.Play(animationType.ToString(), _baseLayerIndex, 0f);
//             ColorTween();
//         }
//
//         public void TurnOver()
//         {
//             _animator.Play(nameof(CharacterAnimation.Type.TurnOver_02), _baseLayerIndex, 0f);
//             ColorTween();
//         }
//
//         public void Die()
//         {
//             _animator.Play(nameof(CharacterAnimation.Type.Die), _baseLayerIndex, 0f);
//             ColorTween();
//         }
//
//         private void ColorTween()
//         {
//             _colorTweenSequence?.Kill();
//             _colorTweenSequence = DOTween.Sequence();
//             _colorTweenSequence.Append(DOTween.To(
//                 () => ColorTweenFrom,
//                 value =>
//                 {
//                     foreach (var meshRenderer in _meshRenderers)
//                     {
//                         meshRenderer.material.SetFloat(FillPhase, value);
//                     }
//                 },
//                 ColorTweenTo,
//                 ColorTweenDuration));
//             _colorTweenSequence.Append(DOTween.To(
//                 () => ColorTweenTo,
//                 value =>
//                 {
//                     foreach (var meshRenderer in _meshRenderers)
//                     {
//                         meshRenderer.material.SetFloat(FillPhase, value);
//                     }
//                 },
//                 ColorTweenFrom,
//                 ColorTweenDuration));
//             _colorTweenSequence.Play().OnComplete(() => _colorTweenSequence = null);
//         }
//     }
// }
