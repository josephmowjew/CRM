// Extension method for applying sorting
using UCS_CRM.Core.Models;

public static class QueryableExtensions
{
    public static IQueryable<Ticket> ApplySorting(this IQueryable<Ticket> query, string sortColumn, string sortDirection)
    {
         return (sortColumn.ToLower(), sortDirection.ToLower()) switch
        {
            ("title", "asc") => query.OrderBy(t => t.Title),
            ("title", "desc") => query.OrderByDescending(t => t.Title),
            ("createddate", "asc") => query.OrderBy(t => t.CreatedDate),
            ("createddate", "desc") => query.OrderByDescending(t => t.CreatedDate),
            ("state", "asc") => query.OrderBy(t => t.State.Name),
            ("state", "desc") => query.OrderByDescending(t => t.State.Name),
            ("priority", "asc") => query.OrderBy(t => t.TicketPriority.Name),
            ("priority", "desc") => query.OrderByDescending(t => t.TicketPriority.Name),
            // Add other columns as needed
            _ => query.OrderBy(t => t.CreatedDate)
        };
    }
}