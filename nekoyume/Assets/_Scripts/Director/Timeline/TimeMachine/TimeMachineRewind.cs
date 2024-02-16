using UnityEngine;

namespace Nekoyume
{
    public class TimeMachineRewind : MonoBehaviour
    {
        [SerializeField]
        private bool isRunning = false;
        public bool IsRunning
        {
            get => isRunning;
            set => isRunning = value;
        }
    }
}
