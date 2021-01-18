using System;
using Nekoyume.Pattern;
using UnityEngine;

namespace Nekoyume.Game
{
    public class StageConfig : MonoSingleton<StageConfig>
    {
        [Serializable]
        public class DropItemOptions
        {
            [Range(0.01f, 1)] [Tooltip("Fade in duration")]
            public float fadeInTime = 0.3f;
            [Range(0.01f, 1)] [Tooltip("Drop duration")]
            public float dropTime = 0.3f;
            [Range(0.01f, 1)] [Tooltip("Animation end time")]
            public float endDelay = 0.2f;
            [Range(0.01f, 2)] [Tooltip("Item Scale up duration")]
            public float scaleUpTime = 1.0f;
            [Range(0.01f, 2)] [Tooltip("Item Scale out duration")]
            public float scaleOutTime = 1.0f;
        }

        [Range(0.01f, 1)] [Tooltip("Execute action delay")]
        public float actionDelay = 0.5f;

        [Range(0.01f, 3)] [Tooltip("Stage Enter delay")]
        public float stageEnterDelay = 2.0f;

        [Range(0.01f, 1)] [Tooltip("Spawn wave delay")]
        public float spawnWaveDelay = 0.3f;

        [Tooltip("DropItem options")]
        public DropItemOptions droopItemOptions = new DropItemOptions();
    }
}
