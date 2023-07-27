using Libplanet.Action;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents;

public class Random : System.Random, IRandom
{
    public Random(int seed)
        : base(seed)
    {
        Seed = seed;
    }

    public int Seed { get; private set; }
}
