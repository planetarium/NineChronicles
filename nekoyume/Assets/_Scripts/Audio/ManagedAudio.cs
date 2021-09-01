using System;
using Nekoyume.Game.Controller;
using UnityEngine;

namespace Nekoyume.Audio
{
    public class ManagedAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

        private void Reset()
        {
            audioSource ??= GetComponent<AudioSource>();
        }

        public void Play()
        {
            if (audioSource is null)
            {
                Reset();
            }
            
            AudioController.instance.PlaySfx(audioSource.clip.name);
        }
    }
}
