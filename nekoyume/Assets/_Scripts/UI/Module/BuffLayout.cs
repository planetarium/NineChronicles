using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Stat;

namespace Nekoyume.UI.Module
{
    public class BuffLayout : MonoBehaviour
    {
        public GameObject iconPrefab;
        private readonly HashSet<Buff> AddedBuffs = new HashSet<Buff>();
        public IReadOnlyDictionary<int, Buff> buffData = new Dictionary<int, Buff>();

        private Transform _buffParent;
        [SerializeField] private List<BuffIcon> pool = new List<BuffIcon>(10);

        public bool IsBuffAdded(StatType statType) => AddedBuffs.Any(buff => buff.RowData.StatModifier.StatType == statType);
        public bool HasBuff(StatType statType) => buffData.Any(buff => buff.Value.RowData.StatModifier.StatType == statType);

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
            foreach (var icon in pool.Where(icon => icon.image.enabled))
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
                if (!buffData.ContainsKey(buff.Key) || buffData[buff.Key].remainedDuration < buffs[buff.Key].remainedDuration)
                {
                    AddedBuffs.Add(buff.Value);
                }
            }

            buffData = buffs;

            var ordered = buffs.Values
                .Where(buff => buff.remainedDuration > 0)
                .OrderBy(buff => buff.RowData.Id);

            foreach (var buff in ordered)
            {
                var icon = GetDisabledIcon();
                icon.Show(buff, AddedBuffs.Contains(buff));
            }
        }

        private BuffIcon GetDisabledIcon()
        {
            var icon = pool.First(i => i.image.enabled == false);
            return icon;
        }
    }
}
