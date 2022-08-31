using Nekoyume.Model.Buff;
using Nekoyume.Model.Stat;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class BuffLayout : MonoBehaviour
    {
        public GameObject iconPrefab;
        private readonly HashSet<Buff> AddedBuffs = new HashSet<Buff>();
        public IReadOnlyDictionary<int, Buff> buffData = new Dictionary<int, Buff>();

        private Transform _buffParent;
        [SerializeField] private List<BuffIcon> pool = new List<BuffIcon>(10);

        public bool IsBuffAdded(StatType statType) => AddedBuffs.Any(buff =>
        {
            if (buff is not StatBuff stat)
            {
                return false;
            }

            return stat.RowData.StatModifier.StatType == statType;
        });

        public bool HasBuff(StatType statType) => buffData.Values.Any(buff =>
        {
            if (buff is not StatBuff stat)
            {
                return false;
            }

            return stat.RowData.StatModifier.StatType == statType;
        });

        public void Awake()
        {
            _buffParent = transform;
        }

        private void OnDisable()
        {
            foreach (var icon in pool)
            {
                icon.Hide();
            }
        }

        public void SetBuff(IReadOnlyDictionary<int, Buff> buffs)
        {
            foreach (var icon in pool.Where(icon => icon.gameObject.activeSelf))
            {
                icon.Hide();
            }

            if (buffs is null)
            {
                return;
            }

            AddedBuffs.Clear();
            foreach (var buff in buffs)
            {
                if (!buffData.ContainsKey(buff.Key) || buffData[buff.Key].RemainedDuration < buffs[buff.Key].RemainedDuration)
                {
                    AddedBuffs.Add(buff.Value);
                }
            }

            buffData = buffs;

            var ordered = buffs.Values
                .Where(buff => buff.RemainedDuration > 0)
                .OrderBy(buff => buff.BuffInfo.Id);

            foreach (var buff in ordered)
            {
                var icon = GetDisabledIcon();
                icon.Show(buff, AddedBuffs.Contains(buff));
            }
        }

        private BuffIcon GetDisabledIcon()
        {
            var icon = pool.First(i => !i.gameObject.activeSelf);
            return icon;
        }
    }
}
