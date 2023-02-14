using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class PetSelectButton : MonoBehaviour
    {
        [SerializeField]
        private GameObject emptyObject;

        [SerializeField]
        private GameObject equippedObject;

        [SerializeField]
        private GameObject selectedObject;

        [SerializeField]
        private GameObject notificationObject;

        public void SetData(int? petId)
        {
            selectedObject.SetActive(false);

            if (!petId.HasValue)
            {
                emptyObject.SetActive(true);
                equippedObject.SetActive(false);
                notificationObject.SetActive(true);
                return;
            }

            emptyObject.SetActive(false);
            equippedObject.SetActive(true);
            notificationObject.SetActive(false);
        }
    }
}
