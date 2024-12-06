using System;

namespace UCS_CRM.Core.Services
{
    public interface IDateTimeService
    {
        DateTime GetCurrentDateTime();
        string FormatDate(DateTime date);
        string FormatDateTime(DateTime dateTime);
        DateTime? ParseDate(string dateString);
        DateTime? ParseDateTime(string dateTimeString);
        DateTime ConvertToUserTimeZone(DateTime utcDateTime);
        DateTime ConvertToUtc(DateTime userDateTime);
        DayOfWeek GetFirstDayOfWeek();
    }
} 