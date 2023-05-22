using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Nekoyume.Action;
using Nekoyume.Action.Loader;

namespace Lib9c.DevExtensions.Action.Loader
{
    /// <summary>
    /// An <see cref="IActionLoader"/> implementation for Nine Chronicles's BlockChain.
    /// Similar to <see cref="NCActionLoader"/>, but this one also includes <see cref="IAction"/>
    /// classes inside the <see cref="Lib9c.DevExtensions"/> assembly.
    /// </summary>
    /// <seealso cref="NCActionLoader"/>
    /// <seealso cref="TypedActionLoader"/>
    public class NCDevActionLoader : IActionLoader
    {
        private readonly IActionLoader _actionLoader;

        public NCDevActionLoader()
        {
            var loader = TypedActionLoader.Create(typeof(ActionBase).Assembly, typeof(ActionBase));
            var devLoader = TypedActionLoader.Create(typeof(Utils).Assembly, typeof(ActionBase));
            _actionLoader = new TypedActionLoader(loader.Types.Union(devLoader.Types).ToDictionary(kv => kv.Key, kv => kv.Value));
        }

        /// <inheritdoc cref="IActionLoader.LoadAction"/>
        public IAction LoadAction(long index, IValue value) => _actionLoader.LoadAction(index, value);
    }
}
