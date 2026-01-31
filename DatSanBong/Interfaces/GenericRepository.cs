// Repositories/GenericRepository.cs
using DatSanBongDa.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq; // Cần cho IQueryable
using System.Text;
using System.Threading.Tasks;

using DatSanBong.Models; // Thay thế bằng namespace chứa DbContext của bạn

namespace DatSanBongDa.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private DatSanBongDaEntities _context;
        private DbSet<T> table;

        public GenericRepository(DatSanBongDaEntities context)
        {
            _context = context;
            table = _context.Set<T>();
        }

        public IEnumerable<T> GetAll()
        {
            return table.ToList();
        }

        // TRIỂN KHAI PHƯƠNG THỨC MỚI
        public IQueryable<T> AsQueryable()
        {
            return table.AsQueryable();
        }

        // Triển khai phương thức Add
        public void Add(T entity)
        {
            table.Add(entity);
        }
        public T GetById(object id)
        {
            return table.Find(id);
        }

        public void Insert(T obj)
        {
            table.Add(obj);
        }

        public void Update(T obj)
        {
            table.Attach(obj);
            _context.Entry(obj).State = EntityState.Modified;
        }

        public void Delete(object id)
        {
            T existing = table.Find(id);
            if (existing != null)
            {
                table.Remove(existing);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
