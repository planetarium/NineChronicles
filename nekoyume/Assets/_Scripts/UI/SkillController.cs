using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Nekoyume.UI
{
    public class SkillController : Widget
    {
        [SerializeField]
        private Transform _grid;
        [SerializeField]
        private GameObject _slotBase;
        private Game.Character.Player _player;
        private Game.Skill.SkillBase[] _skills;

        private List<GameObject> _slots;

        private void Awake()
        {
            Game.Event.OnUseSkill.AddListener(OnUseSkill);
        }

        private void OnUseSkill()
        {
            for (int i = 0; i < _skills.Length; ++i)
            {
                GameObject slot = _slots[i];
                var image = slot.transform.GetChild(0).gameObject.GetComponent<UnityEngine.UI.Image>();
                image.fillAmount = 0.0f;
                image.DOFillAmount(1.0f, _skills[i].GetCooltime());
            }
        }

        public void Show(GameObject player)
        {
            base.Show();
            StartCoroutine(CreateSlots(player));
        }

        private IEnumerator CreateSlots(GameObject player)
        {
            if (_slots == null)
                _slots = new List<GameObject>();

            foreach (GameObject slot in _slots)
            {
                Destroy(slot);
            }
            _slots.Clear();

            yield return null;

            _player = player.GetComponent<Game.Character.Player>();
            _skills = player.GetComponents<Game.Skill.SkillBase>();
            _slotBase.SetActive(true);
            for (int i = 0; i < _skills.Length; ++i)
            {
                GameObject newSlot = Instantiate(_slotBase, _grid);
                _slots.Add(newSlot);
            }
            _slotBase.SetActive(false);

            base.Show();
        }

        public void SlotClick(GameObject slot)
        {
            int index = _slots.IndexOf(slot);
            if (index >= 0)
            {
                _player.UseSkill(_skills[index], false);
            }
        }
    }
}
