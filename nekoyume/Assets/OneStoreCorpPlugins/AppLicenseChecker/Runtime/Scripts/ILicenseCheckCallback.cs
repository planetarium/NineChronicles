namespace OneStore.Alc
{
    public interface ILicenseCheckCallback
    {
        /// <summary>
        /// Called when license granted.
        /// </summary>
        /// <param name="license"></param>
        /// <param name="signature"></param>
         void OnGranted(string license, string signature);
         /// <summary>
         /// Called when license denied
         /// </summary>
         void OnDenied();
         /// <summary>
         /// Called when querying license got an error
         /// Send error code and message
         /// </summary>
         /// <param name="code"></param>
         /// <param name="message"></param>
         void OnError(int code, string message);
    }
}