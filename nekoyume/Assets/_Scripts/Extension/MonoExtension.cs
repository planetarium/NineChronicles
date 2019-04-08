using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Nekoyume
{
    public static class MonoExtension
    {
        /// <summary>
        /// 배열이나 컬랙션은 검사하지 못함.
        /// </summary>
        /// <param name="mono"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="SerializeFieldNullException"></exception>
        public static void ComponentFieldsNotNullTest<T>(this T mono) where T : MonoBehaviour
        {
            var nullValues = typeof(T)
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(field => field.FieldType.IsInheritsFrom(typeof(MonoBehaviour)))
                .Select(field => field.GetValue(mono))
                .Where(value => ReferenceEquals(value, null));

            if (nullValues.GetEnumerator().MoveNext())
            {
                throw new SerializeFieldNullException();
            }
        }
    }
}
