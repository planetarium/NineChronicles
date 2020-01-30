using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model
{
    public class EnemyPlayer: Player
    {
        public readonly string NameWithHash;
        public EnemyPlayer(AvatarState avatarState, Simulator simulator) : base(avatarState, simulator)
        {
            NameWithHash = avatarState.NameWithHash;
        }

        public EnemyPlayer(AvatarState avatarState, TableSheets tableSheets) : base(avatarState, tableSheets)
        {
        }

        public EnemyPlayer(int level, TableSheets tableSheets) : base(level, tableSheets)
        {
        }

        private EnemyPlayer(EnemyPlayer value) : base(value)
        {
            NameWithHash = value.NameWithHash;
        }

        public override void Spawn()
        {
            InitAI();
            var spawn = new SpawnEnemyPlayer((CharacterBase) Clone());
            Simulator.Log.Add(spawn);
        }

        public override object Clone()
        {
            return new EnemyPlayer(this);
        }

        protected override void OnDead()
        {
            base.OnDead();
            var player = (Player) Targets[0];
            player.RemoveTarget(this);
        }
    }
}
