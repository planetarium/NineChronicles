namespace Lib9c.Tests
{
    using Lib9c.Formatters;
    using MessagePack;
    using MessagePack.Resolvers;
    using Nekoyume.Action;
    using Xunit;

    public static class ActionSerializer
    {
        static ActionSerializer()
        {
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                NineChroniclesResolver.Instance,
                StandardResolver.Instance
            );
            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePackSerializer.DefaultOptions = options;
        }

        public static T AssertAction<T>(ActionBase action)
            where T : ActionBase
        {
            var b = MessagePackSerializer.Serialize(action);
            var deserialized = MessagePackSerializer.Deserialize<ActionBase>(b);

            Assert.IsType<T>(deserialized);
            Assert.Equal(action.PlainValue, deserialized.PlainValue);
            return (T)deserialized;
        }
    }
}
