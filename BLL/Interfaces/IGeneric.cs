using DAL.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IGeneric<T> where T :BaseEntity
    {
        IQueryable<T> GetAll();
        T GetById(int id);
        int Add(T entity);
        int Update(T entity);
        int Delete(int id);
        T GetByCondition(Expression<Func<T, bool>> predicate);
    }
}
