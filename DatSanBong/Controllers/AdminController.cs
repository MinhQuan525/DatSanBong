// Controllers/AdminController.cs
using DatSanBong.Models;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Collections.Generic;
using BCrypt.Net;
using System;
using System.IO;
using System.Web;

namespace DatSanBongDa.Controllers
{
    public class AdminController : AuthorizedBaseController // THAY ĐỔI: Kế thừa AuthorizedBaseController
    {
        // GET: Admin/Index
        public ActionResult Index()
        {
            // Trang tổng quan cho Admin
            ViewBag.TotalUsers = _unitOfWork.NguoiDungRepository.GetAll().Count();
            ViewBag.TotalFields = _unitOfWork.SanBongRepository.GetAll().Count();
            ViewBag.PendingBookings = _unitOfWork.DonDatSanRepository.AsQueryable()
                                                .Count(b => b.TrangThaiDonDat.Tenstatus == "Chờ duyệt");
            ViewBag.TotalBookings = _unitOfWork.DonDatSanRepository.GetAll().Count();
            return View();
        }

        #region Quản lý Người dùng (Users)

        // GET: Admin/Users
        public ActionResult Users()
        {
            var users = _unitOfWork.NguoiDungRepository.AsQueryable()
                                   .Include(u => u.Role) // Tải thông tin vai trò
                                   .ToList();
            return View(users);
        }

        // GET: Admin/CreateUser
        [HttpGet]
        public ActionResult CreateUser()
        {
            ViewBag.IDrole = new SelectList(_unitOfWork.RolesRepository.GetAll(), "IDrole", "Role_name");
            return View();
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUser(NguoiDung user)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng username/email/sdt
                if (_unitOfWork.NguoiDungRepository.AsQueryable().Any(u => u.username == user.username))
                {
                    ModelState.AddModelError("username", "Tên đăng nhập đã tồn tại.");
                }
                if (_unitOfWork.NguoiDungRepository.AsQueryable().Any(u => u.email == user.email))
                {
                    ModelState.AddModelError("email", "Email đã tồn tại.");
                }
                if (!string.IsNullOrEmpty(user.sdt) && _unitOfWork.NguoiDungRepository.AsQueryable().Any(u => u.sdt == user.sdt))
                {
                    ModelState.AddModelError("sdt", "Số điện thoại đã tồn tại.");
                }

                if (ModelState.IsValid)
                {
                    user.pass_hash = BCrypt.Net.BCrypt.HashPassword(user.pass_hash); // Hash mật khẩu
                    user.create_at = DateTime.Now;
                    user.update_at = DateTime.Now;
                    user.trangthaiTK = true; // Mặc định tài khoản hoạt động

                    _unitOfWork.NguoiDungRepository.Insert(user);
                    _unitOfWork.Save();
                    TempData["SuccessMessage"] = "Người dùng đã được tạo thành công!";
                    return RedirectToAction("Users");
                }
            }
            ViewBag.IDrole = new SelectList(_unitOfWork.RolesRepository.GetAll(), "IDrole", "Role_name", user.IDrole);
            return View(user);
        }

