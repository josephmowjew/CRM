using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Core.Services;

public class ErrorLogServiceFactory : IErrorLogServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ErrorLogServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IErrorLogService Create()
    {
        var scope = _serviceProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IErrorLogService>();
    }

   
}