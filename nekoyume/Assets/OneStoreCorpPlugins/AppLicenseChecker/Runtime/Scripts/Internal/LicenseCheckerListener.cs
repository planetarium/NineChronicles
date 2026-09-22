using System;
using UnityEngine;

namespace OneStore.Alc.Internal
{
    public class LicenseCheckerListener : AndroidJavaProxy
    {
        public event Action<string, string> Granted = delegate { };

        public event Action Denied = delegate { };

        public event Action<int, string> Error = delegate { };
        public LicenseCheckerListener() : base(Constants.LicenseCheckerListener) {}

        void granted(string license, string signature) {
            Granted.Invoke(license, signature);
        }

        void denied() { 
            Denied.Invoke();
        }

        void error(int code, string message) {
            Error.Invoke(code, message);
        }
    }
}