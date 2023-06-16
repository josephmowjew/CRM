using UCS_CRM.Core.Helpers;
using UCS_CRM.Core.Models;
using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;

namespace UCS_CRM.Persistence.SQLRepositories
{
    public class PositionRepository : IPositionRepository
    {
        private ApplicationDbContext _context;
        public PositionRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public void Add(Position position)
        {
            this._context.Positions.Add(position);
        }

        public Position? Exists(int id, string positionName, int rating)
        {
            if(id < 1)
            {
                return this._context.Positions.FirstOrDefault(d => d.Name.Trim().ToLower() == positionName.Trim().ToLower() && d.Status != Lambda.Deleted);

            }
            else {
                return this._context.Positions.FirstOrDefault(d => d.Name.Trim().ToLower() == positionName.Trim().ToLower() && d.Rating == rating && d.Status != Lambda.Deleted);

            }
        }

        public Task<Position?> GetPosition(int id)
        {
            return this._context.Positions.FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<Position>?> GetPositions(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                //check if there is a search term sent 

                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var positions = (from tblOb in await this._context.Positions.Where(d => d.Status != Lambda.Deleted).Skip(@params.Skip).Take(@params.Take).ToListAsync() select tblOb);


                    if (string.IsNullOrEmpty(@params.SortColum) && !string.IsNullOrEmpty(@params.SortDirection))
                    {
                        positions = positions.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);

                    }


                    return positions.ToList();

                }
                else
                {
                    //include search text in the query
                    var positions = (from tblOb in await this._context.Positions
                                        .Where(a => a.Name.ToLower().Contains(@params.SearchTerm) & a.Status != Lambda.Deleted)
                                        .Skip(@params.Skip)
                                        .Take(@params.Take)
                                        .ToListAsync()
                                       select tblOb);

                    positions = positions.AsQueryable().OrderBy(@params.SortColum + " " + @params.SortDirection);


                    return positions.ToList();

                }

            }
            else
            {
                return null;
            }
        }

        public async Task<List<Position>?> GetPositions()
        {
            return await this._context.Positions.Where(d => d.Status != Lambda.Deleted).ToListAsync();
        }

        public void Remove(Position position)
        {
            position.Status = Lambda.Deleted;
            position.DeletedDate = DateTime.Now;
        }

        public async Task<int> TotalCount()
        {
            return await this._context.Positions.CountAsync();
        }

        public async Task<int> TotalCountFiltered(CursorParams @params)
        {
            if (@params.Take > 0)
            {
                //check if there is a search term sent 

                if (string.IsNullOrEmpty(@params.SearchTerm))
                {
                    var positions = (from tblOb in await this._context.Positions.Where(d => d.Status != Lambda.Deleted).ToListAsync() select tblOb);


                    return positions.Count();

                }
                else
                {
                    //include search text in the query
                    var positions = (from tblOb in await this._context.Positions
                                        .Where(a => a.Name.ToLower().Contains(@params.SearchTerm) & a.Status != Lambda.Deleted)
                                        .ToListAsync()
                                       select tblOb);


                    return positions.Count();

                }

            }
            else
            {
                return 0;
            }
        }
    }
}
