using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace DatSanBong.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("DefaultConnection")
        {
        }
        public DbSet<Role> Roles { get; set; }
        public DbSet<LoaiSanBong> LoaiSanBongs { get; set; }
        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<SanBong> SanBongs { get; set; }
        public DbSet<DonDatSan> DonDatSans { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<TienIchSanBong> TienIchSanBongs { get; set; }
        public DbSet<PhuongThucThanhToan> PhuongThucThanhToans { get; set; }
        public DbSet<TrangThaiDonDat> TrangThaiDonDats { get; set; }
        public DbSet<ThongBao> ThongBaos { get; set; }
    }
}