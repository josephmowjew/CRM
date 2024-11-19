using Microsoft.EntityFrameworkCore;
using UCS_CRM.Data;

namespace UCS_CRM.Core.Helpers;

public static class DateTimeHelper
{
    public static async Task<DateTime> GetNextWorkingDay(ApplicationDbContext context, DateTime date)
    {
        var nextDay = date;
        bool isWorkingDay = false;

        while (!isWorkingDay)
        {
            // Check if it's weekend
            if (nextDay.DayOfWeek == DayOfWeek.Saturday)
            {
                nextDay = nextDay.AddDays(2); // Skip to Monday
            }
            else if (nextDay.DayOfWeek == DayOfWeek.Sunday)
            {
                nextDay = nextDay.AddDays(1); // Skip to Monday
            }

            // Check if it's a holiday
            var holiday = await context.Holidays
                .Where(h => !h.DeletedDate.HasValue &&
                    ((h.IsRecurring && 
                      h.StartDate.Month == nextDay.Month && 
                      h.StartDate.Day == nextDay.Day) ||
                     (!h.IsRecurring && 
                      nextDay.Date >= h.StartDate.Date && 
                      nextDay.Date <= h.EndDate.Date)))
                .FirstOrDefaultAsync();

            if (holiday == null)
            {
                isWorkingDay = true;
            }
            else
            {
                nextDay = nextDay.AddDays(1);
            }
        }

        return nextDay;
    }
}