        // GET: Admin/EditUser/5
        [HttpGet]
        public ActionResult EditUser(int id)
        {
            var user = _unitOfWork.NguoiDungRepository.GetById(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            ViewBag.IDrole = new SelectList(_unitOfWork.RolesRepository.GetAll(), "IDrole", "Role_name", user.IDrole);
            // Không hiển thị pass_hash ra view
            user.pass_hash = null;
            return View(user);
        }

        // POST: Admin/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(NguoiDung user)
        {
            // Lấy người dùng hiện tại từ DB để giữ lại pass_hash nếu không đổi
            var existingUser = _unitOfWork.NguoiDungRepository.GetById(user.IDuser);
            if (existingUser == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra trùng email/sdt (loại trừ chính người dùng đang sửa)
            if (_unitOfWork.NguoiDungRepository.AsQueryable().Any(u => u.email == user.email && u.IDuser != user.IDuser))
            {
                ModelState.AddModelError("email", "Email đã tồn tại.");
            }
            if (!string.IsNullOrEmpty(user.sdt) && _unitOfWork.NguoiDungRepository.AsQueryable().Any(u => u.sdt == user.sdt && u.IDuser != user.IDuser))
            {
                ModelState.AddModelError("sdt", "Số điện thoại đã tồn tại.");
            }

            if (ModelState.IsValid)
            {
                existingUser.fullname = user.fullname;
                existingUser.email = user.email;
                existingUser.sdt = user.sdt;
                existingUser.DiaChi = user.DiaChi;
                existingUser.trangthaiTK = user.trangthaiTK;
                existingUser.IDrole = user.IDrole;
                existingUser.update_at = DateTime.Now;

                // Nếu có nhập mật khẩu mới thì hash và cập nhật
                if (!string.IsNullOrEmpty(user.pass_hash))
                {
                    existingUser.pass_hash = BCrypt.Net.BCrypt.HashPassword(user.pass_hash);
                }

                _unitOfWork.NguoiDungRepository.Update(existingUser);
                _unitOfWork.Save();
                TempData["SuccessMessage"] = "Người dùng đã được cập nhật thành công!";
                return RedirectToAction("Users");
            }
            ViewBag.IDrole = new SelectList(_unitOfWork.RolesRepository.GetAll(), "IDrole", "Role_name", user.IDrole);
            // Gán lại pass_hash null để không hiển thị ra view
            user.pass_hash = null;
            return View(user);
        }

        // POST: Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id)
        {
            // Không cho phép xóa tài khoản Admin đang đăng nhập
            if (id == (int)Session["UserID"])
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản Admin đang đăng nhập.";
                return RedirectToAction("Users");
            }

            _unitOfWork.NguoiDungRepository.Delete(id);
            _unitOfWork.Save();
            TempData["SuccessMessage"] = "Người dùng đã được xóa thành công!";
            return RedirectToAction("Users");
        }

        #endregion

        #region Quản lý Sân bóng (Fields)

        // GET: Admin/Fields
        public ActionResult Fields()
        {
            var fields = _unitOfWork.SanBongRepository.AsQueryable()
                                    .Include(s => s.LoaiSanBong)
                                    .ToList();
            return View(fields);
        }

        // GET: Admin/CreateField
        [HttpGet]
        public ActionResult CreateField()
        {
            ViewBag.IDLoaiSan = new SelectList(_unitOfWork.LoaiSanBongRepository.GetAll(), "IDLoaiSan", "LoaiSan");
            ViewBag.TienIchList = _unitOfWork.TienIchSanBongRepository.GetAll().ToList();
            return View();
        }

        // POST: Admin/CreateField
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateField(SanBong sanBong, List<int> selectedTienIchIds, HttpPostedFileBase AnhSan)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng tên sân và địa chỉ
                if (_unitOfWork.SanBongRepository.AsQueryable().Any(s => s.TenSanBong == sanBong.TenSanBong && s.DiaChi == sanBong.DiaChi))
                {
                    ModelState.AddModelError("", "Tên sân và địa chỉ đã tồn tại.");
                }

                // Xử lý upload ảnh
                string fileName = null;
                if (AnhSan != null && AnhSan.ContentLength > 0)
                {
                    // Kiểm tra định dạng file
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                    var fileExtension = Path.GetExtension(AnhSan.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("AnhSan", "Chỉ chấp nhận các định dạng ảnh: JPG, JPEG, PNG, GIF, BMP");
                    }
                    else if (AnhSan.ContentLength > 5 * 1024 * 1024) // 5MB
                    {
                        ModelState.AddModelError("AnhSan", "Kích thước file không được vượt quá 5MB");
                    }
                    else
                    {
                        try
                        {
                            // Tạo tên file unique để tránh trùng lặp
                            fileName = Guid.NewGuid().ToString() + fileExtension;

                            // Đường dẫn thư mục lưu ảnh
                            string uploadPath = Server.MapPath("~/Content/Images/");

                            // Tạo thư mục nếu chưa tồn tại
                            if (!Directory.Exists(uploadPath))
                            {
                                Directory.CreateDirectory(uploadPath);
                            }

                            // Lưu file
                            string filePath = Path.Combine(uploadPath, fileName);
                            AnhSan.SaveAs(filePath);

                            // Gán tên file vào model
                            sanBong.AnhSan = fileName;
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("AnhSan", "Có lỗi xảy ra khi upload ảnh: " + ex.Message);
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    sanBong.created_at = DateTime.Now;
                    sanBong.updated_at = DateTime.Now;
                    sanBong.TrangThaiSan_ = true; // Mặc định sân hoạt động
                    sanBong.AverageDanhGia = 0.00M;
                    sanBong.TongLuotDanhGia = 0;

                    // Thêm sân bóng trước để có IDSanBong
                    _unitOfWork.SanBongRepository.Insert(sanBong);
                    _unitOfWork.Save(); // Lưu để có IDSanBong

                    // Thêm tiện ích cho sân bóng
                    if (selectedTienIchIds != null && selectedTienIchIds.Any())
                    {
                        foreach (var tienIchId in selectedTienIchIds)
                        {
                            var tienIch = _unitOfWork.TienIchSanBongRepository.GetById(tienIchId);
                            if (tienIch != null)
                            {
                                sanBong.TienIchSanBongs.Add(tienIch);
                            }
                        }
                        _unitOfWork.Save(); // Lưu lại các thay đổi về tiện ích
                    }

                    TempData["SuccessMessage"] = "Sân bóng đã được tạo thành công!";
                    return RedirectToAction("Fields");
                }
            }

            ViewBag.IDLoaiSan = new SelectList(_unitOfWork.LoaiSanBongRepository.GetAll(), "IDLoaiSan", "LoaiSan", sanBong.IDLoaiSan);
            ViewBag.TienIchList = _unitOfWork.TienIchSanBongRepository.GetAll().ToList();
            return View(sanBong);
        }

        // GET: Admin/EditField/5
        [HttpGet]
        public ActionResult EditField(int id)
        {
            var sanBong = _unitOfWork.SanBongRepository.AsQueryable()
                                    .Include(s => s.TienIchSanBongs) // Tải tiện ích hiện có
                                    .FirstOrDefault(s => s.IDSanBong == id);
            if (sanBong == null)
            {
                return HttpNotFound();
            }
            ViewBag.IDLoaiSan = new SelectList(_unitOfWork.LoaiSanBongRepository.GetAll(), "IDLoaiSan", "LoaiSan", sanBong.IDLoaiSan);
            ViewBag.TienIchList = _unitOfWork.TienIchSanBongRepository.GetAll().ToList();
            // Lấy danh sách ID tiện ích hiện có để đánh dấu trong checkbox
            ViewBag.SelectedTienIchIds = sanBong.TienIchSanBongs.Select(ti => ti.IDTienIch).ToList();
            return View(sanBong);
        }

        // POST: Admin/EditField
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditField(SanBong sanBong, List<int> selectedTienIchIds, HttpPostedFileBase AnhSan)
        {
            // Lấy sân bóng hiện tại từ DB để cập nhật
            var existingSanBong = _unitOfWork.SanBongRepository.AsQueryable()
                                            .Include(s => s.TienIchSanBongs)
                                            .FirstOrDefault(s => s.IDSanBong == sanBong.IDSanBong);
            if (existingSanBong == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra trùng tên sân và địa chỉ (loại trừ chính sân đang sửa)
            if (_unitOfWork.SanBongRepository.AsQueryable().Any(s => s.TenSanBong == sanBong.TenSanBong && s.DiaChi == sanBong.DiaChi && s.IDSanBong != sanBong.IDSanBong))
            {
                ModelState.AddModelError("", "Tên sân và địa chỉ đã tồn tại.");
            }

            // Xử lý upload ảnh
            string fileName = null;
            if (AnhSan != null && AnhSan.ContentLength > 0)
            {
                // Kiểm tra định dạng file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                var fileExtension = Path.GetExtension(AnhSan.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("AnhSan", "Chỉ chấp nhận các định dạng ảnh: JPG, JPEG, PNG, GIF, BMP");
                }
                else if (AnhSan.ContentLength > 5 * 1024 * 1024) // 5MB
                {
                    ModelState.AddModelError("AnhSan", "Kích thước file không được vượt quá 5MB");
                }
                else
                {
                    try
                    {
                        // Tạo tên file unique để tránh trùng lặp
                        fileName = Guid.NewGuid().ToString() + fileExtension;

                        // Đường dẫn thư mục lưu ảnh
                        string uploadPath = Server.MapPath("~/Content/Images/");

                        // Tạo thư mục nếu chưa tồn tại
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        // Lưu file
                        string filePath = Path.Combine(uploadPath, fileName);
                        AnhSan.SaveAs(filePath);

                        // Gán tên file vào model
                        sanBong.AnhSan = fileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("AnhSan", "Có lỗi xảy ra khi upload ảnh: " + ex.Message);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                existingSanBong.TenSanBong = sanBong.TenSanBong;
                existingSanBong.DiaChi = sanBong.DiaChi;
                existingSanBong.MoTaKichThuoc = sanBong.MoTaKichThuoc;
                existingSanBong.GiaThue = sanBong.GiaThue;
                existingSanBong.MoTaSan = sanBong.MoTaSan;
                existingSanBong.TrangThaiSan_ = sanBong.TrangThaiSan_;
                existingSanBong.IDLoaiSan = sanBong.IDLoaiSan;
                existingSanBong.updated_at = DateTime.Now;

                // Cập nhật tiện ích (xóa tất cả và thêm lại)
                existingSanBong.TienIchSanBongs.Clear();
                if (selectedTienIchIds != null && selectedTienIchIds.Any())
                {
                    foreach (var tienIchId in selectedTienIchIds)
                    {
                        var tienIch = _unitOfWork.TienIchSanBongRepository.GetById(tienIchId);
                        if (tienIch != null)
                        {
                            existingSanBong.TienIchSanBongs.Add(tienIch);
                        }
                    }
                }

                _unitOfWork.SanBongRepository.Update(existingSanBong);
                _unitOfWork.Save();
                TempData["SuccessMessage"] = "Sân bóng đã được cập nhật thành công!";
                return RedirectToAction("Fields");
            }

            ViewBag.IDLoaiSan = new SelectList(_unitOfWork.LoaiSanBongRepository.GetAll(), "IDLoaiSan", "LoaiSan", sanBong.IDLoaiSan);
            ViewBag.TienIchList = _unitOfWork.TienIchSanBongRepository.GetAll().ToList();
            ViewBag.SelectedTienIchIds = selectedTienIchIds ?? new List<int>();
            return View(sanBong);
        }

        // POST: Admin/DeleteField/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteField(int id)
        {
            _unitOfWork.SanBongRepository.Delete(id);
            _unitOfWork.Save();
            TempData["SuccessMessage"] = "Sân bóng đã được xóa thành công!";
            return RedirectToAction("Fields");
        }

        #endregion

        #region Quản lý Đơn đặt sân (Bookings)

        // GET: Admin/Bookings
        public ActionResult Bookings(string statusFilter)
        {
            var bookings = _unitOfWork.DonDatSanRepository.AsQueryable()
                                    .Include(b => b.NguoiDung)
                                    .Include(b => b.SanBong)
                                    .Include(b => b.TrangThaiDonDat)
                                    .Include(b => b.PhuongThucThanhToan);

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                bookings = bookings.Where(b => b.TrangThaiDonDat.Tenstatus == statusFilter);
            }

            ViewBag.StatusList = new SelectList(_unitOfWork.TrangThaiDonDatRepository.GetAll(), "Tenstatus", "Tenstatus", statusFilter);
            ViewBag.CurrentStatusFilter = statusFilter;

            return View(bookings.OrderByDescending(b => b.created_at).ToList());
        }

        // GET: Admin/BookingDetails/5
        public ActionResult BookingDetails(int id)
        {
            var booking = _unitOfWork.DonDatSanRepository.AsQueryable()
                                     .Include(b => b.NguoiDung)
                                     .Include(b => b.SanBong)
                                     .Include(b => b.TrangThaiDonDat)
                                     .Include(b => b.PhuongThucThanhToan)
                                     .FirstOrDefault(b => b.IDBooking == id);
            if (booking == null)
            {
                return HttpNotFound();
            }
            ViewBag.StatusOptions = new SelectList(_unitOfWork.TrangThaiDonDatRepository.GetAll(), "IDstatus", "Tenstatus", booking.IDstatus);
            return View(booking);
        }

        // POST: Admin/UpdateBookingStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateBookingStatus(int id, int newStatusId, string adminNotes)
        {
            var booking = _unitOfWork.DonDatSanRepository.GetById(id);
            if (booking == null)
            {
                return HttpNotFound();
            }

            booking.IDstatus = newStatusId;
            booking.admin_notes = adminNotes;
            booking.updated_at = DateTime.Now;

            _unitOfWork.DonDatSanRepository.Update(booking);
            _unitOfWork.Save();
            TempData["SuccessMessage"] = "Trạng thái đơn đặt sân đã được cập nhật.";
            return RedirectToAction("BookingDetails", new { id = id });
        }

        #endregion

        #region Quản lý Đánh giá (Reviews)

        // GET: Admin/Reviews
        public ActionResult Reviews()
        {
            var reviews = _unitOfWork.ReviewsRepository.AsQueryable()
                                   .Include(r => r.NguoiDung)
                                   .Include(r => r.SanBong)
                                   .OrderByDescending(r => r.created_at)
                                   .ToList();
            return View(reviews);
        }

        // POST: Admin/ToggleReviewApproval/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleReviewApproval(int id)
        {
            var review = _unitOfWork.ReviewsRepository.GetById(id);
            if (review == null)
            {
                return HttpNotFound();
            }
            review.is_approved = !review.is_approved;
            review.updated_at = DateTime.Now;
            _unitOfWork.ReviewsRepository.Update(review);
            _unitOfWork.Save();
            TempData["SuccessMessage"] = "Trạng thái duyệt đánh giá đã được thay đổi.";
            return RedirectToAction("Reviews");
        }

        // POST: Admin/DeleteReview/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteReview(int id)
        {
            _unitOfWork.ReviewsRepository.Delete(id);
            _unitOfWork.Save();
            TempData["SuccessMessage"] = "Đánh giá đã được xóa.";
            return RedirectToAction("Reviews");
        }

        #endregion

        #region Quản lý Báo cáo sự cố (SuCo)

        // GET: Admin/Incidents
        public ActionResult Incidents()
        {
            var incidents = _unitOfWork.SuCoRepository.AsQueryable()
                                     .Include(s => s.NguoiDung) // Người báo cáo
                                     .Include(s => s.SanBong) // Sân liên quan
                                     .OrderByDescending(s => s.reported_at)
                                     .ToList();
            return View(incidents);
        }

        // GET: Admin/IncidentDetails/5
        public ActionResult IncidentDetails(int id)
        {
            var incident = _unitOfWork.SuCoRepository.AsQueryable()
                                    .Include(s => s.NguoiDung)
                                    .Include(s => s.SanBong)
                                    .Include(s => s.DonDatSan)
                                    .Include(s => s.NguoiDung1) // IDAdmin
                                    .FirstOrDefault(s => s.IDSuCo == id);
            if (incident == null)
            {
                return HttpNotFound();
            }
            ViewBag.AdminList = new SelectList(_unitOfWork.NguoiDungRepository.AsQueryable().Where(u => u.Role.Role_name == "Admin" || u.Role.Role_name == "Employee"), "IDuser", "fullname", incident.IDAdmin);
            return View(incident);
        }

        // POST: Admin/UpdateIncidentStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateIncidentStatus(int id, string trangThai, string resolutionNotes, int? IDAdmin)
        {
            var incident = _unitOfWork.SuCoRepository.GetById(id);
            if (incident == null)
            {
                return HttpNotFound();
            }

            incident.TrangThai = trangThai;
            incident.resolution_notes = resolutionNotes;
            incident.IDAdmin = IDAdmin;
            if (trangThai == "Đã giải quyết" && incident.resolved_at == null)
            {
                incident.resolved_at = DateTime.Now;
            }
            else if (trangThai != "Đã giải quyết")
            {
                incident.resolved_at = null; // Nếu chuyển trạng thái khác "Đã giải quyết" thì xóa thời gian giải quyết
            }

            _unitOfWork.SuCoRepository.Update(incident);
            _unitOfWork.Save();
            TempData["SuccessMessage"] = "Báo cáo sự cố đã được cập nhật.";
            return RedirectToAction("IncidentDetails", new { id = id });
        }

        #endregion

        #region Quản lý Phân công nhân viên (PhanCongNhanVien)

        // GET: Admin/StaffAssignments
        public ActionResult StaffAssignments()
        {
            var assignments = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
                                        .Include(pc => pc.NguoiDung) // Nhân viên được phân công
                                        .Include(pc => pc.SanBong) // Sân được phân công
                                        .Include(pc => pc.NguoiDung1) // Admin phân công
                                        .OrderByDescending(pc => pc.NgayPhanCong)
                                        .ToList();
            return View(assignments);
        }

        // GET: Admin/CreateAssignment
        [HttpGet]
        public ActionResult CreateAssignment()
        {
            ViewBag.IDuser = new SelectList(_unitOfWork.NguoiDungRepository.AsQueryable().Where(u => u.Role.Role_name == "Employee"), "IDuser", "fullname");
            ViewBag.IDSanBong = new SelectList(_unitOfWork.SanBongRepository.GetAll(), "IDSanBong", "TenSanBong");
            return View();
        }

        // POST: Admin/CreateAssignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAssignment(PhanCongNhanVien assignment)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xung đột lịch: Nhân viên đã được phân công sân khác trong cùng ngày chưa?
                var existingAssignment = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
                                                    .Include(pc => pc.SanBong)
                                                    .Where(pc => pc.IDuser == assignment.IDuser 
                                                              && pc.NgayPhanCong.Date == assignment.NgayPhanCong.Date
                                                              && pc.IDSanBong != assignment.IDSanBong)
                                                    .FirstOrDefault();

                if (existingAssignment != null)
                {
                    TempData["WarningMessage"] = $"Cảnh báo: Nhân viên này đã được phân công quản lý sân '{existingAssignment.SanBong.TenSanBong}' trong cùng ngày ({assignment.NgayPhanCong:dd/MM/yyyy}). Vui lòng kiểm tra lại.";
                    ViewBag.IDuser = new SelectList(_unitOfWork.NguoiDungRepository.AsQueryable().Where(u => u.Role.Role_name == "Employee"), "IDuser", "fullname", assignment.IDuser);
                    ViewBag.IDSanBong = new SelectList(_unitOfWork.SanBongRepository.GetAll(), "IDSanBong", "TenSanBong", assignment.IDSanBong);
                    return View(assignment);
                }

                assignment.created_at = DateTime.Now;
                assignment.updated_at = DateTime.Now;
                assignment.IDAdmin = (int)Session["UserID"]; // Admin hiện tại là người phân công

                _unitOfWork.PhanCongNhanVienRepository.Insert(assignment);
                _unitOfWork.Save();
                TempData["SuccessMessage"] = "Phân công nhân viên đã được tạo thành công!";
                return RedirectToAction("StaffAssignments");
            }
            ViewBag.IDuser = new SelectList(_unitOfWork.NguoiDungRepository.AsQueryable().Where(u => u.Role.Role_name == "Employee"), "IDuser", "fullname", assignment.IDuser);
            ViewBag.IDSanBong = new SelectList(_unitOfWork.SanBongRepository.GetAll(), "IDSanBong", "TenSanBong", assignment.IDSanBong);
            return View(assignment);
        }

