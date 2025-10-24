using DAL.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IUnitOfWork:IDisposable
    {
        IGeneric<T> Generic<T>() where T : BaseEntity;
        Task<int> Complete();
    }
}
