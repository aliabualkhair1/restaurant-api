using BLL.Interfaces;
using DAL.DbContext;
using DAL.Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Repositories
{
    public class GenericRepo<T> : IGeneric<T> where T : BaseEntity
    {
        private readonly RestaurantDB db;

        public GenericRepo(RestaurantDB Db)
        {
            db = Db;
        }
        public void Add(T entity)
        {
            db.Set<T>().Add(entity);
        }

        public void Delete(int id)
        {
            var entity = db.Set<T>().Find(id);
            db.Set<T>().Remove(entity);
        }

        public IQueryable<T> GetAll()
        {
           return  db.Set<T>();
        }

        public T GetById(int id)
        {
           return db.Set<T>().Find(id);
        }

        public void Update(T entity)
        {
           db.Set<T>().Update(entity);
        }
    }
}