        // GET: Admin/EditAssignment/5
        [HttpGet]
        public ActionResult EditAssignment(int id)
        {
            var assignment = _unitOfWork.PhanCongNhanVienRepository.GetById(id);
            if (assignment == null)
            {
                return HttpNotFound();
            }
            ViewBag.IDuser = new SelectList(_unitOfWork.NguoiDungRepository.AsQueryable().Where(u => u.Role.Role_name == "Employee"), "IDuser", "fullname", assignment.IDuser);
            ViewBag.IDSanBong = new SelectList(_unitOfWork.SanBongRepository.GetAll(), "IDSanBong", "TenSanBong", assignment.IDSanBong);
            return View(assignment);
        }

        // POST: Admin/EditAssignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAssignment(PhanCongNhanVien assignment)
        {
            if (ModelState.IsValid)
            {
                var existingAssignment = _unitOfWork.PhanCongNhanVienRepository.GetById(assignment.IDPhanCong);
                if (existingAssignment == null)
                {
                    return HttpNotFound();
                }

                existingAssignment.NgayPhanCong = assignment.NgayPhanCong;
                existingAssignment.ChiTiet = assignment.ChiTiet;
                existingAssignment.IDuser = assignment.IDuser;
                existingAssignment.IDSanBong = assignment.IDSanBong;
                existingAssignment.updated_at = DateTime.Now;
                existingAssignment.IDAdmin = (int)Session["UserID"]; // Cập nhật lại Admin phân công

                _unitOfWork.PhanCongNhanVienRepository.Update(existingAssignment);
                _unitOfWork.Save();
                TempData["SuccessMessage"] = "Phân công nhân viên đã được cập nhật thành công!";
                return RedirectToAction("StaffAssignments");
            }
            ViewBag.IDuser = new SelectList(_unitOfWork.NguoiDungRepository.AsQueryable().Where(u => u.Role.Role_name == "Employee"), "IDuser", "fullname", assignment.IDuser);
            ViewBag.IDSanBong = new SelectList(_unitOfWork.SanBongRepository.GetAll(), "IDSanBong", "TenSanBong", assignment.IDSanBong);
            return View(assignment);
        }

