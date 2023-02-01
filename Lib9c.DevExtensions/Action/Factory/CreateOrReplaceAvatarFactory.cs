using System;
using Lib9c.DevExtensions.Action.Interface;

namespace Lib9c.DevExtensions.Action.Factory
{
    public static class CreateOrReplaceAvatarFactory
    {
        public static (Exception exception, ICreateOrReplaceAvatar result)
            TryGetByBlockIndex(
                long blockIndex,
                int avatarIndex = 0,
                string name = "Avatar",
                int hair = 0,
                int lens = 0,
                int ear = 0,
                int tail = 0,
                int level = 1,
                (int itemId, int enhancement)[] equipments = null)
        {
            try
            {
                return (
                    null,
                    new CreateOrReplaceAvatar(
                        avatarIndex,
                        name,
                        hair,
                        lens,
                        ear,
                        tail,
                        level,
                        equipments));
            }
            catch (ArgumentException e)
            {
                return (e, null);
            }
        }
    }
}
