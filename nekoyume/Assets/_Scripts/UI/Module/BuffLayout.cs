using UnityEngine.UI;
using UnityEngine;
using Nekoyume.Game;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.Model;

namespace Nekoyume.UI.Module
{
    public class BuffLayout : MonoBehaviour
    {
        public GameObject iconPrefab;

        private Transform _buffParent;
        private readonly Dictionary<int, BuffIcon> _icons = new Dictionary<int, BuffIcon>();
        private readonly List<BuffIcon> _pool = new List<BuffIcon>(10);
        private readonly List<Buff> _addList = new List<Buff>(10);
        private readonly List<int> _deleteList = new List<int>(10);

        public void Awake()
        {
            _buffParent = transform;
            CreateImage(10);
        }

        public void UpdateBuff(IEnumerable<Buff> buffs)
        {
            _addList.Clear();
            _deleteList.Clear();
            _deleteList.AddRange(_icons.Keys);

            foreach(var buff in buffs)
            {
                int id = buff.Data.Id;
                if (!_icons.ContainsKey(id))
                    _addList.Add(buff);
                else
                    _deleteList.Remove(id);
            }

            AddBuff(_addList);
            DeleteBuff(_deleteList);
            UpdateStatus();
        }

        public void AddBuff(IEnumerable<Buff> buffs)
        {
            foreach (var buff in buffs)
            {
                int id = buff.Data.Id;
                if (id <= 0 || _icons.ContainsKey(id)) continue;
                var icon = GetDisabledIcon();
                icon.Show(buff);
                _icons.Add(id, icon);
            }
        }

        public void DeleteBuff(IEnumerable<int> buffs)
        {
            foreach (var id in buffs)
            {
                if (id <= 0 || !_icons.ContainsKey(id)) continue;
                _icons[id].Hide();
                _icons.Remove(id);
            }
        }

        public void UpdateStatus()
        {
            foreach (var icon in _icons.Values)
            {
                if (icon.enabled)
                    icon.UpdateStatus();
            }
        }

        private void CreateImage(int count)
        {
            iconPrefab.SetActive(true);
            for (int i = 0; i < count; ++i)
            {
                var icon = Instantiate(iconPrefab, _buffParent).GetComponent<BuffIcon>();
                icon.image.enabled = false;
                _pool.Add(icon);
            }
            iconPrefab.SetActive(false);
        }

        private BuffIcon GetDisabledIcon()
        {
            foreach (var icon in _pool)
                if (!icon.image.enabled)
                    return icon;

            CreateImage(5);
            return GetDisabledIcon();
        }
    }
}
