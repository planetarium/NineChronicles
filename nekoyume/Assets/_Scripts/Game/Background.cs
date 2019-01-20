using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Nekoyume.Game
{
    public class Background : MonoBehaviour
    {
        public bool autoParallaxSize = true;
        public float parallaxSize = 0.0f;
        public float parallaxSpeed = 0.0f;
        private Transform cameraTransform;
        
        private Transform[] images;

        private float lastCameraX;
        private int leftIndex = 0;
        private int rightIndex = 0;

        private void Start ()
        {
            cameraTransform = Camera.main.transform;

            images = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; ++i)
            {
                images[i] = transform.GetChild(i);
                if (autoParallaxSize && i == 0)
                {
                    SpriteRenderer spriteRenderer = images[i].GetComponent<SpriteRenderer>();
                    if (spriteRenderer.sprite != null)
                    {
                        parallaxSize = spriteRenderer.sprite.texture.width / spriteRenderer.sprite.pixelsPerUnit;
                    }
                }
                images[i].position = new Vector3(parallaxSize * i, images[i].position.y, images[i].position.z);
            }

            lastCameraX = cameraTransform.position.x;
            leftIndex = 0;
            rightIndex = images.Length - 1;
        }

        private void Update()
        {
            if (parallaxSpeed != 0.0f)
            {
                float deltaX = cameraTransform.position.x - lastCameraX;
                float tempSpeedMultiplier = 3.0f; // Use temp value before adjusting in detail
                transform.position += Vector3.right * (deltaX * parallaxSpeed * tempSpeedMultiplier);
                lastCameraX = cameraTransform.position.x;
            }

            if (images.Length > 1)
            {
                if (cameraTransform.position.x < (images[leftIndex].transform.position.x))
                    MoveLeft();

                if (cameraTransform.position.x > (images[rightIndex].transform.position.x))
                    MoveRight();
            }
        }

        private void MoveLeft()
        {
            Vector3 position = Vector3.right * (images[leftIndex].position.x - parallaxSize);
            position.y = images[rightIndex].position.y;
            images[rightIndex].position = position;
            leftIndex = rightIndex;
            rightIndex--;
            if (rightIndex < 0)
                rightIndex = images.Length - 1;
        }

        private void MoveRight()
        {
            Vector3 position = Vector3.right * (images[rightIndex].position.x + parallaxSize);
            position.y = images[leftIndex].position.y;
            images[leftIndex].position = position;
            rightIndex = leftIndex;
            leftIndex++;
            if (leftIndex == images.Length)
                leftIndex = 0;
        }
    }
}
