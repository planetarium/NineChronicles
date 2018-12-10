namespace Nekoyume.Data.Table
{
    public class Skill : Row
    {
        public string Id = "";
        public string Cls = "";
        public int Power = 0;
        public int Range = 0;
        public int Size = 0;
        public AttackType AttackType = AttackType.Light;
        public int ElementalType = 0;
        public int Cooltime = 0;
        public int CastingTime = 0;
        public int TargetCount = 0;
        public int Cost = 0;
    }
}
