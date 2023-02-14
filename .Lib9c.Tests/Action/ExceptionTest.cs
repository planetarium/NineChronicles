namespace Lib9c.Tests.Action
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Lib9c.Formatters;
    using Libplanet;
    using MessagePack;
    using MessagePack.Resolvers;
    using Nekoyume.Action;
    using Nekoyume.Exceptions;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class ExceptionTest
    {
        public ExceptionTest()
        {
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                NineChroniclesResolver.Instance,
                StandardResolver.Instance
            );
            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePackSerializer.DefaultOptions = options;
        }

        [Theory]
        [InlineData(typeof(InvalidTradableIdException))]
        [InlineData(typeof(AlreadyReceivedException))]
        [InlineData(typeof(ArenaNotEndedException))]
        [InlineData(typeof(AvatarIndexAlreadyUsedException))]
        [InlineData(typeof(FailedLoadStateException))]
        [InlineData(typeof(InvalidNamePatternException))]
        [InlineData(typeof(CombinationSlotResultNullException))]
        [InlineData(typeof(CombinationSlotUnlockException))]
        [InlineData(typeof(NotEnoughMaterialException))]
        [InlineData(typeof(InvalidPriceException))]
        [InlineData(typeof(ItemDoesNotExistException))]
        [InlineData(typeof(EquipmentLevelExceededException))]
        [InlineData(typeof(DuplicateMaterialException))]
        [InlineData(typeof(InvalidMaterialException))]
        [InlineData(typeof(ConsumableSlotOutOfRangeException))]
        [InlineData(typeof(ConsumableSlotUnlockException))]
        [InlineData(typeof(InvalidItemTypeException))]
        [InlineData(typeof(InvalidRedeemCodeException))]
        [InlineData(typeof(DuplicateRedeemException))]
        [InlineData(typeof(SheetRowValidateException))]
        [InlineData(typeof(ShopItemExpiredException))]
        [InlineData(typeof(InvalidMonsterCollectionRoundException))]
        [InlineData(typeof(MonsterCollectionExpiredException))]
        [InlineData(typeof(InvalidLevelException))]
        [InlineData(typeof(ActionPointExceededException))]
        [InlineData(typeof(InvalidItemCountException))]
        [InlineData(typeof(DuplicateOrderIdException))]
        [InlineData(typeof(OrderIdDoesNotExistException))]
        [InlineData(typeof(ActionObsoletedException))]
        [InlineData(typeof(FailedLoadSheetException))]
        [InlineData(typeof(InvalidEquipmentException))]
        [InlineData(typeof(AlreadyRecipeUnlockedException))]
        [InlineData(typeof(InvalidRecipeIdException))]
        [InlineData(typeof(AlreadyWorldUnlockedException))]
        [InlineData(typeof(InvalidActionFieldException))]
        [InlineData(typeof(NotEnoughEventDungeonTicketsException))]
        [InlineData(typeof(InvalidClaimException))]
        [InlineData(typeof(RequiredBlockIntervalException))]
        [InlineData(typeof(ActionUnavailableException))]
        [InlineData(typeof(InvalidTransferCurrencyException))]
        public void Exception_Serializable(Type excType)
        {
            if (Activator.CreateInstance(excType, "for testing") is Exception exc)
            {
                AssertException(excType, exc);
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        [Fact]
        public void AdminPermissionExceptionSerializable()
        {
            var policy = new AdminState(default, 100);
            var address = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var exc = new PermissionDeniedException(policy, address);
            AssertException<PermissionDeniedException>(exc);
            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, exc);

                ms.Seek(0, SeekOrigin.Begin);
                var deserialized = (PermissionDeniedException)formatter.Deserialize(ms);
                AssertAdminState(exc.Policy, deserialized.Policy);
                Assert.Equal(exc.Signer, deserialized.Signer);
            }

            var exc2 = new PolicyExpiredException(policy, 101);
            AssertException<PolicyExpiredException>(exc2);
            var formatter2 = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter2.Serialize(ms, exc2);

                ms.Seek(0, SeekOrigin.Begin);
                var deserialized = (PolicyExpiredException)formatter2.Deserialize(ms);
                AssertAdminState(exc2.Policy, deserialized.Policy);
                Assert.Equal(exc2.BlockIndex, deserialized.BlockIndex);
            }
        }

        private static void AssertException<T>(Exception exc)
            where T : Exception
        {
            AssertException(typeof(T), exc);
        }

        private static void AssertException(Type type, Exception exc)
        {
            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, exc);
                ms.Seek(0, SeekOrigin.Begin);
                var deserialized = formatter.Deserialize(ms);
                Exception exception = (Exception)Convert.ChangeType(deserialized, type);
                Assert.Equal(exc.Message, exception.Message);
            }

            var b = MessagePackSerializer.Serialize(exc);
            var des = MessagePackSerializer.Deserialize<Exception>(b);
            Assert.Equal(exc.Message, des.Message);
        }

        private static void AssertAdminState(AdminState adminState, AdminState adminState2)
        {
            Assert.Equal(adminState.AdminAddress, adminState2.AdminAddress);
            Assert.Equal(adminState.address, adminState2.address);
            Assert.Equal(adminState.ValidUntil, adminState2.ValidUntil);
        }
    }
}
