using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using UCS_CRM.Persistence.Interfaces;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class StateRepository : IStateRepository
    {
        private readonly ApplicationDbContext _context;

        public StateRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(State state)
        {
            //add record to the database
            this._context.States.Add(state);
        }

        public State? Exists(string name)
        {
            return this._context.States.FirstOrDefault(s => name.ToLower().Trim() == s.Name.Trim().ToLower() && s.Status != Lambda.Deleted);
        }

        public State? DefaultState(string name) 
        {
            return this._context.States.FirstOrDefault(s => name.ToLower().Trim() == s.Name.Trim().ToLower() && s.Status != Lambda.Deleted);
        }

        public async Task<State?> GetStateAsync(int id)
        {
            return await this._context.States.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<State>?> GetStates(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var stateList = (from tblObj in _context.States.Where(s => s.Status != Lambda.Deleted).Skip(@params.Skip).Take(@params.Take) select tblObj);

                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        stateList = stateList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return stateList.ToList();

                }
                else
                {
                    //include search text in the query
                    var stateList = (from tblOb in _context.States.Where(s => s.Name.ToLower().Trim().Contains(@params.SearchTerm.ToLower().Trim()) && s.Status != Lambda.Deleted)
                                        .Skip(@params.Skip)
                                        .Take(@params.Take)
                                             select tblOb);

                    stateList = stateList.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


                    return stateList.ToList();

                }

            }

            return null;
        }

        public async Task<List<State>?> GetStates()
        {
            return await this._context.States.Where(a => a.Status != Lambda.Deleted).ToListAsync();
        }

        public void Remove(State state)
        {
            state.DeletedDate = DateTime.Now;
            state.Status = Lambda.Deleted;
        }

        public async Task<int> TotalActiveCount()
        {
           return await this._context.States.CountAsync(s => s.Status == Lambda.Active);
        }

        public async Task<int> TotalDeletedCount()
        {
            return await this._context.States.CountAsync(s => s.Status == Lambda.Deleted);
        }
    }
}
