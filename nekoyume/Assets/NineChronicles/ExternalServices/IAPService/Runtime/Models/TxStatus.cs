namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    public enum TxStatus
    {
        Created = 1,
        Staged = 2,
        Success = 10,
        Failure = 91,
        Invalid = 92,
        NotFound = 93,
        FailToCreate = 94,
        Unknown = 99
    }
}
