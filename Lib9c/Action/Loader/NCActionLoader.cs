using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.Loader;

namespace Nekoyume.Action.Loader
{
    /// <summary>
    /// An <see cref="IActionLoader"/> implementation for Nine Chronicles's BlockChain.
    /// This is a simple wrapper around a <see cref="TypedActionLoader"/> for loading
    /// all <see cref="IAction"/> classes within the same assembly as
    /// the <see cref="ActionBase"/> class, inheriting from the <see cref="ActionBase"/> class,
    /// and has an <see cref="ActionTypeAttribute"/> from <see cref="IValue"/>s
    /// </summary>
    /// <seealso cref="TypedActionLoader"/>
    public class NCActionLoader : IActionLoader
    {
        private readonly IActionLoader _actionLoader;

        public NCActionLoader()
        {
            _actionLoader = TypedActionLoader.Create(typeof(ActionBase).Assembly, typeof(ActionBase));
        }

        /// <inheritdoc cref="IActionLoader.LoadAction"/>
        public IAction LoadAction(long index, IValue value) => _actionLoader.LoadAction(index, value);
    }
}
