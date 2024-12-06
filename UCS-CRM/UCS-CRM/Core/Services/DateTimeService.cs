using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UCS_CRM.Data;
using UCS_CRM.Models;

namespace UCS_CRM.Core.Services
{
    public class DateTimeService : IDateTimeService
    {
        private readonly ApplicationDbContext _context;
        private SystemDateConfiguration _cachedConfig;
        private DateTime _lastConfigCheck = DateTime.MinValue;
        private readonly TimeSpan _configCacheTimeout = TimeSpan.FromMinutes(5);

        public DateTimeService(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<SystemDateConfiguration> GetConfigurationAsync()
        {
            if (_cachedConfig != null && DateTime.UtcNow - _lastConfigCheck < _configCacheTimeout)
            {
                return _cachedConfig;
            }

            _cachedConfig = await _context.SystemDateConfigurations.FirstOrDefaultAsync();
            _lastConfigCheck = DateTime.UtcNow;

            if (_cachedConfig == null)
            {
                // Return default configuration if none exists
                _cachedConfig = new SystemDateConfiguration
                {
                    TimeZone = TimeZoneInfo.Local.Id,
                    DateFormat = "MM/dd/yyyy",
                    FirstDayOfWeek = DayOfWeek.Sunday,
                    UseSystemTime = true
                };
            }

            return _cachedConfig;
        }

        private SystemDateConfiguration GetConfiguration()
        {
            return GetConfigurationAsync().GetAwaiter().GetResult();
        }

        public DateTime GetCurrentDateTime()
        {
            var config = GetConfiguration();
            
            if (config.UseSystemTime)
            {
                return ConvertToUserTimeZone(DateTime.UtcNow);
            }
            
            return config.CustomDateTime ?? ConvertToUserTimeZone(DateTime.UtcNow);
        }

        public string FormatDate(DateTime date)
        {
            var config = GetConfiguration();
            return date.ToString(config.DateFormat);
        }

        public string FormatDateTime(DateTime dateTime)
        {
            var config = GetConfiguration();
            return dateTime.ToString($"{config.DateFormat} HH:mm:ss");
        }

        public DateTime? ParseDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString)) return null;

            var config = GetConfiguration();
            return DateTime.TryParseExact(dateString, config.DateFormat, 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, 
                out DateTime result) ? result : null;
        }

        public DateTime? ParseDateTime(string dateTimeString)
        {
            if (string.IsNullOrWhiteSpace(dateTimeString)) return null;

            var config = GetConfiguration();
            return DateTime.TryParseExact(dateTimeString, $"{config.DateFormat} HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime result) ? result : null;
        }

        public DateTime ConvertToUserTimeZone(DateTime utcDateTime)
        {
            var config = GetConfiguration();
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone);
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
        }

        public DateTime ConvertToUtc(DateTime userDateTime)
        {
            var config = GetConfiguration();
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZone);
            return TimeZoneInfo.ConvertTimeToUtc(userDateTime, timeZone);
        }

        public DayOfWeek GetFirstDayOfWeek()
        {
            var config = GetConfiguration();
            return config.FirstDayOfWeek;
        }
    }
} 