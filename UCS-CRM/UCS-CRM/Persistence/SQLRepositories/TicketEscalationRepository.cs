using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UCS_CRM.Persistence.SQLRepositories
{

    public class TicketEscalationRepository : ITicketEscalationRepository
    {
        private readonly ApplicationDbContext _context;

        public TicketEscalationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(TicketEscalation ticketEscalation)
        {
            this._context.TicketEscalations.Add(ticketEscalation);
        }

        public TicketEscalation? Exists(TicketEscalation ticketEscalation)
        {
            return this._context.TicketEscalations.Include(t => t.Ticket).FirstOrDefault(a => a.TicketId == ticketEscalation.TicketId && a.DateEscalated.Date == ticketEscalation.DateEscalated.Date & a.Status != Lambda.Deleted);
        }

        public async Task<TicketEscalation?> GetTicketEscalation(int id)
        {
            return await this._context.TicketEscalations.Include(t => t.EscalatedTo).Include(t=>t.Ticket).ThenInclude(m=>m.Member).Include(p => p.Ticket.TicketPriority).Include(a => a.Ticket.AssignedTo).FirstOrDefaultAsync(x => x.Id == id & x.Status != Lambda.Deleted);
        }

        //public async Task<List<TicketEscalation>?> GetTicketEscalations(int departmentId, CursorParams @params)
        //{
        //    if (@params.Take > 0)
        //    {

        //        if(departmentId > 0)
        //        {
        //            //check if there is a search term sent 

        //            if (string.IsNullOrEmpty(@params.SearchTerm))
        //            {
        //                var ticketEscalations = (from tblOb in await this._context
        //                                         .TicketEscalations
        //                                         .Include(t => t.Ticket)
        //                                         .ThenInclude(m => m.Member)
        //                                         .Include(a => a.Ticket.AssignedTo)
        //                                         .Include(p => p.Ticket.TicketPriority)
        //                                         .Where(t => t.Ticket.AssignedTo != null && t.Ticket.AssignedTo.DepartmentId == departmentId)
        //                                         .Where(a => a.Status != Lambda.Deleted && a.Resolved == false)
        //                                         .Skip(@params.Skip)
        //                                         .Take(@params.Take)
        //                                         .ToListAsync()
        //                                         select tblOb);


        //                if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
        //                {
        //                    ticketEscalations = ticketEscalations.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

        //                }


        //                return ticketEscalations.ToList();

        //            }
        //            else
        //            {
        //                //include search text in the query
        //                var ticketEscalations = (from tblOb in await this._context
        //                                         .TicketEscalations
        //                                         .Include(t => t.Ticket)
        //                                         .ThenInclude(m => m.Member)
        //                                         .Include(a => a.Ticket.AssignedTo)
        //                                         .Include(p => p.Ticket.TicketPriority)
        //                                          .Where(t => t.Ticket.AssignedTo != null && t.Ticket.AssignedTo.DepartmentId == departmentId)
        //                                         .Where(a => a.Ticket.Title.ToLower()
        //                                         .Contains(@params.SearchTerm) & a.Status != Lambda.Deleted)
        //                                        .Skip(@params.Skip)
        //                                        .Take(@params.Take)
        //                                        .ToListAsync()
        //                                         select tblOb);

        //                ticketEscalations = ticketEscalations.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


        //                return ticketEscalations.ToList();

        //            }
        //        }
        //        else
        //        {
        //            //check if there is a search term sent 

        //            if (string.IsNullOrEmpty(@params.SearchTerm))
        //            {
        //                var ticketEscalations = (from tblOb in await this._context
        //                                         .TicketEscalations
        //                                         .Include(t => t.Ticket)
        //                                         .ThenInclude(m => m.Member)
        //                                         .Include(a => a.Ticket.AssignedTo)
        //                                         .Include(p => p.Ticket.TicketPriority)
        //                                         .Where(a => a.Status != Lambda.Deleted && a.Resolved == false)
        //                                         .Skip(@params.Skip)
        //                                         .Take(@params.Take)
        //                                         .ToListAsync()
        //                                         select tblOb);


        //                if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
        //                {
        //                    ticketEscalations = ticketEscalations.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

        //                }


        //                return ticketEscalations.ToList();

        //            }
        //            else
        //            {
        //                //include search text in the query
        //                var ticketEscalations = (from tblOb in await this._context
        //                                         .TicketEscalations
        //                                         .Include(t => t.Ticket)
        //                                         .ThenInclude(m => m.Member)
        //                                         .Include(a => a.Ticket.AssignedTo)
        //                                         .Include(p => p.Ticket.TicketPriority)
        //                                         .Where(a => a.Ticket.Title.ToLower()
        //                                         .Contains(@params.SearchTerm) & a.Status != Lambda.Deleted)
        //                                        .Skip(@params.Skip)
        //                                        .Take(@params.Take)
        //                                        .ToListAsync()
        //                                         select tblOb);

        //                ticketEscalations = ticketEscalations.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


        //                return ticketEscalations.ToList();

        //            }
        //        }


        //    }
        //    else
        //    {
        //        return null;
        //    }


        //}

        public async Task<List<TicketEscalation>?> GetTicketEscalations(int? departmentId, CursorParams @params)
        {
            if (@params.Take <= 0)
            {
                return null;
            }

            var query = this._context.TicketEscalations
                .Include(t => t.Ticket.Member)
                .Include(a => a.Ticket.AssignedTo)
                .Include(p => p.Ticket.TicketPriority)
                .Where(a => a.Status != Lambda.Deleted && a.Resolved == false);

            if (departmentId != null || departmentId > 0)
            {
                query = query.Where(t => t.Ticket.AssignedTo != null && t.Ticket.AssignedTo.DepartmentId == departmentId);
            }

            if (!string.IsNullOrEmpty(@params.SearchTerm))
            {
                query = query.Where(a => a.Ticket.Title.ToLower().Contains(@params.SearchTerm));
            }

            if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
            {
                var sortExpression = @params.SortColum + " " + @params.SortDirection;
                query = query.OrderBy(sortExpression);
            }

            var ticketEscalations = await query
                .Skip(@params.Skip)
                .Take(@params.Take)
                .ToListAsync();

            return ticketEscalations;
        }
        public async Task<int> GetTicketEscalationsCount(int? departmentId, CursorParams @params)
        {
            if (@params.Take <= 0)
            {
                return 0;
            }

            var query = this._context.TicketEscalations
                .Include(t => t.Ticket.Member)
                .Include(a => a.Ticket.AssignedTo)
                .Include(p => p.Ticket.TicketPriority)
                .Where(a => a.Status != Lambda.Deleted && a.Resolved == false);

            if (departmentId != null || departmentId > 0)
            {
                query = query.Where(t => t.Ticket.AssignedTo != null && t.Ticket.AssignedTo.DepartmentId == departmentId);
            }

            if (!string.IsNullOrEmpty(@params.SearchTerm))
            {
                query = query.Where(a => a.Ticket.Title.ToLower().Contains(@params.SearchTerm));
            }

            if (!string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
            {
                var sortExpression = @params.SortColum + " " + @params.SortDirection;
                query = query.OrderBy(sortExpression);
            }

            var ticketEscalations = await query.CountAsync();

            return ticketEscalations;
        }


        public async Task<List<TicketEscalation>?> GetTicketEscalationsForUser(CursorParams @params, string memberId)
        {
            if (@params.Take > 0)
            {
                //check if there is a search term sent 

                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var ticketEscalations = (from tblOb in await this._context.TicketEscalations.Include(t => t.Ticket).ThenInclude(m => m.Member).Include(a => a.Ticket.AssignedTo)
                                             .Include(p => p.Ticket.TicketPriority).Where(a => a.Status != Lambda.Deleted && a.CreatedById == memberId
                                             && a.Resolved == false).Skip(@params.Skip).Take(@params.Take).ToListAsync()
                                             select tblOb);


                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        ticketEscalations = ticketEscalations.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return ticketEscalations.ToList();

                }
                else
                {
                    //include search text in the query
                    var ticketEscalations = (from tblOb in await this._context.TicketEscalations.Include(t => t.Ticket).
                                             ThenInclude(m => m.Member).Include(a => a.Ticket.AssignedTo).Include(p => p.Ticket.TicketPriority)
                                        .Where(a => a.Ticket.Title.ToLower().Contains(@params.SearchTerm) & a.Status != Lambda.Deleted && a.CreatedById == memberId)
                                        .Skip(@params.Skip)
                                        .Take(@params.Take)
                                        .ToListAsync()
                                             select tblOb);

                    ticketEscalations = ticketEscalations.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


                    return ticketEscalations.ToList();

                }

            }
            else
            {
                return null;
            }


        }

        public async Task<List<TicketEscalation>?> GetTicketEscalations()
        {
            return await this._context.TicketEscalations.Include(t => t.Ticket).ThenInclude(m => m.Member).Include(a => a.Ticket.AssignedTo).Where(a => a.Status != Lambda.Deleted).ToListAsync();
        }

        public void Remove(TicketEscalation ticketEscalation)
        {
            //mark the record as deleted

            ticketEscalation.Status = Lambda.Deleted;
            ticketEscalation.DeletedDate = DateTime.UtcNow;
        }

        public Task<int> TotalCount()
        {
            return this._context.TicketEscalations.CountAsync(a => a.Status != Lambda.Deleted);
        }

        public Task<int> TotalCountForUser(string user)
        {
            return this._context.TicketEscalations.CountAsync(a => a.Status != Lambda.Deleted && a.CreatedById == user);
        }
    }
}
