// Interfaces/IUnitOfWork.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DatSanBong.Models;
using System.Web.Security;
using DatSanBong.Models; 
using DatSanBongDa.Repositories; 

namespace DatSanBongDa.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Khai báo các Repository cụ thể mà bạn muốn truy cập
        // Ví dụ:
        IGenericRepository<NguoiDung> NguoiDungRepository { get; }
        IGenericRepository<SanBong> SanBongRepository { get; }
        IGenericRepository<DonDatSan> DonDatSanRepository { get; }
        IGenericRepository<Review> ReviewsRepository { get; }
        IGenericRepository<Role> RolesRepository { get; }
        IGenericRepository<LoaiSanBong> LoaiSanBongRepository { get; }
        IGenericRepository<TienIchSanBong> TienIchSanBongRepository { get; }
        IGenericRepository<PhuongThucThanhToan> PhuongThucThanhToanRepository { get; }
        IGenericRepository<TrangThaiDonDat> TrangThaiDonDatRepository { get; }
        IGenericRepository<ThongBao> ThongBaoRepository { get; }
        IGenericRepository<SuCo> SuCoRepository { get; }
        IGenericRepository<PhanCongNhanVien> PhanCongNhanVienRepository { get; }
        
        void Save();
    }
}
