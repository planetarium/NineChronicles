using System;
using UnityEngine;

namespace OneStore.Auth.Internal
{
    public class OnAuthListener : AndroidJavaProxy
    {
        public event Action<AndroidJavaObject> OnAuthResponse = delegate { };
        public OnAuthListener() : base(Constants.OnAuthListener) { }

        void onResponse(AndroidJavaObject signInResult)
        {
            OnAuthResponse.Invoke(signInResult);
        }
    }
}
