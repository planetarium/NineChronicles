using System;

namespace OneStore.Purchasing
{
    /// <summary>
    /// It is an object with the code and message of in-app purchase API responses included.
    /// Public documentation can be found at
    /// https://onestore-dev.gitbook.io/dev/v/eng/tools/tools/v21/references/en-classes/en-iapresult
    /// </summary>
    [Serializable]
    public class IapResult
    {
        private int _code;
        private string _message;

        public int Code { get { return _code; } }
        public string Message { get { return _message; } }

        public IapResult(int code, string message)
        {
            _code = code;
            _message = message;
        }

        public bool IsSuccessful()
        {
            return _code == (int) ResponseCode.RESULT_OK;
        }

        public override string ToString()
        {
            return "IapResult(code=" + _code +  ", message: " + _message + ")";
        }
    }
}
