namespace Lib9c.Tests.Action
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Libplanet;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class ExceptionTest
    {
        [Fact]
        public void AlreadyReceivedExceptionSerializable()
        {
            var exc = new AlreadyReceivedException("for testing");
            AssertException<AlreadyReceivedException>(exc);
        }

        [Fact]
        public void ArenaNotEndedExceptionSerializable()
        {
            var exc = new ArenaNotEndedException("for testing");
            AssertException<ArenaNotEndedException>(exc);
        }

        [Fact]
        public void AvatarIndexAlreadyUsedExceptionSerializable()
        {
            var exc = new AvatarIndexAlreadyUsedException("for testing");
            AssertException<AvatarIndexAlreadyUsedException>(exc);
        }

        [Fact]
        public void FailedLoadStateExceptionSerializable()
        {
            var exc = new FailedLoadStateException("for testing");
            AssertException<FailedLoadStateException>(exc);
        }

        [Fact]
        public void InvalidNamePatternExceptionSerializable()
        {
            var exc = new InvalidNamePatternException("for testing");
            AssertException<InvalidNamePatternException>(exc);
        }

        [Fact]
        public void CombinationSlotResultNullExceptionSerializable()
        {
            var exc = new CombinationSlotResultNullException("fot testing");
            AssertException<CombinationSlotResultNullException>(exc);
        }

        [Fact]
        public void CombinationSlotUnlockExceptionSerializable()
        {
            var exc = new CombinationSlotUnlockException("for testing");
            AssertException<CombinationSlotUnlockException>(exc);
        }

        [Fact]
        public void NotEnoughMaterialExceptionSerializable()
        {
            var exc = new NotEnoughMaterialException("for testing");
            AssertException<NotEnoughMaterialException>(exc);
        }

        [Fact]
        public void InvalidPriceExceptionSerializable()
        {
            var exc = new InvalidPriceException("for testing");
            AssertException<InvalidPriceException>(exc);
        }

        [Fact]
        public void ItemDoesNotExistException()
        {
            var exc = new ItemDoesNotExistException("for testing");
            AssertException<ItemDoesNotExistException>(exc);
        }

        [Fact]
        public void EquipmentLevelExceededExceptionSerializable()
        {
            var exc = new EquipmentLevelExceededException("for testing");
            AssertException<EquipmentLevelExceededException>(exc);
        }

        [Fact]
        public void DuplicateMaterialExceptionSerializable()
        {
            var exc = new DuplicateMaterialException("for testing");
            AssertException<DuplicateMaterialException>(exc);
        }

        [Fact]
        public void InvalidMaterialExceptionSerializable()
        {
            var exc = new InvalidMaterialException("for testing");
            AssertException<InvalidMaterialException>(exc);
        }

        [Fact]
        public void ConsumableSlotOutOfRangeExceptionSerializable()
        {
            var exc = new ConsumableSlotOutOfRangeException();
            AssertException<ConsumableSlotOutOfRangeException>(exc);
        }

        [Fact]
        public void ConsumableSlotUnlockExceptionSerializable()
        {
            var exc = new ConsumableSlotUnlockException("for testing");
            AssertException<ConsumableSlotUnlockException>(exc);
        }

        [Fact]
        public void InvalidItemTypeExceptionSerializable()
        {
            var exc = new InvalidItemTypeException("for testing");
            AssertException<InvalidItemTypeException>(exc);
        }

        [Fact]
        public void InvalidRedeemCodeExceptionSerializable()
        {
            var exc = new InvalidRedeemCodeException();
            AssertException<InvalidRedeemCodeException>(exc);
        }

        [Fact]
        public void DuplicateRedeemExceptionSerializable()
        {
            var exc = new DuplicateRedeemException("for testing");
            AssertException<DuplicateRedeemException>(exc);
        }

        [Fact]
        public void SheetRowValidateExceptionSerializable()
        {
            var exc = new SheetRowValidateException("for testing");
            AssertException<SheetRowValidateException>(exc);
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

        [Fact]
        public void ShopItemExpiredExceptionSerializable()
        {
            var exc = new ShopItemExpiredException("for testing.");
            AssertException<ShopItemExpiredException>(exc);
        }

        [Fact]
        public void InvalidMonsterCollectionRoundException_Serializable()
        {
            var exc = new InvalidMonsterCollectionRoundException("for testing.");
            AssertException<InvalidMonsterCollectionRoundException>(exc);
        }

        [Fact]
        public void MonsterCollectionExpiredException_Serializable()
        {
            var exc = new MonsterCollectionExpiredException("for testing.");
            AssertException<MonsterCollectionExpiredException>(exc);
        }

        [Fact]
        public void InvalidLevelException_Serializable()
        {
            var exc = new InvalidLevelException("for testing.");
            AssertException<InvalidLevelException>(exc);
        }

        [Fact]
        public void ActionPointExceededException_Serializable()
        {
            var exc = new ActionPointExceededException("for testing.");
            AssertException<ActionPointExceededException>(exc);
        }

        [Fact]
        public void InvalidItemCountException_Serializable()
        {
            var exc = new InvalidItemCountException("for testing.");
            AssertException<InvalidItemCountException>(exc);
        }

        [Fact]
        public void DuplicateOrderIdException_Serializable()
        {
            var exc = new DuplicateOrderIdException("for testing.");
            AssertException<DuplicateOrderIdException>(exc);
        }

        [Fact]
        public void OrderIdDoesNotExistException_Serializable()
        {
            var exc = new OrderIdDoesNotExistException("for testing.");
            AssertException<OrderIdDoesNotExistException>(exc);
        }

        [Fact]
        public void InvalidTradableIdException_Serializable()
        {
            var exc = new InvalidTradableIdException("for testing.");
            AssertException<InvalidTradableIdException>(exc);
        }

        [Fact]
        public void SerializeFailedException_Serializable()
        {
            var exc = new SerializeFailedException("for testing.");
            AssertException<SerializeFailedException>(exc);
        }

        [Fact]
        public void DeserializeFailedException_Serializable()
        {
            var exc = new DeserializeFailedException("for testing.");
            AssertException<DeserializeFailedException>(exc);
        }

        private static void AssertException<T>(Exception exc)
            where T : Exception
        {
            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, exc);

                ms.Seek(0, SeekOrigin.Begin);
                var deserialized = (T)formatter.Deserialize(ms);
                Assert.Equal(exc.Message, deserialized.Message);
            }
        }

        private static void AssertAdminState(AdminState adminState, AdminState adminState2)
        {
            Assert.Equal(adminState.AdminAddress, adminState2.AdminAddress);
            Assert.Equal(adminState.address, adminState2.address);
            Assert.Equal(adminState.ValidUntil, adminState2.ValidUntil);
        }
    }
}
