namespace Nekoyume.Data.Table
{
    public class MonsterAppear : Row
    {
        public int Id = 0;
        public int Zone = 0;
        public int MonsterId = 0;
        public int Weight = 0;
        // calc_weight = weight - (weight_stage * stage)
        public int WeightStage = 0;
    }
}
