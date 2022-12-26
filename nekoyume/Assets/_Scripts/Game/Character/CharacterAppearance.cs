using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Arena;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class CharacterAppearance : MonoBehaviour
    {
        [SerializeField]
        private BoxCollider boxCollider;

        private CharacterAnimator _animator;
        private HudContainer _hudContainer;
        private GameObject _cachedCharacterTitle;

        public PlayerSpineController SpineController { get; private set; }
        public BoxCollider BoxCollider => boxCollider;

        public void Set(
            ArenaPlayerDigest digest,
            CharacterAnimator animator,
            HudContainer hudContainer)
        {
            _animator = animator;
            _hudContainer = hudContainer;
            Destroy(_cachedCharacterTitle);

            UpdateArmor(digest.Equipments);
            UpdateWeapon(digest.Equipments);

            UpdateEarById(digest.EarIndex);
            UpdateEyeById(digest.LensIndex);
            UpdateHairById(digest.HairIndex);
            UpdateTailById(digest.TailIndex);

            UpdateCostumes(digest.Costumes);
        }

        public void Set(
            CharacterAnimator animator,
            HudContainer hudContainer,
            List<Costume> costumes,
            List<Equipment> equipments,
            int earIndex,
            int lensIndex,
            int hairIndex,
            int tailIndex)
        {
            _animator = animator;
            _hudContainer = hudContainer;
            UpdateArmor(equipments);
            UpdateWeapon(equipments);
            UpdateEarById(earIndex);
            UpdateEyeById(lensIndex);
            UpdateHairById(hairIndex);
            UpdateTailById(tailIndex);
            UpdateCostumes(costumes);
        }

        private void UpdateArmor(IEnumerable<Equipment> equipments)
        {
            if (equipments is null)
            {
                ChangeSpineObject($"Character/Player/{GameConfig.DefaultAvatarArmorId}", false);
                return;
            }

            var armor = equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
            var armorId = armor?.Id ?? GameConfig.DefaultAvatarArmorId;
            var spineResourcePath = armor?.SpineResourcePath ?? $"Character/Player/{armorId}";
            ChangeSpineObject(spineResourcePath, false);
        }

        private void UpdateWeapon(IEnumerable<Equipment> equipments)
        {
            if (equipments is null)
            {
                var defaultId = GameConfig.DefaultAvatarWeaponId;
                var defaultSprite = SpriteHelper.GetPlayerSpineTextureWeapon(defaultId);
                SpineController.UpdateWeapon(defaultId, defaultSprite);
                return;
            }

            var weapon = (Weapon)equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Weapon);
            var id = weapon?.Id ?? 0;
            var level = weapon?.level ?? 0;
            var levelVFXPrefab = ResourcesHelper.GetAuraWeaponPrefab(id, level);
            var sprite = weapon.GetPlayerSpineTexture();
            SpineController.UpdateWeapon(id, sprite, levelVFXPrefab);
        }

        private void UpdateEarById(int earCostumeId)
        {
            const string prefix = "Character/PlayerSpineTexture/EarCostume";
            var leftSprite = Resources.Load<Sprite>($"{prefix}/{earCostumeId}_left");
            var rightSprite = Resources.Load<Sprite>($"{prefix}/{earCostumeId}_right");
            SpineController.UpdateEar(leftSprite, rightSprite);
        }

        private void UpdateEyeById(int eyeCostumeId)
        {
            var prefix = "Character/PlayerSpineTexture/EyeCostume";
            var halfSprite = Resources.Load<Sprite>($"{prefix}/{eyeCostumeId}_half");
            var openSprite = Resources.Load<Sprite>($"{prefix}/{eyeCostumeId}_open");
            SpineController.UpdateEye(halfSprite, openSprite);
        }

        private void UpdateHairById(int hairCostumeId)
        {
            if (!TryGetCostumeRow(hairCostumeId, out var row))
            {
                return;
            }

            var sprites = Enumerable.Range(0, SpineController.HairSlotCount)
                .Select(index =>
                    $"{row.SpineResourcePath}_{SpineController.hairTypeIndex:00}_{index + 1:00}")
                .Select(Resources.Load<Sprite>).ToList();
            SpineController.UpdateHair(sprites);
        }

        private bool TryGetCostumeRow(int costumeId, out CostumeItemSheet.Row row)
        {
            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            return sheet.TryGetValue(costumeId, out row, false);
        }

        private void UpdateTailById(int tailCostumeId)
        {
            SpineController.UpdateTail(tailCostumeId);
        }

        private void UpdateTitle(ItemBase costume)
        {
            Destroy(_cachedCharacterTitle);
            if (costume == null)
            {
                return;
            }

            var clone = ResourcesHelper.GetCharacterTitle(costume.Grade,
                costume.GetLocalizedNonColoredName(false));
            _cachedCharacterTitle = Instantiate(clone, _hudContainer.transform);
            _cachedCharacterTitle.name = costume.Id.ToString();
            _cachedCharacterTitle.transform.SetAsFirstSibling();
        }

        private void UpdateCostumes(List<Costume> costumes)
        {
            if (costumes == null)
            {
                return;
            }

            foreach (var costume in costumes)
            {
                EquipCostume(costume);
            }

            var fullCostume = costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.FullCostume);
            if (fullCostume != null)
            {
                ChangeSpineObject(fullCostume.SpineResourcePath, true);
            }
        }

        private void EquipCostume(Costume costume)
        {
            if (costume is null)
            {
                return;
            }

            switch (costume.ItemSubType)
            {
                case ItemSubType.EarCostume:
                    UpdateEarById(costume.Id);
                    break;
                case ItemSubType.EyeCostume:
                    UpdateEyeById(costume.Id);
                    break;
                case ItemSubType.HairCostume:
                    UpdateHairById(costume.Id);
                    break;
                case ItemSubType.TailCostume:
                    UpdateTailById(costume.Id);
                    break;
                case ItemSubType.Title:
                    UpdateTitle(costume);
                    break;
                case ItemSubType.FullCostume:
                    // FullCostume is handled elsewhere
                    break;
           }
        }

        private void ChangeSpineObject(string spineResourcePath, bool isFullCostume, bool updateHitPoint = true)
        {
            if (!(_animator.Target is null))
            {
                var animatorTargetName = spineResourcePath.Split('/').Last();
                if (_animator.Target.name.Contains(animatorTargetName))
                {
                    return;
                }

                _animator.DestroyTarget();
            }

            var origin = Resources.Load<GameObject>(spineResourcePath);
            if (!origin)
            {
                throw new FailedToLoadResourceException<GameObject>(spineResourcePath);
            }

            var go = Instantiate(origin, gameObject.transform);
            SpineController = go.GetComponent<PlayerSpineController>();
            if (!isFullCostume)
            {
                SpineController.AttachTail();
            }

            _animator.ResetTarget(go);

            if (updateHitPoint)
            {
                UpdateHitPoint();
            }
        }

        private void UpdateHitPoint()
        {
            var source = GetAnimatorHitPointBoxCollider();
            if (!source)
            {
                throw new NullReferenceException(
                    $"{nameof(GetAnimatorHitPointBoxCollider)}() returns null.");
            }

            var scale = _animator.Target.transform.localScale;
            var center = source.center;
            var size = source.size;
            boxCollider.center =
                new Vector3(center.x * scale.x, center.y * scale.y, center.z * scale.z);
            boxCollider.size =
                new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);
        }

        protected BoxCollider GetAnimatorHitPointBoxCollider()
        {
            return SpineController.BoxCollider;
        }
    }
}
