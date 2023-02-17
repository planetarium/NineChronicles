using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "UI_PetData", menuName = "Scriptable Object/Pet Data",
        order = int.MaxValue)]
    public class PetScriptableObject : ScriptableObject
    {
        [SerializeField]
        private List<PetData> dataList;

        [SerializeField]
        private int defaultPetId;

        public PetData GetPetData(int id)
        {
            return dataList.FirstOrDefault(x => x.id == id) ??
                dataList[defaultPetId];
        }

        [Serializable]
        public class PetData
        {
            public int id;
            public Sprite icon;
        }
    }
}
