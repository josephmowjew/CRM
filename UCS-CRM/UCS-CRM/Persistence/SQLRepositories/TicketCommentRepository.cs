using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class TicketCommentRepository : ITicketCommentRepository
    {
        private readonly ApplicationDbContext _context;
        public TicketCommentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(TicketComment ticketComment)
        {
             this._context.TicketComments.Add(ticketComment);
        }

        public TicketComment? Exists(TicketComment ticketComment)
        {
            return this._context.TicketComments.Where(tc => tc.TicketId == ticketComment.TicketId & tc.Comment.ToLower().Trim() == ticketComment.Comment.ToLower().Trim()).FirstOrDefault();
        }

        public async Task<TicketComment?> GetTicketCommentAsync(int id)
        {
            return await this._context.TicketComments.FirstOrDefaultAsync(tc => tc.Id == id);
        }

        public async Task<List<TicketComment>?> GetTicketCommentsAsync(int ticketId, CursorParams @params)
        {
            //check if the count has a value in it above zero before proceeding

            if (@params.Take > 0)
            {
                //check if there is a search parameter
                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var records = (from tblOb in await this._context.TicketComments.OrderByDescending(t => t.Id).Include(t => t.CreatedBy).Where(t => t.Status != Lambda.Deleted && t.TicketId == ticketId).Take(@params.Take).Skip(@params.Skip).ToListAsync() select tblOb);

                    //accountTypes.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        records = records.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return records.ToList();
                }
                else
                {
                    //include search query

                    var records = (from tblOb in await this._context.TicketComments.OrderByDescending(t => t.Id).Include(t => t.CreatedBy)
                                   .Where(t => t.Status != Lambda.Deleted && t.TicketId == ticketId
                                        && t.Comment.Trim().Contains(@params.SearchTerm.Trim().ToLower()))
                                   .Take(@params.Take)
                                   .Skip(@params.Skip)
                                   .ToListAsync()
                                   select tblOb);

                    //accountTypes.AsQueryable().OrderBy("gjakdgdag");

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        records = records.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }

                    return records.ToList();
                }
            }
            else
            {
                return null;
            }
        }

        public void Remove(TicketComment ticketComment)
        {
            ticketComment.Status = Lambda.Deleted;
            ticketComment.DeletedDate = DateTime.UtcNow;
        }

        public async Task<int> TotalActiveCount(int ticketId)
        {
            return await this._context.TicketComments.CountAsync(t => t.Id == ticketId);
        }
    }
}
