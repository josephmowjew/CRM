using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;
using System.Security.Claims;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly ApplicationDbContext _context;

        public FeedbackRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(Feedback state)
        {
            //add record to the database
            this._context.Feedbacks.Add(state);
        }

        public Feedback? Exists(string name)
        {
            return this._context.Feedbacks.FirstOrDefault(s => name.ToLower().Trim() == s.Description.Trim().ToLower() && s.Status != Lambda.Deleted);
        }

        public Feedback? DefaultFeedback(string name)
        {
            return this._context.Feedbacks.FirstOrDefault(s => name.ToLower().Trim() == s.Description.Trim().ToLower() && s.Status != Lambda.Deleted);
        }

        public async Task<Feedback?> GetFeedbackAsync(int id)
        {
            return await this._context.Feedbacks.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Feedback>?> GetFeedbacks(CursorParams @params, ClaimsPrincipal user)
        {
            if (@params.Take > 0)
            {
                if (user.IsInRole(Lambda.Member))
                {
                    if (string.IsNullOrEmpty(@params.SearchTerm))
                    {
                        var stateList = (from tblObj in _context.Feedbacks.Where(s => s.Status != Lambda.Deleted && s.CreatedById == user.FindFirstValue(ClaimTypes.NameIdentifier)).Skip(@params.Skip).Take(@params.Take) select tblObj);

                        if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                        {
                            stateList = stateList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                        }


                        return stateList.ToList();

                    }
                    else
                    {
                        //include search text in the query
                        var stateList = (from tblOb in _context.Feedbacks.Where(s => s.Description.ToLower().Trim().Contains(@params.SearchTerm.ToLower().Trim()) && s.Status != Lambda.Deleted && s.CreatedById == user.FindFirstValue(ClaimTypes.NameIdentifier))
                                            .Skip(@params.Skip)
                                            .Take(@params.Take)
                                         select tblOb);

                        stateList = stateList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


                        return stateList.ToList();

                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(@params.SearchTerm))
                    {
                        var stateList = (from tblObj in _context.Feedbacks.Where(s => s.Status != Lambda.Deleted).Include(c => c.CreatedBy).Skip(@params.Skip).Take(@params.Take) select tblObj);

                        if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                        {
                            stateList = stateList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                        }


                        return stateList.ToList();

                    }
                    else
                    {
                        //include search text in the query
                        var stateList = (from tblOb in _context.Feedbacks.Where(s => s.Description.ToLower().Trim().Contains(@params.SearchTerm.ToLower().Trim()) && s.Status != Lambda.Deleted)
                                         .Include(c => c.CreatedBy)
                                            .Skip(@params.Skip)
                                            .Take(@params.Take)
                                         select tblOb);

                        stateList = stateList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


                        return stateList.ToList();

                    }
                }
                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var stateList = (from tblObj in _context.Feedbacks.Where(s => s.Status != Lambda.Deleted).Skip(@params.Skip).Take(@params.Take) select tblObj);

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        stateList = stateList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return stateList.ToList();

                }
                else
                {
                    //include search text in the query
                    var stateList = (from tblOb in _context.Feedbacks.Where(s => s.Description.ToLower().Trim().Contains(@params.SearchTerm.ToLower().Trim()) && s.Status != Lambda.Deleted)
                                        .Skip(@params.Skip)
                                        .Take(@params.Take)
                                     select tblOb);

                    stateList = stateList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


                    return stateList.ToList();

                }

            }

            return null;
        }

        public async Task<List<Feedback>?> GetFeedbacks()
        {
            return await this._context.Feedbacks.Where(a => a.Status != Lambda.Deleted).ToListAsync();
        }

        public void Remove(Feedback state)
        {
            state.DeletedDate = DateTime.Now;
            state.Status = Lambda.Deleted;
        }

        public async Task<int> TotalActiveCount(ClaimsPrincipal user)
        {
            return await this._context.Feedbacks.CountAsync(s => s.Status == Lambda.Active);
        }

        public async Task<int> TotalDeletedCount()
        {
            return await this._context.Feedbacks.CountAsync(s => s.Status == Lambda.Deleted);
        }
    }
}
