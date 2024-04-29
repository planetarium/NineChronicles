using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.Pattern
{
    public class Fsm<T> where T : struct, IComparable, IConvertible, IFormattable
    {
        private readonly MonoBehaviour _mono;
        private readonly Dictionary<T, Func<IEnumerator>> _states = new Dictionary<T, Func<IEnumerator>>();

        private Coroutine _runningCoroutineHandle;
        private Coroutine _stateCoroutineHandle;

        public bool isRunning { get; private set; }

        public T current { get; private set; }

        public T next { get; set; }

        public bool shouldChange => !EqualityComparer<T>.Default.Equals(current, next);

        public Fsm(MonoBehaviour mono)
        {
            if (ReferenceEquals(mono, null))
            {
                throw new ArgumentNullException();
            }

            _mono = mono;

            isRunning = false;

            var t = typeof(T);
            var names = Enum.GetNames(t);
            foreach (var name in names)
            {
                InitState((T)Enum.Parse(t, name), null);
            }
        }

        public void RegisterStateCoroutine(T state, Func<IEnumerator> func)
        {
            if (!_states.ContainsKey(state))
            {
                throw new InvalidStateException();
            }

            _states[state] = func;
        }

        public void Run(T state)
        {
            if (isRunning)
            {
                NcDebug.LogWarning("Already started.");
                return;
            }

            if (!_states.ContainsKey(state))
            {
                throw new InvalidStateException();
            }

            isRunning = true;

            current = state;
            next = current;

            StopCoroutine(ref _runningCoroutineHandle);
            _runningCoroutineHandle = _mono.StartCoroutine(CoRunning());
        }

        public void Kill()
        {
            StopCoroutine(ref _stateCoroutineHandle);
            StopCoroutine(ref _runningCoroutineHandle);

            isRunning = false;
        }

        private void InitState(T state, Func<IEnumerator> func)
        {
            if (_states.ContainsKey(state))
            {
                throw new StateAlreadyContainedException();
            }

            _states.Add(state, func);
        }

        private IEnumerator CoRunning()
        {
            while (isRunning)
            {
                var func = _states[current];
                if (!ReferenceEquals(func, null))
                {
                    _stateCoroutineHandle = _mono.StartCoroutine(func());
                    yield return _stateCoroutineHandle;
                }

                while (isRunning && current.Equals(next))
                {
                    yield return null;
                }

                StopCoroutine(ref _stateCoroutineHandle);

                current = next;
                next = current;
            }
        }

        private void StopCoroutine(ref Coroutine coroutine)
        {
            if (ReferenceEquals(coroutine, null))
            {
                return;
            }

            _mono.StopCoroutine(coroutine);
            coroutine = null;
        }
    }

    public class FsmException : Exception
    {
        public FsmException()
        {
        }

        public FsmException(string message) : base(message)
        {
        }

        public FsmException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    public class InvalidStateException : Exception
    {
        public InvalidStateException()
        {
        }

        public InvalidStateException(string message) : base(message)
        {
        }

        public InvalidStateException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    public class StateAlreadyContainedException : Exception
    {
        public StateAlreadyContainedException()
        {
        }

        public StateAlreadyContainedException(string message) : base(message)
        {
        }

        public StateAlreadyContainedException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
