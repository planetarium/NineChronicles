using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Background : MonoBehaviour
    {
        public Transform[] backgrounds;
        private Transform cameraTransform;

        private void Start ()
        {
            cameraTransform = Camera.main.transform;
        }

        public void Update()
        {
            for (int i = 0; i < backgrounds.Length; i++)
            {
                // TODO
                var backgroundPos = backgrounds[i].position;
                backgroundPos.x = cameraTransform.position.x;
                backgrounds[i].position = backgroundPos;
            }
        }
    }
}
