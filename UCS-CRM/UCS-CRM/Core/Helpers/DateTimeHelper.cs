using Microsoft.EntityFrameworkCore;
using UCS_CRM.Data;
using UCS_CRM.Core.Models;
using System;

namespace UCS_CRM.Core.Helpers;

public static class DateTimeHelper
{
    private static readonly TimeZoneInfo MalawiTimeZone = GetMalawiTimeZone();

    private static TimeZoneInfo GetMalawiTimeZone()
    {
        try
        {
            // Try IANA timezone first (Linux/macOS)
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Blantyre");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Fallback to Windows timezone
                return TimeZoneInfo.FindSystemTimeZoneById("South African Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // If all else fails, create a custom timezone for GMT+2
                return TimeZoneInfo.CreateCustomTimeZone(
                    "Malawi Time",
                    new TimeSpan(2, 0, 0),
                    "Malawi Time",
                    "Malawi Standard Time");
            }
        }
    }

    public static DateTime AdjustToMalawiTime(DateTime date)
    {
        if (date.Kind == DateTimeKind.Utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(date, MalawiTimeZone);
        }
        return date;
    }

    public static async Task<DateTime> GetNextWorkingDay(ApplicationDbContext context, DateTime date)
    {
        var nextDay = AdjustToMalawiTime(date);
        var workingHours = await context.WorkingHours.FirstOrDefaultAsync(w => !w.DeletedDate.HasValue);
        
        if (workingHours == null)
        {
            // Default working hours if not configured
            workingHours = new WorkingHours
            {
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                BreakStartTime = new TimeSpan(12, 0, 0),
                BreakEndTime = new TimeSpan(13, 0, 0)
            };
        }

        bool isWorkingDay = false;
        while (!isWorkingDay)
        {
            if (nextDay.DayOfWeek == DayOfWeek.Saturday)
            {
                nextDay = nextDay.AddDays(2).Date + workingHours.StartTime;
            }
            else if (nextDay.DayOfWeek == DayOfWeek.Sunday)
            {
                nextDay = nextDay.AddDays(1).Date + workingHours.StartTime;
            }

            var currentTime = nextDay.TimeOfDay;
            
            // If outside working hours, move to next working day start
            if (currentTime < workingHours.StartTime || currentTime > workingHours.EndTime)
            {
                nextDay = nextDay.AddDays(1).Date + workingHours.StartTime;
                continue;
            }

            // If during lunch break, move to after break
            if (currentTime >= workingHours.BreakStartTime && currentTime <= workingHours.BreakEndTime)
            {
                nextDay = nextDay.Date + workingHours.BreakEndTime;
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
                nextDay = nextDay.AddDays(1).Date + workingHours.StartTime;
            }
        }

        return nextDay;
    }

    public static async Task<TimeSpan> CalculateWorkingHours(ApplicationDbContext context, DateTime startDate, DateTime endDate)
    {
        var adjustedStartDate = AdjustToMalawiTime(startDate);
        var adjustedEndDate = AdjustToMalawiTime(endDate);
        
        var workingHours = await context.WorkingHours.FirstOrDefaultAsync(w => !w.DeletedDate.HasValue);
        var totalWorkingTime = TimeSpan.Zero;
        var currentDate = adjustedStartDate;

        while (currentDate <= adjustedEndDate)
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && 
                currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                var workStart = workingHours.StartTime;
                var workEnd = workingHours.EndTime;
                var breakStart = workingHours.BreakStartTime;
                var breakEnd = workingHours.BreakEndTime;

                var dailyWorkingTime = (workEnd - workStart) - (breakEnd - breakStart);
                totalWorkingTime += dailyWorkingTime;
            }
            currentDate = currentDate.AddDays(1);
        }

        return totalWorkingTime;
    }

    public static async Task<bool> IsWithinBusinessHours(ApplicationDbContext context, DateTime time)
    {
        var workingHours = await context.WorkingHours.FirstOrDefaultAsync(w => !w.DeletedDate.HasValue);
        
        if (workingHours == null)
        {
            // Use default working hours if not configured
            workingHours = new WorkingHours
            {
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                BreakStartTime = new TimeSpan(12, 0, 0),
                BreakEndTime = new TimeSpan(13, 0, 0)
            };
        }

        var localTime = AdjustToMalawiTime(time);
        
        // Check if it's a weekend
        if (localTime.DayOfWeek == DayOfWeek.Saturday || localTime.DayOfWeek == DayOfWeek.Sunday)
            return false;
        
        var currentTime = localTime.TimeOfDay;
        
        // Check if within working hours and not during break
        return (currentTime >= workingHours.StartTime && currentTime <= workingHours.EndTime) &&
               !(currentTime >= workingHours.BreakStartTime && currentTime <= workingHours.BreakEndTime);
    }
}