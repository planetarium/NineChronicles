using System;
using UnityEngine;

namespace OneStore.Common.Internal
{
    public class ResultListener : AndroidJavaProxy
    {
        public event Action<int, string> OnResponse = delegate { };

        public ResultListener() : base(Constants.ResultListener) { }

        void onResponse(int code, string message)
        {
            OnResponse.Invoke(code, message);
        }
    }
}
