-- =========================================
-- DATABASE CREATION SCRIPT
-- Football Field Booking System (DatSanBong)
-- Date: 2024
-- Version: 1.0
-- =========================================

USE master;
GO

-- Drop database if exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'DatSanBong')
BEGIN
    ALTER DATABASE DatSanBong SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE DatSanBong;
END
GO

-- Create new database
CREATE DATABASE DatSanBong;
GO

USE DatSanBong;
GO

-- =========================================
-- 1. ROLES TABLE
-- =========================================
CREATE TABLE Roles (
    IDrole INT PRIMARY KEY IDENTITY(1,1),
    Role_name NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- =========================================
-- 2. NGUOI DUNG (USERS) TABLE
-- =========================================
CREATE TABLE NguoiDung (
    IDuser INT PRIMARY KEY IDENTITY(1,1),
    username NVARCHAR(50) NOT NULL UNIQUE,
    email NVARCHAR(100) NOT NULL UNIQUE,
    pass_hash NVARCHAR(255) NOT NULL,
    fullname NVARCHAR(100) NULL,
    sdt NVARCHAR(20) NULL UNIQUE,
    DiaChi NVARCHAR(255) NULL,
    trangthaiTK BIT NOT NULL DEFAULT 1, -- 1: Active, 0: Inactive
    last_log DATETIME NULL,
    create_at DATETIME NOT NULL DEFAULT GETDATE(),
    update_at DATETIME NOT NULL DEFAULT GETDATE(),
    IDrole INT NOT NULL,
    CONSTRAINT FK_NguoiDung_Roles FOREIGN KEY (IDrole) REFERENCES Roles(IDrole)
);
GO

-- =========================================
-- 3. LOAI SAN BONG (FIELD TYPES) TABLE
-- =========================================
CREATE TABLE LoaiSanBong (
    IDLoaiSan INT PRIMARY KEY IDENTITY(1,1),
    LoaiSan NVARCHAR(50) NOT NULL UNIQUE -- e.g., "Sân 5 ng??i", "Sân 7 ng??i", "Sân 11 ng??i"
);
GO

-- =========================================
-- 4. SAN BONG (FOOTBALL FIELDS) TABLE
-- =========================================
CREATE TABLE SanBong (
    IDSanBong INT PRIMARY KEY IDENTITY(1,1),
    TenSanBong NVARCHAR(100) NOT NULL,
    DiaChi NVARCHAR(255) NOT NULL,
    MoTaKichThuoc NVARCHAR(100) NULL,
    GiaThue DECIMAL(18,2) NOT NULL,
    MoTaSan NVARCHAR(MAX) NULL,
    AnhSan NVARCHAR(255) NULL, -- Image filename
    TrangThaiSan_ BIT NOT NULL DEFAULT 1, -- 1: Active, 0: Inactive
    AverageDanhGia DECIMAL(3,2) NOT NULL DEFAULT 0.00, -- Average rating 0.00 - 5.00
    TongLuotDanhGia INT NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    IDLoaiSan INT NOT NULL,
    CONSTRAINT FK_SanBong_LoaiSanBong FOREIGN KEY (IDLoaiSan) REFERENCES LoaiSanBong(IDLoaiSan),
    CONSTRAINT CHK_AverageDanhGia CHECK (AverageDanhGia >= 0.00 AND AverageDanhGia <= 5.00)
);
GO

-- =========================================
-- 5. TIEN ICH SAN BONG (FIELD AMENITIES) TABLE
-- =========================================
CREATE TABLE TienIchSanBong (
    IDTienIch INT PRIMARY KEY IDENTITY(1,1),
    TenTienIch NVARCHAR(100) NOT NULL UNIQUE, -- e.g., "Wifi", "Parking", "Shower", "Changing Room"
    MoTaTienIch NVARCHAR(255) NULL
);
GO

-- =========================================
-- 6. SAN BONG - TIEN ICH (MANY-TO-MANY) TABLE
-- =========================================
CREATE TABLE SanBong_TienIch (
    IDSanBong INT NOT NULL,
    IDTienIch INT NOT NULL,
    PRIMARY KEY (IDSanBong, IDTienIch),
    CONSTRAINT FK_SanBongTienIch_SanBong FOREIGN KEY (IDSanBong) REFERENCES SanBong(IDSanBong) ON DELETE CASCADE,
    CONSTRAINT FK_SanBongTienIch_TienIch FOREIGN KEY (IDTienIch) REFERENCES TienIchSanBong(IDTienIch) ON DELETE CASCADE
);
GO

-- =========================================
-- 7. PHUONG THUC THANH TOAN (PAYMENT METHODS) TABLE
-- =========================================
CREATE TABLE PhuongThucThanhToan (
    IDPT INT PRIMARY KEY IDENTITY(1,1),
    TenPT NVARCHAR(50) NOT NULL UNIQUE -- e.g., "Ti?n m?t", "Chuy?n kho?n", "VNPay", "Momo"
);
GO

-- =========================================
-- 8. TRANG THAI DON DAT (BOOKING STATUS) TABLE
-- =========================================
CREATE TABLE TrangThaiDonDat (
    IDstatus INT PRIMARY KEY IDENTITY(1,1),
    Tenstatus NVARCHAR(50) NOT NULL UNIQUE -- e.g., "Ch? duy?t", "?ã duy?t", "Hoàn thành", "?ã h?y"
);
GO

-- =========================================
-- 9. DON DAT SAN (BOOKINGS) TABLE
-- =========================================
CREATE TABLE DonDatSan (
    IDBooking INT PRIMARY KEY IDENTITY(1,1),
    NgayBooking DATE NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    TongTien DECIMAL(18,2) NOT NULL,
    TTThanhToan BIT NOT NULL DEFAULT 0, -- 0: Unpaid, 1: Paid
    admin_notes NVARCHAR(MAX) NULL,
    LyDoHuy NVARCHAR(MAX) NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    IDuser INT NOT NULL,
    IDPT INT NULL,
    IDstatus INT NULL,
    IDSanBong INT NOT NULL,
    CONSTRAINT FK_DonDatSan_NguoiDung FOREIGN KEY (IDuser) REFERENCES NguoiDung(IDuser),
    CONSTRAINT FK_DonDatSan_PhuongThucThanhToan FOREIGN KEY (IDPT) REFERENCES PhuongThucThanhToan(IDPT),
    CONSTRAINT FK_DonDatSan_TrangThaiDonDat FOREIGN KEY (IDstatus) REFERENCES TrangThaiDonDat(IDstatus),
    CONSTRAINT FK_DonDatSan_SanBong FOREIGN KEY (IDSanBong) REFERENCES SanBong(IDSanBong),
    CONSTRAINT CHK_EndTime CHECK (end_time > start_time)
);
GO

-- =========================================
-- 10. REVIEWS TABLE
-- =========================================
CREATE TABLE Reviews (
    IDReview INT PRIMARY KEY IDENTITY(1,1),
    Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    Comment NVARCHAR(MAX) NULL,
    is_approved BIT NOT NULL DEFAULT 0, -- 0: Pending, 1: Approved
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    IDuser INT NOT NULL,
    IDSanBong INT NOT NULL,
    IDBooking INT NULL,
    CONSTRAINT FK_Reviews_NguoiDung FOREIGN KEY (IDuser) REFERENCES NguoiDung(IDuser),
    CONSTRAINT FK_Reviews_SanBong FOREIGN KEY (IDSanBong) REFERENCES SanBong(IDSanBong) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_DonDatSan FOREIGN KEY (IDBooking) REFERENCES DonDatSan(IDBooking)
);
GO

-- =========================================
-- 11. SU CO (INCIDENTS) TABLE
-- =========================================
CREATE TABLE SuCo (
    IDSuCo INT PRIMARY KEY IDENTITY(1,1),
    LoaiSuCo NVARCHAR(100) NOT NULL,
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'M?i', -- "M?i", "?ang x? lý", "?ã gi?i quy?t", "Không h?p l?"
    resolution_notes NVARCHAR(MAX) NULL,
    reported_at DATETIME NOT NULL DEFAULT GETDATE(),
    resolved_at DATETIME NULL,
    MoTa NVARCHAR(MAX) NULL,
    IDuser INT NULL,
    IDBooking INT NULL,
    IDSanBong INT NULL,
    IDAdmin INT NULL, -- Employee/Admin handling the incident
    user_backup_info NVARCHAR(255) NULL,
    booking_backup_info NVARCHAR(255) NULL,
    sanBong_backup_info NVARCHAR(255) NULL,
    admin_backup_info NVARCHAR(255) NULL,
    CONSTRAINT FK_SuCo_NguoiDung FOREIGN KEY (IDuser) REFERENCES NguoiDung(IDuser),
    CONSTRAINT FK_SuCo_DonDatSan FOREIGN KEY (IDBooking) REFERENCES DonDatSan(IDBooking),
    CONSTRAINT FK_SuCo_SanBong FOREIGN KEY (IDSanBong) REFERENCES SanBong(IDSanBong),
    CONSTRAINT FK_SuCo_Admin FOREIGN KEY (IDAdmin) REFERENCES NguoiDung(IDuser)
);
GO

-- =========================================
-- 12. PHAN CONG NHAN VIEN (STAFF ASSIGNMENTS) TABLE
-- =========================================
CREATE TABLE PhanCongNhanVien (
    IDPhanCong INT PRIMARY KEY IDENTITY(1,1),
    NgayPhanCong DATE NOT NULL,
    ChiTiet NVARCHAR(MAX) NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME NOT NULL DEFAULT GETDATE(),
    IDuser INT NOT NULL, -- Employee
    IDSanBong INT NOT NULL,
    IDAdmin INT NOT NULL, -- Admin who created the assignment
    CONSTRAINT FK_PhanCongNhanVien_Employee FOREIGN KEY (IDuser) REFERENCES NguoiDung(IDuser),
    CONSTRAINT FK_PhanCongNhanVien_SanBong FOREIGN KEY (IDSanBong) REFERENCES SanBong(IDSanBong),
    CONSTRAINT FK_PhanCongNhanVien_Admin FOREIGN KEY (IDAdmin) REFERENCES NguoiDung(IDuser)
);
GO

-- =========================================
-- 13. THONG BAO (NOTIFICATIONS) TABLE
-- =========================================
CREATE TABLE ThongBao (
    IDThongBao INT PRIMARY KEY IDENTITY(1,1),
    NoiDung NVARCHAR(MAX) NOT NULL,
    is_read BIT NOT NULL DEFAULT 0, -- 0: Unread, 1: Read
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    IDuser INT NOT NULL,
    CONSTRAINT FK_ThongBao_NguoiDung FOREIGN KEY (IDuser) REFERENCES NguoiDung(IDuser) ON DELETE CASCADE
);
GO

-- =========================================
-- CREATE INDEXES FOR PERFORMANCE
-- =========================================

-- Users
CREATE NONCLUSTERED INDEX IX_NguoiDung_Username ON NguoiDung(username);
CREATE NONCLUSTERED INDEX IX_NguoiDung_Email ON NguoiDung(email);
CREATE NONCLUSTERED INDEX IX_NguoiDung_Role ON NguoiDung(IDrole);
GO

-- Bookings
CREATE NONCLUSTERED INDEX IX_DonDatSan_User ON DonDatSan(IDuser);
CREATE NONCLUSTERED INDEX IX_DonDatSan_Field ON DonDatSan(IDSanBong);
CREATE NONCLUSTERED INDEX IX_DonDatSan_Date ON DonDatSan(NgayBooking);
CREATE NONCLUSTERED INDEX IX_DonDatSan_Status ON DonDatSan(IDstatus);
CREATE NONCLUSTERED INDEX IX_DonDatSan_DateField ON DonDatSan(NgayBooking, IDSanBong);
GO

-- Reviews
CREATE NONCLUSTERED INDEX IX_Reviews_Field ON Reviews(IDSanBong);
CREATE NONCLUSTERED INDEX IX_Reviews_User ON Reviews(IDuser);
CREATE NONCLUSTERED INDEX IX_Reviews_Approved ON Reviews(is_approved);
GO

-- Incidents
CREATE NONCLUSTERED INDEX IX_SuCo_Status ON SuCo(TrangThai);
CREATE NONCLUSTERED INDEX IX_SuCo_Field ON SuCo(IDSanBong);
CREATE NONCLUSTERED INDEX IX_SuCo_User ON SuCo(IDuser);
GO

-- Assignments
CREATE NONCLUSTERED INDEX IX_PhanCongNhanVien_Employee ON PhanCongNhanVien(IDuser);
CREATE NONCLUSTERED INDEX IX_PhanCongNhanVien_Field ON PhanCongNhanVien(IDSanBong);
CREATE NONCLUSTERED INDEX IX_PhanCongNhanVien_Date ON PhanCongNhanVien(NgayPhanCong);
GO

-- =========================================
-- INSERT DEFAULT DATA
-- =========================================

-- 1. Insert Roles
SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (IDrole, Role_name) VALUES 
(1, N'Customer'),
(2, N'Employee'),
(3, N'Admin');
SET IDENTITY_INSERT Roles OFF;
GO

-- 2. Insert Default Admin User
-- Password: Admin@123 (hashed with BCrypt)
INSERT INTO NguoiDung (username, email, pass_hash, fullname, IDrole, trangthaiTK)
VALUES (
    N'admin',
    N'admin@datsanbong.com',
    N'$2a$11$xGDj5JQ5YyQZxYYy5xYy5.5xYy5xYy5xYy5xYy5xYy5xYy5xYy5xY', -- You need to generate real BCrypt hash
    N'Qu?n Tr? Viên',
    3,
    1
);
GO

-- 3. Insert Loai San Bong
INSERT INTO LoaiSanBong (LoaiSan) VALUES 
(N'Sân 5 ng??i'),
(N'Sân 7 ng??i'),
(N'Sân 11 ng??i');
GO

-- 4. Insert Payment Methods
INSERT INTO PhuongThucThanhToan (TenPT) VALUES 
(N'Ti?n m?t'),
(N'Chuy?n kho?n ngân hàng'),
(N'VNPay'),
(N'Momo'),
(N'ZaloPay');
GO

-- 5. Insert Booking Statuses
INSERT INTO TrangThaiDonDat (Tenstatus) VALUES 
(N'Ch? duy?t'),
(N'?ã duy?t'),
(N'Hoàn thành'),
(N'?ã h?y');
GO

-- 6. Insert Amenities
INSERT INTO TienIchSanBong (TenTienIch, MoTaTienIch) VALUES 
(N'Wifi mi?n phí', N'K?t n?i internet không dây t?c ?? cao'),
(N'Bãi ?? xe', N'Bãi ?? xe r?ng rãi, an toàn'),
(N'Phòng thay ??', N'Phòng thay ?? s?ch s?, ti?n nghi'),
(N'Nhà v? sinh', N'Nhà v? sinh hi?n ??i, s?ch s?'),
(N'Khu v?c hút thu?c', N'Khu v?c riêng bi?t cho ng??i hút thu?c'),
(N'C?ng tin', N'Cung c?p ?? ?n, n??c u?ng'),
(N'Camera an ninh', N'H? th?ng camera giám sát 24/7'),
(N'Ánh sáng ?èn', N'H? th?ng chi?u sáng hi?n ??i cho ca t?i');
GO

-- 7. Insert Sample Football Fields
INSERT INTO SanBong (TenSanBong, DiaChi, MoTaKichThuoc, GiaThue, MoTaSan, TrangThaiSan_, IDLoaiSan) VALUES 
(N'Sân Th? Thao Qu?n 1', N'123 ???ng Nguy?n Hu?, Qu?n 1, TP.HCM', N'40m x 20m', 500000, N'Sân bóng hi?n ??i, m?t c? nhân t?o ch?t l??ng cao', 1, 1),
(N'Sân Bóng Bình Th?nh', N'456 ???ng Xô Vi?t Ngh? T?nh, Bình Th?nh, TP.HCM', N'50m x 30m', 700000, N'Sân r?ng, thoáng mát, phù h?p cho gi?i ??u', 1, 2),
(N'Sân V?n ??ng Gò V?p', N'789 ???ng Quang Trung, Gò V?p, TP.HCM', N'70m x 50m', 1200000, N'Sân chu?n 11 ng??i, có khán ?ài', 1, 3);
GO

-- 8. Link Fields with Amenities
INSERT INTO SanBong_TienIch (IDSanBong, IDTienIch)
SELECT s.IDSanBong, t.IDTienIch
FROM SanBong s, TienIchSanBong t
WHERE s.IDSanBong IN (1, 2, 3) AND t.IDTienIch IN (1, 2, 3, 4, 7, 8);
GO

PRINT N'=========================================';
PRINT N'Database DatSanBong created successfully!';
PRINT N'=========================================';
PRINT N'';
PRINT N'Default Data Inserted:';
PRINT N'- 3 Roles (Customer, Employee, Admin)';
PRINT N'- 1 Admin User (username: admin)';
PRINT N'- 3 Field Types';
PRINT N'- 5 Payment Methods';
PRINT N'- 4 Booking Statuses';
PRINT N'- 8 Amenities';
PRINT N'- 3 Sample Football Fields';
PRINT N'';
PRINT N'Next Steps:';
PRINT N'1. Update Admin password hash with real BCrypt hash';
PRINT N'2. Add more sample data as needed';
PRINT N'3. Configure connection string in Web.config';
PRINT N'=========================================';
GO
