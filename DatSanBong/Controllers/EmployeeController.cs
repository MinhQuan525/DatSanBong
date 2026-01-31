// Controllers/EmployeeController.cs
using DatSanBong.Models;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System;
using System.Collections.Generic;

namespace DatSanBongDa.Controllers
{
    public class EmployeeController : AuthorizedBaseController // THAY ĐỔI: Kế thừa AuthorizedBaseController
    {
        // GET: Employee/Index
        public ActionResult Index()
        {
            int employeeId = (int)Session["UserID"];

            // Lấy danh sách ID các sân mà nhân viên này được phân công quản lý
            var managedFieldIds = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
                                            .Where(pc => pc.IDuser == employeeId)
                                            .Select(pc => pc.IDSanBong)
                                            .Distinct()
                                            .ToList();

            // Đếm đơn đặt sân "Chờ duyệt" chỉ của các sân được phân công
            if (managedFieldIds.Any())
            {
                ViewBag.PendingBookings = _unitOfWork.DonDatSanRepository.AsQueryable()
                                                    .Where(b => managedFieldIds.Contains(b.IDSanBong))
                                                    .Count(b => b.TrangThaiDonDat.Tenstatus == "Chờ duyệt");
                
                ViewBag.NewIncidents = _unitOfWork.SuCoRepository.AsQueryable()
                                                .Where(s => managedFieldIds.Contains(s.IDSanBong.Value))
                                                .Count(s => s.TrangThai == "Mới");

                // Lấy danh sách sân được phân công để hiển thị
                ViewBag.ManagedFields = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
                                                .Where(pc => pc.IDuser == employeeId)
                                                .Include(pc => pc.SanBong)
                                                .Select(pc => pc.SanBong)
                                                .Distinct()
                                                .ToList();
            }
            else
            {
                ViewBag.PendingBookings = 0;
                ViewBag.NewIncidents = 0;
                ViewBag.ManagedFields = new List<SanBong>();
            }

            return View();
        }

        #region Quản lý Đơn đặt sân (Bookings)

