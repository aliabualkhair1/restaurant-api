using BLL.Interfaces;
using DAL.DbContext;
using DAL.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly RestaurantDB db;

        public UnitOfWork(RestaurantDB Db)
        {
            db = Db;
        }
        public async Task<int> Complete()
        {
         return await db.SaveChangesAsync();
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public IGeneric<T> Generic<T>() where T : BaseEntity
        {
            return new GenericRepo<T>(db);
        }
    }
}
