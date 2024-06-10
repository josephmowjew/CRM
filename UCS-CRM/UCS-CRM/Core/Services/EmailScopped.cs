using Hangfire;

namespace UCS_CRM.Core.Services;


// Inject IServiceProvider in your class where you enqueue Hangfire jobs


public class HangfireJobEnqueuer
{
    private static IServiceProvider _serviceProvider;

    public HangfireJobEnqueuer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public void EnqueueEmailJob(string email, string subject, string body)
    {
        // Check if email is not null or empty
        if (!string.IsNullOrEmpty(email))
        {
            BackgroundJob.Enqueue(() => SendEmailInScope(email, subject, body));
        }
        else
        {
            // Optionally log an error or throw an exception
            // throw new ArgumentException("Email cannot be null or empty");
            Console.WriteLine("Invalid email address. Email cannot be null or empty.");
        }
    }

    public static async Task SendEmailInScope(string email, string subject, string body)
    {
        // Ensure email is not null or empty before proceeding
        if (string.IsNullOrEmpty(email))
        {
            // Optionally log an error or throw an exception
            // throw new ArgumentException("Email cannot be null or empty");
            Console.WriteLine("Invalid email address. Email cannot be null or empty.");
            return;
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            
            // Check if emailService is not null
            if (emailService == null)
            {
                // Optionally log an error or throw an exception
                // throw new InvalidOperationException("EmailService not resolved");
                Console.WriteLine("EmailService could not be resolved.");
                return;
            }

            await emailService.SendMailWithKeyVarReturn(email, subject, body);
        }
    }
}

