using System;
using UnityEngine;
using Logger = OneStore.Common.OneStoreLogger;

namespace OneStore.Auth.Internal
{
    public static class AuthHelper
    {
        /// <summary>
        /// Parses the SignIn results returned by Gaa Auth Client.
        /// </summary>
        /// <returns> The SignInResult that indicates the outcome of the Java SignInResult. </returns>
        public static SignInResult ParseJavaSignInpResult(AndroidJavaObject javaSignInResult)
        {
            var code = javaSignInResult.Call<int>("getCode");
            var message = javaSignInResult.Call<string>("getMessage");

            return new SignInResult(code, message);
        }

        /// <summary>
        /// Parses the SignInResults returned by Gaa SignIn Client.
        /// </summary>
        /// <returns>Returns the code value of the SignInResult in ResponseCode.</returns>
        public static ResponseCode GetResponseCodeFromSignInResult(SignInResult signInResult)
        {
            var resultResponseCode = ResponseCode.RESULT_ERROR;
            try
            {
                resultResponseCode = (ResponseCode) Enum.Parse(typeof(ResponseCode), signInResult.Code.ToString());
            }
            catch (ArgumentNullException)
            {
                Logger.Error("Missing response code, return ResponseCode.RESULT_ERROR.");
            }
            catch (ArgumentException)
            {
                Logger.Error("Unknown response code {0}, return ResponseCode.RESULT_ERROR.", signInResult.Code);
            }
            return resultResponseCode;
        }
    }
}
