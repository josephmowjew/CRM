namespace UCS_CRM.Core.Services
{
    public interface IErrorLogService
    {
        Task LogErrorAsync(Exception ex);
    }
}