        // POST: Admin/DeleteAssignment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAssignment(int id)
        {
            _unitOfWork.PhanCongNhanVienRepository.Delete(id);
            _unitOfWork.Save();
            TempData["SuccessMessage"] = "Phân công nhân viên đã được xóa thành công!";
            return RedirectToAction("StaffAssignments");
        }

        #endregion

        #region Quản lý Loại sân (LoaiSanBong)
        // GET: Admin/LoaiSanBongs
        public ActionResult LoaiSanBongs()
        {
            var loaiSanBongs = _unitOfWork.LoaiSanBongRepository.GetAll().ToList();
            return View(loaiSanBongs);
        }
        // GET: Admin/CreateLoaiSanBong
        [HttpGet]
        public ActionResult CreateLoaiSanBong()
        {
            return View();
        }
        // POST: Admin/CreateLoaiSanBong
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateLoaiSanBong(LoaiSanBong loaiSanBong)
        {
            if (ModelState.IsValid)
            {
                if (_unitOfWork.LoaiSanBongRepository.AsQueryable().Any(l => l.LoaiSan == loaiSanBong.LoaiSan))
                {
                    ModelState.AddModelError("LoaiSan", "Loại sân này đã tồn tại.");
                }
                if (ModelState.IsValid)
                {
                    _unitOfWork.LoaiSanBongRepository.Insert(loaiSanBong);
                    _unitOfWork.Save();
                    TempData["SuccessMessage"] = "Loại sân đã được tạo thành công!";
                    return RedirectToAction("LoaiSanBongs");
                }
            }
            return View(loaiSanBong);
        }
        // GET: Admin/EditLoaiSanBong/5
        [HttpGet]
        public ActionResult EditLoaiSanBong(int id)
        {
            var loaiSanBong = _unitOfWork.LoaiSanBongRepository.GetById(id);
            if (loaiSanBong == null)
            {
                return HttpNotFound();
            }
            return View(loaiSanBong);
        }
        // POST: Admin/EditLoaiSanBong
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditLoaiSanBong(LoaiSanBong loaiSanBong)
        {
            if (ModelState.IsValid)
            {
                if (_unitOfWork.LoaiSanBongRepository.AsQueryable().Any(l => l.LoaiSan == loaiSanBong.LoaiSan && l.IDLoaiSan != loaiSanBong.IDLoaiSan))
                {
                    ModelState.AddModelError("LoaiSan", "Loại sân này đã tồn tại.");
                }
                if (ModelState.IsValid)
                {
                    _unitOfWork.LoaiSanBongRepository.Update(loaiSanBong);
                    _unitOfWork.Save();
                    TempData["SuccessMessage"] = "Loại sân đã được cập nhật thành công!";
                    return RedirectToAction("LoaiSanBongs");
                }
            }
            return View(loaiSanBong);
        }
        // POST: Admin/DeleteLoaiSanBong/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteLoaiSanBong(int id)
        {
            try
            {
                _unitOfWork.LoaiSanBongRepository.Delete(id);
                _unitOfWork.Save();
                TempData["SuccessMessage"] = "Loại sân đã được xóa thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không thể xóa loại sân này vì có sân bóng đang sử dụng. Vui lòng xóa hoặc cập nhật các sân bóng liên quan trước.";
                // Log lỗi chi tiết hơn nếu cần: ex.Message
            }
            return RedirectToAction("LoaiSanBongs");
        }
        #endregion

