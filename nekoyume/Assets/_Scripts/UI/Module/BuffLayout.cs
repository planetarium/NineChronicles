using UnityEngine;
using Nekoyume.Game;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;

namespace Nekoyume.UI.Module
{
    public class BuffLayout : MonoBehaviour
    {
        public GameObject iconPrefab;
        public readonly HashSet<Buff> addedBuffs = new HashSet<Buff>();
        public IReadOnlyDictionary<int, Buff> buffData = new Dictionary<int, Buff>();

        private Transform _buffParent;
        private readonly List<BuffIcon> _pool = new List<BuffIcon>(10);

        public bool IsBuffAdded(StatType statType) => addedBuffs.Any(buff => buff.RowData.StatModifier.StatType == statType);
        public bool HasBuff(StatType statType) => buffData.Any(buff => buff.Value.RowData.StatModifier.StatType == statType);

        public void Awake()
        {
            _buffParent = transform;
            CreateImage(10);
        }

        public void SetBuff(IReadOnlyDictionary<int, Game.Buff> buffs)
        {
            foreach (var icon in _pool)
            {
                if (icon.image.enabled)
                    icon.Hide();
            }

            if (buffs is null)
            {
                return;
            }

            addedBuffs.Clear();
            foreach (var buff in buffs)
            {
                if (!buffData.ContainsKey(buff.Key) || buffData[buff.Key].remainedDuration < buffs[buff.Key].remainedDuration)
                {
                    addedBuffs.Add(buff.Value);
                }
            }

            buffData = buffs;

            var ordered = buffs.Values
                .Where(buff => buff.remainedDuration > 0)
                .OrderBy(buff => buff.RowData.Id);

            foreach (var buff in ordered)
            {
                var icon = GetDisabledIcon();
                icon.Show(buff, addedBuffs.Contains(buff));
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
