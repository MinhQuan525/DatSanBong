// UnitOfWork/UnitOfWork.cs
using DatSanBong.Models;
using DatSanBongDa.Interfaces;
using DatSanBong.Models;
using DatSanBongDa.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;

namespace DatSanBongDa.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        // DbContext của Entity Framework
        private DatSanBongDaEntities _context; 

        // Khai báo các private field cho từng Repository
        private IGenericRepository<NguoiDung> _nguoiDungRepository;
        private IGenericRepository<SanBong> _sanBongRepository;
        private IGenericRepository<DonDatSan> _donDatSanRepository;
        private IGenericRepository<Review> _reviewsRepository;
        private IGenericRepository<Role> _rolesRepository;
        private IGenericRepository<LoaiSanBong> _loaiSanBongRepository;
        private IGenericRepository<TienIchSanBong> _tienIchSanBongRepository;
        private IGenericRepository<PhuongThucThanhToan> _phuongThucThanhToanRepository;
        private IGenericRepository<TrangThaiDonDat> _trangThaiDonDatRepository;
        private IGenericRepository<ThongBao> _thongBaoRepository;
        private IGenericRepository<SuCo> _suCoRepository;
        private IGenericRepository<PhanCongNhanVien> _phanCongNhanVienRepository;

        public UnitOfWork()
        {
            _context = new DatSanBongDaEntities();
        }

        // Triển khai các thuộc tính Repository
        public IGenericRepository<NguoiDung> NguoiDungRepository
        {
            get { return _nguoiDungRepository ?? (_nguoiDungRepository = new GenericRepository<NguoiDung>(_context)); }
        }

        public IGenericRepository<SanBong> SanBongRepository
        {
            get { return _sanBongRepository ?? (_sanBongRepository = new GenericRepository<SanBong>(_context)); }
        }

        public IGenericRepository<DonDatSan> DonDatSanRepository
        {
            get { return _donDatSanRepository ?? (_donDatSanRepository = new GenericRepository<DonDatSan>(_context)); }
        }

        public IGenericRepository<Review> ReviewsRepository
        {
            get { return _reviewsRepository ?? (_reviewsRepository = new GenericRepository<Review>(_context)); }
        }

        public IGenericRepository<Role> RolesRepository
        {
            get { return _rolesRepository ?? (_rolesRepository = new GenericRepository<Role>(_context)); }
        }

        public IGenericRepository<LoaiSanBong> LoaiSanBongRepository
        {
            get { return _loaiSanBongRepository ?? (_loaiSanBongRepository = new GenericRepository<LoaiSanBong>(_context)); }
        }

        public IGenericRepository<TienIchSanBong> TienIchSanBongRepository
        {
            get { return _tienIchSanBongRepository ?? (_tienIchSanBongRepository = new GenericRepository<TienIchSanBong>(_context)); }
        }

        public IGenericRepository<PhuongThucThanhToan> PhuongThucThanhToanRepository
        {
            get { return _phuongThucThanhToanRepository ?? (_phuongThucThanhToanRepository = new GenericRepository<PhuongThucThanhToan>(_context)); }
        }

        public IGenericRepository<TrangThaiDonDat> TrangThaiDonDatRepository
        {
            get { return _trangThaiDonDatRepository ?? (_trangThaiDonDatRepository = new GenericRepository<TrangThaiDonDat>(_context)); }
        }

        public IGenericRepository<ThongBao> ThongBaoRepository
        {
            get { return _thongBaoRepository ?? (_thongBaoRepository = new GenericRepository<ThongBao>(_context)); }
        }

        public IGenericRepository<SuCo> SuCoRepository
        {
            get { return _suCoRepository ?? (_suCoRepository = new GenericRepository<SuCo>(_context)); }
        }

        public IGenericRepository<PhanCongNhanVien> PhanCongNhanVienRepository
        {
            get { return _phanCongNhanVienRepository ?? (_phanCongNhanVienRepository = new GenericRepository<PhanCongNhanVien>(_context)); }
        }

        public void Save()
        {
            try
            {
                _context.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // Lỗi validation từ Entity Framework
                var errorMessages = new System.Text.StringBuilder();
                errorMessages.AppendLine("Entity Validation Errors:");
                
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        var errorMsg = string.Format("Property: {0}, Error: {1}", 
                            validationError.PropertyName, 
                            validationError.ErrorMessage);
                        errorMessages.AppendLine(errorMsg);
                        System.Diagnostics.Debug.WriteLine(errorMsg);
                    }
                }
                
                throw new Exception(errorMessages.ToString(), ex);
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                // Lỗi update database (constraint violations, etc.)
                var errorMessages = new System.Text.StringBuilder();
                errorMessages.AppendLine("Database Update Error:");
                
                var innerException = ex.InnerException;
                while (innerException != null)
                {
                    errorMessages.AppendLine(innerException.Message);
                    System.Diagnostics.Debug.WriteLine(innerException.Message);
                    innerException = innerException.InnerException;
                }
                
                throw new Exception(errorMessages.ToString(), ex);
            }
        }

        // Triển khai IDisposable để giải phóng tài nguyên DbContext
        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