        #region Quản lý Tiện ích (TienIchSanBong)
        // GET: Admin/TienIchSanBongs
        public ActionResult TienIchSanBongs()
        {
            var tienIchSanBongs = _unitOfWork.TienIchSanBongRepository.GetAll().ToList();
            return View(tienIchSanBongs);
        }
        // GET: Admin/CreateTienIchSanBong
        [HttpGet]
        public ActionResult CreateTienIchSanBong()
        {
            return View();
        }
        // POST: Admin/CreateTienIchSanBong
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTienIchSanBong(TienIchSanBong tienIchSanBong)
        {
            if (ModelState.IsValid)
            {
                if (_unitOfWork.TienIchSanBongRepository.AsQueryable().Any(t => t.TenTienIch == tienIchSanBong.TenTienIch))
                {
                    ModelState.AddModelError("TenTienIch", "Tên tiện ích này đã tồn tại.");
                }
                if (ModelState.IsValid)
                {
                    _unitOfWork.TienIchSanBongRepository.Insert(tienIchSanBong);
                    _unitOfWork.Save();
                    TempData["SuccessMessage"] = "Tiện ích đã được tạo thành công!";
                    return RedirectToAction("TienIchSanBongs");
                }
            }
            return View(tienIchSanBong);
        }
        // GET: Admin/EditTienIchSanBong/5
        [HttpGet]
        public ActionResult EditTienIchSanBong(int id)
        {
            var tienIchSanBong = _unitOfWork.TienIchSanBongRepository.GetById(id);
            if (tienIchSanBong == null)
            {
                return HttpNotFound();
            }
            return View(tienIchSanBong);
        }
        // POST: Admin/EditTienIchSanBong
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTienIchSanBong(TienIchSanBong tienIchSanBong)
        {
            if (ModelState.IsValid)
            {
                if (_unitOfWork.TienIchSanBongRepository.AsQueryable().Any(t => t.TenTienIch == tienIchSanBong.TenTienIch && t.IDTienIch != tienIchSanBong.IDTienIch))
                {
                    ModelState.AddModelError("TenTienIch", "Tên tiện ích này đã tồn tại.");
                }
                if (ModelState.IsValid)
                {
                    _unitOfWork.TienIchSanBongRepository.Update(tienIchSanBong);
                    _unitOfWork.Save();
                    TempData["SuccessMessage"] = "Tiện ích đã được cập nhật thành công!";
                    return RedirectToAction("TienIchSanBongs");
                }
            }
            return View(tienIchSanBong);
        }
        // POST: Admin/DeleteTienIchSanBong/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteTienIchSanBong(int id)
        {
            try
            {
                _unitOfWork.TienIchSanBongRepository.Delete(id);
                _unitOfWork.Save();
                TempData["SuccessMessage"] = "Tiện ích đã được xóa thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không thể xóa tiện ích này vì có sân bóng đang sử dụng. Vui lòng xóa hoặc cập nhật các sân bóng liên quan trước.";
            }
            return RedirectToAction("TienIchSanBongs");
        }
        #endregion


