using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Nekoyume.Game
{
    public class EventSubject : MonoBehaviour
    {
        public Dictionary<string, Subject<Unit>> SubjectMap { get; }
            = new Dictionary<string, Subject<Unit>>();

        private void OnDestroy()
        {
            Dispose();
        }

        public IObservable<Unit> GetEvent(string key)
        {
            if (!SubjectMap.ContainsKey(key))
            {
                SubjectMap[key] = new Subject<Unit>();
            }

            return SubjectMap[key];
        }

        /// <summary>
        /// Raises event with parameter. (Can be used in unity animation event.)
        /// </summary>
        /// <param name="key">
        /// Event key to raise.
        /// </param>
        public void RaiseEvent(string key)
        {
            if (!SubjectMap.ContainsKey(key))
            {
                return;
            }

            SubjectMap[key].OnNext(default);
        }

        private void Dispose()
        {
            foreach (var subject in SubjectMap.Values)
            {
                subject.Dispose();
            }
        }
    }
}
