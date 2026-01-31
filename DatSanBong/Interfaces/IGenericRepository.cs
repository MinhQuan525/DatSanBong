// Interfaces/IGenericRepository.cs
using System;
using System.Collections.Generic;
using System.Linq; // Cần cho IQueryable
using System.Text;
using System.Threading.Tasks;

namespace DatSanBongDa.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        // Lấy tất cả các bản ghi của thực thể T (tải ngay lập tức)
        IEnumerable<T> GetAll();

        IQueryable<T> AsQueryable(); // THÊM DÒNG NÀY

        // Lấy một bản ghi của thực thể T theo ID
        T GetById(object id);
        void Add(T entity);

        // Thêm một bản ghi mới của thực thể T
        void Insert(T obj);

        // Cập nhật một bản ghi của thực thể T
        void Update(T obj);

        // Xóa một bản ghi của thực thể T theo ID
        void Delete(object id);

        // Lưu các thay đổi vào cơ sở dữ liệu
        void Save();
    }
}
