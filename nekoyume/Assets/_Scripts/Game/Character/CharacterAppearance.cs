using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Libplanet;
using Nekoyume.EnumType;
using Nekoyume.Game.Avatar;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI;
using Nekoyume.UI.Model;
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

        private void OnDisable()
        {
            Destroy(_cachedCharacterTitle);
        }

        public void Set(
            ArenaPlayerDigest digest,
            Address avatarAddress,
            CharacterAnimator animator,
            HudContainer hudContainer)
        {
            var armor = (Armor)digest.Equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
            var weapon = (Weapon)digest.Equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Weapon);

            UpdateAvatar(avatarAddress, animator, hudContainer,
                digest.Costumes, armor, weapon,
                digest.EarIndex, digest.LensIndex, digest.HairIndex, digest.TailIndex);
        }

        public void Set(
            Address avatarAddress,
            CharacterAnimator animator,
            HudContainer hudContainer,
            List<Costume> costumes,
            List<Equipment> equipments,
            int earIndex,
            int lensIndex,
            int hairIndex,
            int tailIndex)
        {
            var armor = (Armor)equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
            var weapon = (Weapon)equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Weapon);

            UpdateAvatar(avatarAddress, animator, hudContainer,
                costumes, armor, weapon,
                earIndex, lensIndex, hairIndex, tailIndex);
        }

        public void Set(
            Address avatarAddress,
            CharacterAnimator animator,
            HudContainer hudContainer,
            List<Costume> costumes,
            Armor armor,
            Weapon weapon,
            int earIndex,
            int lensIndex,
            int hairIndex,
            int tailIndex)
        {
            UpdateAvatar(avatarAddress, animator, hudContainer,
                costumes, armor, weapon,
                earIndex, lensIndex, hairIndex, tailIndex);
        }

        public void SetForPrologue(
            CharacterAnimator animator,
            HudContainer hudContainer,
            int armorId,
            int weaponId,
            int earIndex,
            int lensIndex,
            int hairIndex,
            int tailIndex)
        {
            _animator = animator;
            _hudContainer = hudContainer;

            SpineController.UnequipFullCostume();
            UpdateEar(earIndex, false);
            UpdateFace(lensIndex, false);
            UpdateHair(hairIndex, false);
            UpdateTail(tailIndex, false);
            UpdateAcFace(0, false);
            UpdateAcEye(0, false);
            UpdateAcHead(0, false);
            SpineController.UpdateBody(armorId, 0);
            SpineController.UpdateWeapon(weaponId, null);
            UpdateTarget();
            UpdateHitPoint();
        }

        private void UpdateAvatar(
            Address avatarAddress,
            CharacterAnimator animator,
            HudContainer hudContainer,
            List<Costume> costumes,
            Armor armor,
            Weapon weapon,
            int earIndex,
            int lensIndex,
            int hairIndex,
            int tailIndex)
        {
            _animator = animator;
            _hudContainer = hudContainer;
            Destroy(_cachedCharacterTitle);

            var isDcc = Game.instance.Dcc.IsActive(avatarAddress, out var id, out var isVisible);
            if (isDcc && States.Instance.CurrentAvatarState is not null &&
                avatarAddress == States.Instance.CurrentAvatarState.address)
            {
                isDcc = isVisible;
            }

            if (isDcc)
            {
                var parts = GetDccParts(id);
                // ignore full costume
                SpineController.UnequipFullCostume();
                UpdateEar(parts[DccPartsType.ear_tail], true);
                UpdateFace(parts[DccPartsType.face], true);
                UpdateHair(parts[DccPartsType.hair], true);
                UpdateTail(parts[DccPartsType.ear_tail], true);
                UpdateAcFace(parts[DccPartsType.ac_face], true);
                UpdateAcEye(parts[DccPartsType.ac_eye], true);
                UpdateAcHead(parts[DccPartsType.ac_head], true);
                UpdateArmor(armor);
                UpdateWeapon(weapon);
            }
            else
            {
                var fullCostume =
                    costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.FullCostume);
                if (fullCostume is not null)
                {
                    UpdateFullCostume(fullCostume);
                }
                else
                {
                    SpineController.UnequipFullCostume();
                    UpdateEar(earIndex, false);
                    UpdateFace(lensIndex, false);
                    UpdateHair(hairIndex, false);
                    UpdateTail(tailIndex, false);
                    UpdateAcFace(0, false);
                    UpdateAcEye(0, false);
                    UpdateAcHead(0, false);
                    UpdateArmor(armor);
                    UpdateWeapon(weapon);
                }
            }

            var title = costumes.FirstOrDefault(x => x.ItemSubType == ItemSubType.Title);
            if (title is not null)
            {
                UpdateTitle(title);
            }
        }

        private Dictionary<DccPartsType, int> GetDccParts(int id)
        {
            var dccParts = new Dictionary<DccPartsType, int>();
            var url = $"{Game.instance.URL.DccMetadata}{id}.json";
            StartCoroutine(RequestManager.instance.GetJson(url, (json) =>
            {
                var result = JsonSerializer.Deserialize<DccMetadata>(json);
                dccParts.Add(DccPartsType.background, result.traits[0]);
                dccParts.Add(DccPartsType.skin, result.traits[1]);
                dccParts.Add(DccPartsType.face, result.traits[2]);
                dccParts.Add(DccPartsType.ear_tail, result.traits[3]);
                dccParts.Add(DccPartsType.ac_face, result.traits[4]);
                dccParts.Add(DccPartsType.hair, result.traits[5]);
                dccParts.Add(DccPartsType.ac_eye, result.traits[6]);
                dccParts.Add(DccPartsType.ac_head, result.traits[7]);
            }));

            return dccParts;
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

        private void UpdateArmor(Armor armor)
        {
            var armorId = armor?.Id ?? GameConfig.DefaultAvatarArmorId;
            SpineController.UpdateBody(armorId, 0);
            UpdateTarget();
            UpdateHitPoint();
        }

        private void UpdateWeapon(Weapon weapon)
        {
            var weaponId = weapon?.Id ?? GameConfig.DefaultAvatarWeaponId;
            var level = weapon?.level ?? 0;
            var vfx = ResourcesHelper.GetAuraWeaponPrefab(weaponId, level);
            SpineController.UpdateWeapon(weaponId, vfx);
        }

        private void UpdateEar(int index, bool isDcc)
        {
            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            var row = sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.EarCostume);
            var id = row.Id + index;
            SpineController.UpdateEar(id, isDcc);
        }

        private void UpdateFace(int index, bool isDcc)
        {
            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            var row = sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.EyeCostume);
            var id = row.Id + index;
            SpineController.UpdateFace(id, isDcc);
        }

        private void UpdateHair(int index, bool isDcc)
        {
            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            var row = sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.HairCostume);
            var id = row.Id + index;
            SpineController.UpdateHair(id, isDcc);
        }

        private void UpdateTail(int index, bool isDcc)
        {
            var sheet = Game.instance.TableSheets.CostumeItemSheet;
            var row = sheet.OrderedList.FirstOrDefault(row => row.ItemSubType == ItemSubType.TailCostume);
            var id = row.Id + index;
            SpineController.UpdateTail(id, isDcc);
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
