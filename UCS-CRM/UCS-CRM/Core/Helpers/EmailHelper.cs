using System;
using System.Net.Mail;
using System.Threading.Tasks;
using UCS_CRM.Core.Services;

namespace UCS_CRM.Core.Helpers;

    public static class EmailHelper
{
    public static void SendEmail(HangfireJobEnqueuer jobEnqueuer, string primaryEmail, string subject, string body, string secondaryEmail = null)
    {
        if (string.IsNullOrEmpty(primaryEmail))
        {
            throw new ArgumentException("Primary email cannot be null or empty", nameof(primaryEmail));
        }

        jobEnqueuer.EnqueueEmailJob(primaryEmail, subject, body);

        if (!string.IsNullOrEmpty(secondaryEmail))
        {
            jobEnqueuer.EnqueueEmailJob(secondaryEmail, subject, body);
        }
    }
}
