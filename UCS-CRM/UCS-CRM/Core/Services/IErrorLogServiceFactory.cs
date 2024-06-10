using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Core.Services;

public interface IErrorLogServiceFactory
{
    IErrorLogService Create();
}