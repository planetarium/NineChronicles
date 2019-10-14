using UnityEngine;
using Nekoyume.Game;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.UI.Module
{
    public class BuffLayout : MonoBehaviour
    {
        public GameObject iconPrefab;

        private Transform _buffParent;
        private readonly List<BuffIcon> _pool = new List<BuffIcon>(10);

        public void Awake()
        {
            _buffParent = transform;
            CreateImage(10);
        }

        public void UpdateBuff(IEnumerable<Game.Buff> buffs)
        {
            var ordered = buffs
                .Where(buff => buff.remainedDuration > 0)
                .OrderBy(buff => buff.RowData.Id);
            
            foreach (var icon in _pool)
            {
                if (icon.image.enabled)
                    icon.Hide();
            }

            foreach (var buff in ordered)
            {
                var icon = GetDisabledIcon();
                icon.Show(buff);
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