        #region Quản lý Phương thức thanh toán (PhuongThucThanhToan)

        // GET: Admin/PaymentMethods
        public ActionResult PaymentMethods()
        {
            var paymentMethods = _unitOfWork.PhuongThucThanhToanRepository.GetAll().ToList();
            return View(paymentMethods);
        }

        // GET: Admin/CreatePaymentMethod
        [HttpGet]
        public ActionResult CreatePaymentMethod()
        {
            return View();
        }

        // POST: Admin/CreatePaymentMethod
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreatePaymentMethod(PhuongThucThanhToan paymentMethod)
        {
            if (ModelState.IsValid)
            {
                if (_unitOfWork.PhuongThucThanhToanRepository.AsQueryable().Any(p => p.TenPT == paymentMethod.TenPT))
                {
                    ModelState.AddModelError("TenPT", "Tên phương thức thanh toán này đã tồn tại.");
                }
                if (ModelState.IsValid)
                {
                    _unitOfWork.PhuongThucThanhToanRepository.Insert(paymentMethod);
                    _unitOfWork.Save();
                    TempData["SuccessMessage"] = "Phương thức thanh toán đã được tạo thành công!";
                    return RedirectToAction("PaymentMethods");
                }
            }
            return View(paymentMethod);
        }

        // GET: Admin/EditPaymentMethod/5
        [HttpGet]
        public ActionResult EditPaymentMethod(int id)
        {
            var paymentMethod = _unitOfWork.PhuongThucThanhToanRepository.GetById(id);
            if (paymentMethod == null)
            {
                return HttpNotFound();
            }
            return View(paymentMethod);
        }

