using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using UCS_CRM.Core.Services;

namespace UCS_CRM.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToFormattedDate(this DateTime dateTime, IDateTimeService dateTimeService)
        {
            return dateTimeService.FormatDate(dateTime);
        }

        public static string ToFormattedDateTime(this DateTime dateTime, IDateTimeService dateTimeService)
        {
            return dateTimeService.FormatDateTime(dateTime);
        }

        public static string ToFormattedDate(this DateTime? dateTime, IDateTimeService dateTimeService)
        {
            return dateTime.HasValue ? dateTimeService.FormatDate(dateTime.Value) : string.Empty;
        }

        public static string ToFormattedDateTime(this DateTime? dateTime, IDateTimeService dateTimeService)
        {
            return dateTime.HasValue ? dateTimeService.FormatDateTime(dateTime.Value) : string.Empty;
        }

        // Helper methods for views
        public static string ToFormattedDate(this IHtmlHelper html, DateTime dateTime)
        {
            var dateTimeService = html.ViewContext.HttpContext.RequestServices.GetRequiredService<IDateTimeService>();
            return dateTimeService.FormatDate(dateTime);
        }

        public static string ToFormattedDateTime(this IHtmlHelper html, DateTime dateTime)
        {
            var dateTimeService = html.ViewContext.HttpContext.RequestServices.GetRequiredService<IDateTimeService>();
            return dateTimeService.FormatDateTime(dateTime);
        }

        public static string ToFormattedDate(this IHtmlHelper html, DateTime? dateTime)
        {
            if (!dateTime.HasValue) return string.Empty;
            var dateTimeService = html.ViewContext.HttpContext.RequestServices.GetRequiredService<IDateTimeService>();
            return dateTimeService.FormatDate(dateTime.Value);
        }

        public static string ToFormattedDateTime(this IHtmlHelper html, DateTime? dateTime)
        {
            if (!dateTime.HasValue) return string.Empty;
            var dateTimeService = html.ViewContext.HttpContext.RequestServices.GetRequiredService<IDateTimeService>();
            return dateTimeService.FormatDateTime(dateTime.Value);
        }
    }
} 