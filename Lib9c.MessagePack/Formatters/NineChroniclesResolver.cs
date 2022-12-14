using MessagePack;
using MessagePack.Formatters;

namespace Lib9c.Formatters
{
    public class NineChroniclesResolver : IFormatterResolver
    {
        // Resolver should be singleton.
        public static readonly IFormatterResolver Instance = new NineChroniclesResolver();

        private NineChroniclesResolver()
        {
        }

        // GetFormatter<T>'s get cost should be minimized so use type cache.
        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            // generic's static constructor should be minimized for reduce type generation size!
            // use outer helper method.
#pragma warning disable S3963
            static FormatterCache()
            {
                Formatter =
                    (IMessagePackFormatter<T>?)(NineChroniclesResolverGetFormatterHelper
                        .GetFormatter(typeof(T)));
            }
#pragma warning restore S3963
        }
    }
}
