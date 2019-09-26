using UnityEngine.UI;
using UnityEngine;
using Nekoyume.Game;
using System.Collections.Generic;
using Nekoyume.TableData;

namespace Nekoyume.UI
{
    public class BuffLayout : MonoBehaviour
    {
        public GameObject iconPrefab;

        private Transform _buffParent;
        private readonly Dictionary<int, Image> _images = new Dictionary<int, Image>();
        private readonly List<Image> _pool = new List<Image>(10);
        private readonly List<int> _addList = new List<int>(10);
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
            _deleteList.AddRange(_images.Keys);

            foreach(var buff in buffs)
            {
                int id = buff.data.id;
                if (!_images.ContainsKey(id))
                    _addList.Add(id);
                else
                    _deleteList.Remove(id);
            }

            AddBuff(_addList);
            DeleteBuff(_deleteList);
        }

        public void AddBuff(IEnumerable<int> buffs)
        {
            foreach (var id in buffs)
            {
                if (id <= 0 || _images.ContainsKey(id)) continue;
                var img = GetUnenabledImage();
                img.enabled = true;
                SetSprite(img, id);
                _images.Add(id, img);
            }
        }

        public void DeleteBuff(IEnumerable<int> buffs)
        {
            foreach (var id in buffs)
            {
                if (id <= 0 || !_images.ContainsKey(id)) continue;
                _images[id].overrideSprite = null;
                _images[id].enabled = false;
                _images.Remove(id);
            }
        }

        private void CreateImage(int count)
        {
            iconPrefab.SetActive(true);
            for (int i = 0; i < count; ++i)
            {
                var img = Instantiate(iconPrefab, _buffParent).GetComponent<Image>();
                img.enabled = false;
                _pool.Add(img);
            }
            iconPrefab.SetActive(false);
        }

        private Image GetUnenabledImage()
        {
            foreach (var image in _pool)
                if (!image.enabled)
                    return image;

            return null;
        }

        private void SetSprite(Image img, int id)
        {
            if (id == 0)
            {
                img.overrideSprite = null;
                return;
            }
            var sprite = BuffSheetRowExtension.GetIcon(id);
            img.overrideSprite = sprite;
        }
    }
}