        // POST: Admin/EditPaymentMethod
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPaymentMethod(PhuongThucThanhToan paymentMethod)
        {
            if (ModelState.IsValid)
            {
                if (_unitOfWork.PhuongThucThanhToanRepository.AsQueryable().Any(p => p.TenPT == paymentMethod.TenPT && p.IDPT != paymentMethod.IDPT))
                {
                    ModelState.AddModelError("TenPT", "Tên phương thức thanh toán này đã tồn tại.");
                }
                if (ModelState.IsValid)
                {
                    _unitOfWork.PhuongThucThanhToanRepository.Update(paymentMethod);
                    _unitOfWork.Save();
                    TempData["SuccessMessage"] = "Phương thức thanh toán đã được cập nhật thành công!";
                    return RedirectToAction("PaymentMethods");
                }
            }
            return View(paymentMethod);
        }

        // POST: Admin/DeletePaymentMethod/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePaymentMethod(int id)
        {
            try
            {
                _unitOfWork.PhuongThucThanhToanRepository.Delete(id);
                _unitOfWork.Save();
                TempData["SuccessMessage"] = "Phương thức thanh toán đã được xóa thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không thể xóa phương thức thanh toán này vì có đơn đặt sân đang sử dụng. Vui lòng xóa hoặc cập nhật các đơn đặt sân liên quan trước.";
            }
            return RedirectToAction("PaymentMethods");
        }

        #endregion


        #region Quản lý Trạng thái đơn đặt sân (TrangThaiDonDat)

        // GET: Admin/BookingStatuses
        public ActionResult BookingStatuses()
        {
            var bookingStatuses = _unitOfWork.TrangThaiDonDatRepository.GetAll().ToList();
            return View(bookingStatuses);
        }

        // GET: Admin/CreateBookingStatus
        [HttpGet]
        public ActionResult CreateBookingStatus()
        {
            return View();
        }

        // POST: Admin/CreateBookingStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateBookingStatus(TrangThaiDonDat bookingStatus)
        {
            if (ModelState.IsValid)
            {
                if (_unitOfWork.TrangThaiDonDatRepository.AsQueryable().Any(t => t.Tenstatus == bookingStatus.Tenstatus))
                {
                    ModelState.AddModelError("Tenstatus", "Tên trạng thái này đã tồn tại.");
                }
                if (ModelState.IsValid)
                {
                    _unitOfWork.TrangThaiDonDatRepository.Insert(bookingStatus);
                    _unitOfWork.Save();
                    TempData["SuccessMessage"] = "Trạng thái đã được tạo thành công!";
                    return RedirectToAction("BookingStatuses");
                }
            }
            return View(bookingStatus);
        }

        // GET: Admin/EditBookingStatus/5
        [HttpGet]
        public ActionResult EditBookingStatus(int id)
        {
            var bookingStatus = _unitOfWork.TrangThaiDonDatRepository.GetById(id);
            if (bookingStatus == null)
            {
                return HttpNotFound();
            }
            return View(bookingStatus);
        }

        // POST: Admin/EditBookingStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBookingStatus(TrangThaiDonDat bookingStatus)
        {
            if (ModelState.IsValid)
            {
                if (_unitOfWork.TrangThaiDonDatRepository.AsQueryable().Any(t => t.Tenstatus == bookingStatus.Tenstatus && t.IDstatus != bookingStatus.IDstatus))
                {
                    ModelState.AddModelError("Tenstatus", "Tên trạng thái này đã tồn tại.");
                }
                if (ModelState.IsValid)
                {
                    _unitOfWork.TrangThaiDonDatRepository.Update(bookingStatus);
                    _unitOfWork.Save();
                    TempData["SuccessMessage"] = "Trạng thái đã được cập nhật thành công!";
                    return RedirectToAction("BookingStatuses");
                }
            }
            return View(bookingStatus);
        }

        // POST: Admin/DeleteBookingStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBookingStatus(int id)
        {
            try
            {
                _unitOfWork.TrangThaiDonDatRepository.Delete(id);
                _unitOfWork.Save();
                TempData["SuccessMessage"] = "Trạng thái đã được xóa thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Không thể xóa trạng thái này vì có đơn đặt sân đang sử dụng. Vui lòng xóa hoặc cập nhật các đơn đặt sân liên quan trước.";
            }
            return RedirectToAction("BookingStatuses");
        }

        #endregion
        #region Báo cáo và Thống kê

        // GET: Admin/Reports
        public ActionResult Reports()
        {
            // Trang tổng quan các báo cáo
            return View();
        }

        // GET: Admin/RevenueReport
        public ActionResult RevenueReport(string period = "month")
        {
            try
            {
                // Lấy dữ liệu đơn đặt sân đã hoàn thành và đã thanh toán
                var bookings = _unitOfWork.DonDatSanRepository.AsQueryable()
                    .Include(b => b.TrangThaiDonDat)
                    .Where(b => b.TrangThaiDonDat.Tenstatus == "Hoàn thành" && b.TTThanhToan == true)
                    .ToList();

                var revenueData = new Dictionary<string, decimal>();

                if (period == "day")
                {
                    revenueData = bookings
                        .GroupBy(b => b.NgayBooking.ToString("yyyy-MM-dd"))
                        .OrderBy(g => g.Key)
                        .ToDictionary(g => g.Key, g => g.Sum(b => b.TongTien));
                    ViewBag.ReportTitle = "Doanh thu theo ngày";
                    ViewBag.XAxisLabel = "Ngày";
                }
                else if (period == "month")
                {
                    revenueData = bookings
                        .GroupBy(b => b.NgayBooking.ToString("yyyy-MM"))
                        .OrderBy(g => g.Key)
                        .ToDictionary(g => g.Key, g => g.Sum(b => b.TongTien));
                    ViewBag.ReportTitle = "Doanh thu theo tháng";
                    ViewBag.XAxisLabel = "Tháng";
                }
                else if (period == "year")
                {
                    revenueData = bookings
                        .GroupBy(b => b.NgayBooking.Year.ToString())
                        .OrderBy(g => g.Key)
                        .ToDictionary(g => g.Key, g => g.Sum(b => b.TongTien));
                    ViewBag.ReportTitle = "Doanh thu theo năm";
                    ViewBag.XAxisLabel = "Năm";
                }

                // Tính tổng doanh thu
                ViewBag.TotalRevenue = revenueData.Values.Sum();
                ViewBag.TotalBookings = bookings.Count;

                ViewBag.Labels = Newtonsoft.Json.JsonConvert.SerializeObject(revenueData.Keys);
                ViewBag.Data = Newtonsoft.Json.JsonConvert.SerializeObject(revenueData.Values);
                ViewBag.CurrentPeriod = period;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải báo cáo: " + ex.Message;
                return RedirectToAction("Reports");
            }
        }

