using System.Collections;
using System.Threading.Tasks;

namespace Nekoyume.Extension
{
    public static class TaskExtensions
    {
        public static IEnumerator AsIEnumerator(this Task task)
        {
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted && !(task.Exception is null))
                throw task.Exception;
        }
    }
}
