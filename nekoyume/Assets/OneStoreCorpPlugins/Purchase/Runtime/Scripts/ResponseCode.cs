namespace OneStore.Purchasing
{
    public enum ResponseCode
    {
        /// <summary>
        /// Possible response codes.
        /// Public documentation can be found at
        /// https://onestore-dev.gitbook.io/dev/v/eng/tools/tools/v21/references/en-annotations/en-purchaseclient.responsecode
        /// </summary>
        RESULT_OK = 0,
        RESULT_USER_CANCELED = 1,
        RESULT_SERVICE_UNAVAILABLE = 2,
        RESULT_BILLING_UNAVAILABLE = 3,
        RESULT_ITEM_UNAVAILABLE = 4,
        RESULT_DEVELOPER_ERROR = 5,
        RESULT_ERROR = 6,
        RESULT_ITEM_ALREADY_OWNED = 7,
        RESULT_ITEM_NOT_OWNED = 8,
        RESULT_FAIL = 9,
        RESULT_NEED_LOGIN = 10,
        RESULT_NEED_UPDATE = 11,
        RESULT_SECURITY_ERROR = 12,
        RESULT_BLOCKED_APP = 13,
        RESULT_NOT_SUPPORT_SANDBOX = 14,

        RESULT_EMERGENCY_ERROR = 99999,

        ERROR_DATA_PARSING = 1001,
        ERROR_SIGNATURE_VERIFICATION = 1002,
        ERROR_ILLEGAL_ARGUMENT = 1003,
        ERROR_UNDEFINED_CODE = 1004,
        ERROR_SIGNATURE_NOT_VALIDATION = 1005,
        ERROR_UPDATE_OR_INSTALL = 1006,
        ERROR_SERVICE_DISCONNECTED = 1007,
        ERROR_FEATURE_NOT_SUPPORTED = 1008,
        ERROR_SERVICE_TIMEOUT = 1009,
        ERROR_CLIENT_NOT_ENABLED = 1010
    }
}