        // GET: Admin/BookingStatusReport
        public ActionResult BookingStatusReport()
        {
            try
            {
                var statusCounts = _unitOfWork.DonDatSanRepository.AsQueryable()
                    .Include(b => b.TrangThaiDonDat)
                    .GroupBy(b => b.TrangThaiDonDat.Tenstatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToList();

                ViewBag.TotalBookings = statusCounts.Sum(s => s.Count);
                ViewBag.Labels = Newtonsoft.Json.JsonConvert.SerializeObject(statusCounts.Select(s => s.Status));
                ViewBag.Data = Newtonsoft.Json.JsonConvert.SerializeObject(statusCounts.Select(s => s.Count));
                ViewBag.ReportTitle = "Số lượng đơn đặt sân theo trạng thái";

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải báo cáo: " + ex.Message;
                return RedirectToAction("Reports");
            }
        }

        // GET: Admin/TopFieldsReport
        public ActionResult TopFieldsReport(string metric = "bookings")
        {
            try
            {
                var query = _unitOfWork.DonDatSanRepository.AsQueryable()
                    .Include(b => b.SanBong)
                    .Include(b => b.TrangThaiDonDat)
                    .Where(b => b.TrangThaiDonDat.Tenstatus == "Hoàn thành");

                var topFieldsData = new List<object>();

                if (metric == "bookings")
                {
                    topFieldsData = query
                        .GroupBy(b => b.SanBong.TenSanBong)
                        .Select(g => new { FieldName = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(10)
                        .ToList()
                        .Select(x => new { Label = x.FieldName, Value = (object)x.Count })
                        .ToList<object>();
                    ViewBag.ReportTitle = "Top 10 sân bóng được đặt nhiều nhất";
                    ViewBag.YAxisLabel = "Số lượt đặt";
                }
                else if (metric == "revenue")
                {
                    topFieldsData = query
                        .Where(b => b.TTThanhToan == true)
                        .GroupBy(b => b.SanBong.TenSanBong)
                        .Select(g => new { FieldName = g.Key, TotalRevenue = g.Sum(b => b.TongTien) })
                        .OrderByDescending(x => x.TotalRevenue)
                        .Take(10)
                        .ToList()
                        .Select(x => new { Label = x.FieldName, Value = (object)x.TotalRevenue })
                        .ToList<object>();
                    ViewBag.ReportTitle = "Top 10 sân bóng có doanh thu cao nhất";
                    ViewBag.YAxisLabel = "Tổng doanh thu (VNĐ)";
                }

                ViewBag.Labels = Newtonsoft.Json.JsonConvert.SerializeObject(topFieldsData.Select(x => ((dynamic)x).Label));
                ViewBag.Data = Newtonsoft.Json.JsonConvert.SerializeObject(topFieldsData.Select(x => ((dynamic)x).Value));
                ViewBag.CurrentMetric = metric;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải báo cáo: " + ex.Message;
                return RedirectToAction("Reports");
            }
        }

        // GET: Admin/UserActivityReport
        public ActionResult UserActivityReport()
        {
            try
            {
                // Thống kê số lượng người dùng mới theo tháng
                var allUsers = _unitOfWork.NguoiDungRepository.GetAll().ToList();
                var newUsersByMonth = allUsers
                    .GroupBy(u => new { Year = u.create_at.Year, Month = u.create_at.Month })
                    .Select(g => new { Month = $"{g.Key.Year}-{g.Key.Month:D2}", Count = g.Count() })
                    .OrderBy(g => g.Month)
                    .ToList();

                ViewBag.NewUserLabels = Newtonsoft.Json.JsonConvert.SerializeObject(newUsersByMonth.Select(x => x.Month));
                ViewBag.NewUserData = Newtonsoft.Json.JsonConvert.SerializeObject(newUsersByMonth.Select(x => x.Count));

                // Thống kê số lượt đặt sân trung bình của người dùng
                var userBookingCounts = _unitOfWork.DonDatSanRepository.AsQueryable()
                    .GroupBy(b => b.IDuser)
                    .Select(g => new { UserId = g.Key, BookingCount = g.Count() })
                    .ToList();

                decimal averageBookingsPerUser = 0;
                if (userBookingCounts.Any())
                {
                    averageBookingsPerUser = (decimal)userBookingCounts.Average(x => x.BookingCount);
                }

                ViewBag.AverageBookingsPerUser = averageBookingsPerUser.ToString("F2");
                ViewBag.TotalUsers = allUsers.Count;
                ViewBag.ActiveUsers = userBookingCounts.Count;
                ViewBag.ReportTitle = "Báo cáo hoạt động người dùng";

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải báo cáo: " + ex.Message;
                return RedirectToAction("Reports");
            }
        }

        #endregion



        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
