using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities.Pagination
{
    public class Pagination<T>
    {
        public List<T> Items { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalRecords/(double)PageSize);
        public Pagination(List<T> items,int pagenumber,int pagesize,int totalrecords)
        {
         Items= items;
         PageNumber = pagenumber;
        PageSize= pagesize;
        TotalRecords= totalrecords;
        }
        public async static Task<Pagination<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
        {
            var totalRecords =await source.CountAsync();
            var items =await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new Pagination<T>(items, pageNumber, pageSize, totalRecords);
        }
    }
}
