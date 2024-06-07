using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model.Buff;
using UnityEngine;

namespace Nekoyume.Game.VFX
{
    public class FrostBiteVFX : PersistingVFX
    {
        public enum FrostBiteLevel
        {
            NonInitialized,
            Level1,
            Level2,
            Level3,
        }

        protected override ParticleSystem ParticlesRoot
        {
            get => base.ParticlesRoot;
            set
            {
                base.ParticlesRoot = value;
                if (ParticlesRoot != null)
                {
                    var main = ParticlesRoot.main;
                    main.loop = true;
                }
            }
        }

        public FrostBiteLevel Level
        {
            get => _level;
            set
            {
                if (_level == value)
                {
                    return;
                }

                _level = value;
                switch (_level)
                {
                    case FrostBiteLevel.Level1:
                        ParticlesRoot = frostBiteLevel1;
                        break;
                    case FrostBiteLevel.Level2:
                        ParticlesRoot = frostBiteLevel2;
                        break;
                    case FrostBiteLevel.Level3:
                        ParticlesRoot = frostBiteLevel3;
                        break;
                }

                if (_isPlaying)
                {
                    ChangePlayParticle(_level);
                }
            }
        }

        private FrostBiteLevel _level = FrostBiteLevel.NonInitialized;

        [SerializeField]
        private ParticleSystem frostBiteLevel1 = null;

        [SerializeField]
        private ParticleSystem frostBiteLevel2 = null;

        [SerializeField]
        private ParticleSystem frostBiteLevel3 = null;

        public override void Awake()
        {
            base.Awake();
            Level = FrostBiteLevel.Level1;
            ParticlesRoot = frostBiteLevel1;
        }

        public override void Play()
        {
            base.Play();
            Level = FrostBiteLevel.Level1;
            if (Target != null)
            {
                Target.AddFrostbiteColor();
            }
        }

        public override void LazyStop()
        {
            base.LazyStop();
            if (Target != null)
            {
                Target.RemoveFrostbiteColor();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Level = FrostBiteLevel.NonInitialized;
        }

        // TODO: 이벤트 기반으로 변경
        public void Update()
        {
            if (Target == null)
            {
                return;
            }

            Level = GetFrostBiteLevel(Buff);
        }

        private FrostBiteLevel GetFrostBiteLevel(Buff buff)
        {
            if (buff is not StatBuff statBuff)
            {
                return FrostBiteLevel.Level1;
            }

            switch (statBuff.Stack)
            {
                case >= 4:
                    return FrostBiteLevel.Level3;
                case >= 2:
                    return FrostBiteLevel.Level2;
                default:
                    return FrostBiteLevel.Level1;
            }
        }

        private void ChangePlayParticle(FrostBiteLevel level)
        {
            switch (level)
            {
                case FrostBiteLevel.Level1:
                    frostBiteLevel1.gameObject.SetActive(true);
                    frostBiteLevel1.Play();

                    frostBiteLevel2.gameObject.SetActive(false);
                    frostBiteLevel2.Stop();

                    frostBiteLevel3.gameObject.SetActive(false);
                    frostBiteLevel3.Stop();
                    break;
                case FrostBiteLevel.Level2:
                    frostBiteLevel2.gameObject.SetActive(true);
                    frostBiteLevel2.Play();

                    frostBiteLevel1.gameObject.SetActive(false);
                    frostBiteLevel1.Stop();

                    frostBiteLevel3.gameObject.SetActive(false);
                    frostBiteLevel3.Stop();
                    break;
                case FrostBiteLevel.Level3:
                    frostBiteLevel3.gameObject.SetActive(true);
                    frostBiteLevel3.Play();

                    frostBiteLevel1.gameObject.SetActive(false);
                    frostBiteLevel1.Stop();

                    frostBiteLevel2.gameObject.SetActive(false);
                    frostBiteLevel2.Stop();
                    break;
            }
        }
    }
}
