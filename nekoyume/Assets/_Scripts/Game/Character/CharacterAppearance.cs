using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Avatar;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class CharacterAppearance : MonoBehaviour
    {
        [SerializeField]
        private AvatarSpineController avatarSpineController;

        [SerializeField]
        private BoxCollider boxCollider;

        private CharacterAnimator _animator;
        private HudContainer _hudContainer;
        private GameObject _cachedCharacterTitle;
        private Costume _fullCostume;

        public AvatarSpineController SpineController => avatarSpineController;
        public BoxCollider BoxCollider => boxCollider;

        public void Set(
            ArenaPlayerDigest digest,
            CharacterAnimator animator,
            HudContainer hudContainer)
        {
            UpdateAvatar(animator, hudContainer, digest.Costumes, digest.Equipments,
                digest.EarIndex, digest.LensIndex, digest.HairIndex, digest.TailIndex);
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
            UpdateAvatar(animator, hudContainer, costumes, equipments, earIndex, lensIndex,
                hairIndex, tailIndex);
        }

        private void UpdateAvatar(
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
            Destroy(_cachedCharacterTitle);

            var fullCostume =
                costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.FullCostume);
            if (fullCostume is not null)
            {
                UpdateFullCostume(fullCostume);
            }
            else
            {
                SpineController.UnequipFullCostume();
                UpdateEar(earIndex);
                UpdateFace(lensIndex);
                UpdateHair(hairIndex);
                UpdateTail(tailIndex);
                UpdateAcFace(1, true);
                UpdateAcEye(1, true);
                UpdateAcHead(1, true);
                UpdateArmor(equipments);
                UpdateWeapon(equipments);
            }

            UpdateCostumes(costumes);
        }

        private void UpdateFullCostume(Costume fullCostume)
        {
            SpineController.UpdateFullCostume(fullCostume.Id);
            UpdateTarget();
            UpdateHitPoint();
        }

        private void UpdateAcFace(int index, bool isDcc)
        {
            SpineController.UpdateAcFace(index, isDcc);
        }

        private void UpdateAcEye(int index, bool isDcc)
        {
            SpineController.UpdateAcEye(index, isDcc);
        }

        private void UpdateAcHead(int index, bool isDcc)
        {
            SpineController.UpdateAcHead(index, isDcc);
        }

        private void UpdateArmor(IEnumerable<Equipment> equipments)
        {
            var armorId = GameConfig.DefaultAvatarArmorId;
            if (equipments is not null)
            {
                var armor = equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
                armorId = armor?.Id ?? GameConfig.DefaultAvatarArmorId;
            }

            SpineController.UpdateBody(armorId, 0);
            UpdateTarget();
            UpdateHitPoint();
        }

        private void UpdateWeapon(IEnumerable<Equipment> equipments)
        {
            var weaponId = GameConfig.DefaultAvatarWeaponId;
            GameObject vfx = null;
            if (equipments is not null)
            {
                var weapon =
                    (Weapon)equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Weapon);
                weaponId = weapon?.Id ?? GameConfig.DefaultAvatarWeaponId;
                var level = weapon?.level ?? 0;
                vfx = ResourcesHelper.GetAuraWeaponPrefab(weaponId, level);
            }

            SpineController.UpdateWeapon(weaponId, vfx);
        }

        private void UpdateEar(int index)
        {
            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            var row = sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.EarCostume);
            var id = row.Id + index;
            SpineController.UpdateEar(id, false);
        }

        private void UpdateFace(int index)
        {
            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            var row = sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.EyeCostume);
            var id = row.Id + index;
            SpineController.UpdateFace(id, false);
        }

        private void UpdateHair(int index)
        {
            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            var row = sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.HairCostume);
            var id = row.Id + index;
            SpineController.UpdateHair(id, false);
        }

        private void UpdateTail(int index)
        {
            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            var row = sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.TailCostume);
            var id = row.Id + index;
            Debug.Log("Update tail by lobby");
            SpineController.UpdateTail(id, false);
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
                    UpdateEar(costume.Id);
                    break;
                case ItemSubType.EyeCostume:
                    UpdateFace(costume.Id);
                    break;
                case ItemSubType.HairCostume:
                    UpdateHair(costume.Id);
                    break;
                case ItemSubType.TailCostume:
                    UpdateTail(costume.Id);
                    break;
                case ItemSubType.Title:
                    UpdateTitle(costume);
                    break;
                case ItemSubType.FullCostume:
                    // FullCostume is handled elsewhere
                    break;
            }
        }

        private void UpdateTarget()
        {
            var target = SpineController.gameObject;
            var animator = SpineController.GetComponent<Animator>();
            var sk = SpineController.GetSkeletonAnimation();
            var mr = sk.GetComponent<MeshRenderer>();
            _animator.InitTarget(target, mr, sk, animator);
        }

        private void UpdateHitPoint()
        {
            var source = SpineController.Collider;
            var scale = _animator.Target.transform.localScale;
            var center = source.center;
            var size = source.size;
            boxCollider.center =
                new Vector3(center.x * scale.x, center.y * scale.y, center.z * scale.z);
            boxCollider.size =
                new Vector3(size.x * scale.x, size.y * scale.y, size.z * scale.z);
        }
    }
}
