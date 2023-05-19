namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Lib9c.DevExtensions;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.Loader;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class ActionTypesTest
    {
        private readonly ITestOutputHelper _output;

        public ActionTypesTest(ITestOutputHelper outputHelper)
        {
            _output = outputHelper;
        }

        [Fact]
        public void TypesExecute()
        {
            Type actionBaseType = typeof(ActionBase);
            Type gameActionType = typeof(GameAction);
            Assert.Equal(actionBaseType.Assembly, gameActionType.Assembly);
            var baseLoader = TypedActionLoader.Create(actionBaseType.Assembly, actionBaseType);
            var gameLoader = TypedActionLoader.Create(gameActionType.Assembly, gameActionType);
            var devBaseLoader = TypedActionLoader.Create(typeof(Utils).Assembly, actionBaseType);
            var devGameLoader = TypedActionLoader.Create(typeof(Utils).Assembly, gameActionType);

            Assert.Equal(devBaseLoader.Types.Count(), devGameLoader.Types.Count());
            var nonGameTypes = baseLoader.Types.Where(kv => !gameLoader.Types.ContainsKey(kv.Key));
            foreach (var typeMap in nonGameTypes)
            {
                _output.WriteLine((Bencodex.Types.Text)typeMap.Key);
            }

            Assert.NotEqual(baseLoader.Types.Count(), gameLoader.Types.Count());
        }
    }
}
