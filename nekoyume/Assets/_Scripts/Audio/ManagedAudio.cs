using System;
using Nekoyume.Game.Controller;
using UnityEngine;

namespace Nekoyume.Audio
{
    public class ManagedAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

        public void Play()
        {
            audioSource ??= GetComponent<AudioSource>();
            AudioController.instance.PlaySfx(audioSource.clip.name);
        }
    }
}
