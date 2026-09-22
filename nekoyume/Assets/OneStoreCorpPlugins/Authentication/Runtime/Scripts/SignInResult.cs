using System;

namespace OneStore.Auth
{
    [Serializable]
    public class SignInResult
    {
        private int _code;
        private string _message;

        public int Code { get { return _code; } }
        public string Message { get { return _message; } }

        public SignInResult(int code, string message)
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
            return "SignInResult(code=" + _code +  ", message: " + _message + ")";
        }
    }
}