        // GET: Employee/Bookings
        public ActionResult Bookings(string statusFilter)
        {
            int employeeId = (int)Session["UserID"];

            // 1. Lấy danh sách ID các sân mà nhân viên này được phân công quản lý
            var managedFieldIds = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
                                            .Where(pc => pc.IDuser == employeeId)
                                            .Select(pc => pc.IDSanBong)
                                            .Distinct() // Đảm bảo không có ID sân trùng lặp
                                            .ToList();

            // Nếu nhân viên không được phân công sân nào, trả về danh sách rỗng
            if (!managedFieldIds.Any())
            {
                ViewBag.StatusList = new SelectList(_unitOfWork.TrangThaiDonDatRepository.GetAll(), "Tenstatus", "Tenstatus", statusFilter);
                ViewBag.CurrentStatusFilter = statusFilter;
                return View(new List<DonDatSan>()); // Trả về danh sách đơn đặt sân rỗng
            }

            // 2. Lọc đơn đặt sân chỉ cho các sân mà nhân viên quản lý
            var bookings = _unitOfWork.DonDatSanRepository.AsQueryable()
                                    .Where(b => managedFieldIds.Contains(b.IDSanBong)) // LỌC THEO SÂN QUẢN LÝ
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

        // GET: Employee/BookingDetails/5
        public ActionResult BookingDetails(int id)
        {
            int employeeId = (int)Session["UserID"];

            // 1. Lấy danh sách ID các sân mà nhân viên này được phân công quản lý
            var managedFieldIds = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
                                            .Where(pc => pc.IDuser == employeeId)
                                            .Select(pc => pc.IDSanBong)
                                            .Distinct()
                                            .ToList();

            // 2. Lấy chi tiết đơn đặt sân, nhưng chỉ nếu nó thuộc về sân mà nhân viên quản lý
            var booking = _unitOfWork.DonDatSanRepository.AsQueryable()
                                     .Where(b => b.IDBooking == id && managedFieldIds.Contains(b.IDSanBong)) // LỌC THEO SÂN QUẢN LÝ
                                     .Include(b => b.NguoiDung)
                                     .Include(b => b.SanBong)
                                     .Include(b => b.TrangThaiDonDat)
                                     .Include(b => b.PhuongThucThanhToan)
                                     .FirstOrDefault();

            if (booking == null)
            {
                // Nếu không tìm thấy đơn hoặc đơn không thuộc sân quản lý của nhân viên
                return HttpNotFound(); // Hoặc chuyển hướng đến trang AccessDenied
            }
            ViewBag.StatusOptions = new SelectList(_unitOfWork.TrangThaiDonDatRepository.GetAll(), "IDstatus", "Tenstatus", booking.IDstatus);
            return View(booking);
        }

        // POST: Employee/UpdateBookingStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateBookingStatus(int id, int newStatusId, string adminNotes)
        {
            int employeeId = (int)Session["UserID"];

            // 1. Lấy danh sách ID các sân mà nhân viên này được phân công quản lý
            var managedFieldIds = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
                                            .Where(pc => pc.IDuser == employeeId)
                                            .Select(pc => pc.IDSanBong)
                                            .Distinct()
                                            .ToList();

            // 2. Lấy đơn đặt sân để cập nhật, nhưng chỉ nếu nó thuộc về sân mà nhân viên quản lý
            var booking = _unitOfWork.DonDatSanRepository.AsQueryable()
                                     .Where(b => b.IDBooking == id && managedFieldIds.Contains(b.IDSanBong)) // LỌC THEO SÂN QUẢN LÝ
                                     .FirstOrDefault();

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền cập nhật đơn đặt sân này.";
                return RedirectToAction("Bookings"); // Hoặc về trang chi tiết nếu có
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

        #region Quản lý Báo cáo sự cố (SuCo)

        // GET: Employee/Incidents
        public ActionResult Incidents()
        {
            int employeeId = (int)Session["UserID"];

            // 1. Lấy danh sách ID các sân mà nhân viên này được phân công quản lý
            var managedFieldIds = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
                                            .Where(pc => pc.IDuser == employeeId)
                                            .Select(pc => pc.IDSanBong)
                                            .Distinct()
                                            .ToList();

            // Nếu nhân viên không được phân công sân nào, trả về danh sách rỗng
            if (!managedFieldIds.Any())
            {
                return View(new List<SuCo>()); // Trả về danh sách sự cố rỗng
            }

            // 2. Lọc báo cáo sự cố chỉ cho các sân mà nhân viên quản lý
            var incidents = _unitOfWork.SuCoRepository.AsQueryable()
                                     .Where(s => managedFieldIds.Contains(s.IDSanBong.Value)) // LỌC THEO SÂN QUẢN LÝ (IDSanBong có thể NULL)
                                     .Include(s => s.NguoiDung)
                                     .Include(s => s.SanBong)
                                     .OrderByDescending(s => s.reported_at)
                                     .ToList();
            return View(incidents);
        }

        // GET: Employee/IncidentDetails/5
        public ActionResult IncidentDetails(int id)
        {
            int employeeId = (int)Session["UserID"];

            // 1. Lấy danh sách ID các sân mà nhân viên này được phân công quản lý
            var managedFieldIds = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
                                            .Where(pc => pc.IDuser == employeeId)
                                            .Select(pc => pc.IDSanBong)
                                            .Distinct()
                                            .ToList();

            // 2. Lấy chi tiết sự cố, nhưng chỉ nếu nó thuộc về sân mà nhân viên quản lý
            var incident = _unitOfWork.SuCoRepository.AsQueryable()
                                    .Where(s => s.IDSuCo == id && managedFieldIds.Contains(s.IDSanBong.Value)) // LỌC THEO SÂN QUẢN LÝ
                                    .Include(s => s.NguoiDung)
                                    .Include(s => s.SanBong)
                                    .Include(s => s.DonDatSan)
                                    .Include(s => s.NguoiDung1) // IDAdmin
                                    .FirstOrDefault();

            if (incident == null)
            {
                // Nếu không tìm thấy sự cố hoặc sự cố không thuộc sân quản lý của nhân viên
                return HttpNotFound(); // Hoặc chuyển hướng đến trang AccessDenied
            }
            // Nhân viên chỉ có thể tự phân công cho mình hoặc không phân công
            ViewBag.AdminList = new SelectList(_unitOfWork.NguoiDungRepository.AsQueryable().Where(u => u.IDuser == employeeId), "IDuser", "fullname", incident.IDAdmin);
            return View(incident);
        }

        // POST: Employee/UpdateIncidentStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateIncidentStatus(int id, string trangThai, string resolutionNotes, int? IDAdmin)
        {
            int employeeId = (int)Session["UserID"];

            // 1. Lấy danh sách ID các sân mà nhân viên này được phân công quản lý
            var managedFieldIds = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
                                            .Where(pc => pc.IDuser == employeeId)
                                            .Select(pc => pc.IDSanBong)
                                            .Distinct()
                                            .ToList();

            // 2. Lấy sự cố để cập nhật, nhưng chỉ nếu nó thuộc về sân mà nhân viên quản lý
            var incident = _unitOfWork.SuCoRepository.AsQueryable()
                                     .Where(s => s.IDSuCo == id && managedFieldIds.Contains(s.IDSanBong.Value)) // LỌC THEO SÂN QUẢN LÝ
                                     .FirstOrDefault();

            if (incident == null)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền cập nhật báo cáo sự cố này.";
                return RedirectToAction("Incidents");
            }

            // Kiểm tra quyền cập nhật:
            // - Nếu chưa có ai nhận: Cho phép nhận việc
            // - Nếu đã có người nhận: Chỉ người đó mới được cập nhật
            if (incident.IDAdmin != null && incident.IDAdmin != employeeId)
            {
                var adminName = _unitOfWork.NguoiDungRepository.GetById(incident.IDAdmin.Value)?.fullname ?? "Nhân viên khác";
                TempData["ErrorMessage"] = $"Sự cố này đang được xử lý bởi {adminName}.";
                return RedirectToAction("IncidentDetails", new { id = id });
            }

            // Cập nhật thông tin sự cố
            incident.TrangThai = trangThai;
            incident.resolution_notes = resolutionNotes;
            
            // Gán người xử lý (nếu chưa có hoặc là chính nhân viên này)
            if (incident.IDAdmin == null || incident.IDAdmin == employeeId)
            {
                incident.IDAdmin = employeeId; // Nhận việc hoặc tiếp tục xử lý
            }

            // Cập nhật thời gian giải quyết
            if (trangThai == "Đã giải quyết" && incident.resolved_at == null)
            {
                incident.resolved_at = DateTime.Now;
            }
            else if (trangThai != "Đã giải quyết")
            {
                incident.resolved_at = null;
            }

            _unitOfWork.SuCoRepository.Update(incident);
            _unitOfWork.Save();
            TempData["SuccessMessage"] = "Báo cáo sự cố đã được cập nhật.";
            return RedirectToAction("IncidentDetails", new { id = id });
        }

        #endregion

        #region Xem lịch phân công (StaffAssignments)
        // GET: Employee/MyAssignments
        public ActionResult MyAssignments()
        {
            int employeeId = (int)Session["UserID"];
            var assignments = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
                                        .Include(pc => pc.NguoiDung) // Nhân viên được phân công
                                        .Include(pc => pc.SanBong) // Sân được phân công
                                        .Include(pc => pc.NguoiDung1) // Admin phân công
                                        .Where(pc => pc.IDuser == employeeId) // Chỉ xem phân công của mình
                                        .OrderByDescending(pc => pc.NgayPhanCong)
                                        .ToList();
            return View(assignments);
        }
        #endregion
        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
