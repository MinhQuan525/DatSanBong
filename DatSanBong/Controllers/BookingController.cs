// Controllers/BookingController.cs
using DatSanBong.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity; // Cần cho .Include()

namespace DatSanBongDa.Controllers
{
    public class BookingController : AuthorizedBaseController // THAY ĐỔI: Kế thừa AuthorizedBaseController
    {
        // GET: Booking/BookField
        // Đặt sân (Form)
        [HttpGet]
        public ActionResult BookField(int? sanBongId, DateTime? selectedDate, TimeSpan? startTime, TimeSpan? endTime)
        {
            // Chỉ hiển thị các sân đang hoạt động
            ViewBag.SanBongList = new SelectList(
                _unitOfWork.SanBongRepository.AsQueryable().Where(s => s.TrangThaiSan_ == true), 
                "IDSanBong", 
                "TenSanBong", 
                sanBongId
            );
            ViewBag.PhuongThucThanhToanList = new SelectList(_unitOfWork.PhuongThucThanhToanRepository.GetAll(), "IDPT", "TenPT");

            // Mặc định trạng thái là "Chờ duyệt"
            var defaultStatus = _unitOfWork.TrangThaiDonDatRepository.GetAll().FirstOrDefault(s => s.Tenstatus == "Chờ duyệt");
            if (defaultStatus == null)
            {
                ModelState.AddModelError("", "Không tìm thấy trạng thái 'Chờ duyệt' trong hệ thống.");
                return View();
            }
            ViewBag.DefaultStatusId = defaultStatus.IDstatus;

            // Nếu có dữ liệu từ CheckAvailability, điền vào model
            var model = new DonDatSan();
            if (sanBongId.HasValue && selectedDate.HasValue && startTime.HasValue && endTime.HasValue)
            {
                model.IDSanBong = sanBongId.Value;
                model.NgayBooking = selectedDate.Value;
                model.start_time = startTime.Value;
                model.end_time = endTime.Value;
            }

            return View(model);
        }

        // POST: Booking/BookField
        // Đặt sân (Logic)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BookField(DonDatSan model, TimeSpan? startTime, TimeSpan? endTime)
        {
            // Gán lại các giá trị từ form
            if (startTime.HasValue && endTime.HasValue)
            {
                model.start_time = startTime.Value;
                model.end_time = endTime.Value;
            }

            model.IDuser = (int)Session["UserID"];
            model.created_at = System.DateTime.Now;
            model.updated_at = System.DateTime.Now;
            model.TTThanhToan = false; // Mặc định là chưa thanh toán

            // Lấy ID của trạng thái "Chờ duyệt"
            var defaultStatus = _unitOfWork.TrangThaiDonDatRepository.GetAll().FirstOrDefault(s => s.Tenstatus == "Chờ duyệt");
            if (defaultStatus == null)
            {
                ModelState.AddModelError("", "Không tìm thấy trạng thái 'Chờ duyệt' trong hệ thống.");
                PopulateBookingDropdowns(model.IDSanBong, model.IDPT);
                return View(model);
            }
            model.IDstatus = defaultStatus.IDstatus;

            // VALIDATION: Kiểm tra sân có tồn tại và đang hoạt động
            var sanBong = _unitOfWork.SanBongRepository.GetById(model.IDSanBong);
            if (sanBong == null)
            {
                ModelState.AddModelError("", "Sân bóng không tồn tại.");
            }
            else if (!sanBong.TrangThaiSan_)
            {
                ModelState.AddModelError("", "Sân bóng hiện không hoạt động. Vui lòng chọn sân khác.");
            }

            // Kiểm tra tính hợp lệ của thời gian
            if (model.end_time <= model.start_time)
            {
                ModelState.AddModelError("", "Thời gian kết thúc phải sau thời gian bắt đầu.");
            }
            else
            {
                // VALIDATION: Kiểm tra thời gian đặt tối thiểu 1 giờ
                var duration = model.end_time - model.start_time;
                if (duration.TotalHours < 1)
                {
                    ModelState.AddModelError("", "Thời gian đặt sân tối thiểu phải là 1 giờ.");
                }
            }

            if (model.NgayBooking < DateTime.Today || (model.NgayBooking == DateTime.Today && model.start_time < DateTime.Now.TimeOfDay))
            {
                ModelState.AddModelError("", "Không thể đặt sân trong quá khứ hoặc thời gian đã qua.");
            }

            // Kiểm tra trùng lịch
            bool isConflict = _unitOfWork.DonDatSanRepository.GetAll()
                                         .Any(b => b.IDSanBong == model.IDSanBong &&
                                                   b.NgayBooking == model.NgayBooking &&
                                                   (b.IDstatus == 1 || b.IDstatus == 2) && // Chỉ kiểm tra các đơn đã duyệt hoặc chờ duyệt
                                                   ((model.start_time >= b.start_time && model.start_time < b.end_time) ||
                                                    (model.end_time > b.start_time && model.end_time <= b.end_time) ||
                                                    (model.start_time < b.start_time && model.end_time > b.end_time)));

            if (isConflict)
            {
                ModelState.AddModelError("", "Sân đã được đặt trong khoảng thời gian này. Vui lòng chọn khung giờ khác.");
            }

            // Tính tổng tiền
            if (sanBong != null)
            {
                decimal durationHours = (decimal)(model.end_time - model.start_time).TotalHours;
                model.TongTien = sanBong.GiaThue * durationHours;
            }

            if (ModelState.IsValid)
            {
                _unitOfWork.DonDatSanRepository.Insert(model);
                _unitOfWork.Save();
                TempData["SuccessMessage"] = "Đơn đặt sân của bạn đã được gửi thành công và đang chờ duyệt!";
                return RedirectToAction("BookingDetails", "User", new { id = model.IDBooking });
            }

            PopulateBookingDropdowns(model.IDSanBong, model.IDPT);
            return View(model);
        }

        // GET: Booking/ChangeBooking/5
        // Đổi lịch đặt sân (Form)
        [HttpGet]
        public ActionResult ChangeBooking(int id)
        {
            int userId = (int)Session["UserID"];
            var booking = _unitOfWork.DonDatSanRepository.AsQueryable()
                                     .Where(b => b.IDBooking == id && b.IDuser == userId)
                                     .Include(b => b.SanBong)
                                     .Include(b => b.TrangThaiDonDat)
                                     .Include(b => b.PhuongThucThanhToan)
                                     .FirstOrDefault();

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt sân hoặc bạn không có quyền truy cập.";
                return RedirectToAction("MyBookings", "User");
            }

            // Chỉ cho phép đổi lịch nếu đơn đang ở trạng thái "Chờ duyệt" hoặc "Đã duyệt" và chưa quá ngày đặt
            var allowedStatuses = new[] { "Chờ duyệt", "Đã duyệt" };
            if (!allowedStatuses.Contains(booking.TrangThaiDonDat.Tenstatus))
            {
                TempData["ErrorMessage"] = "Không thể đổi lịch cho đơn đặt sân ở trạng thái: " + booking.TrangThaiDonDat.Tenstatus;
                return RedirectToAction("BookingDetails", "User", new { id = id });
            }

            // Kiểm tra thời gian: chỉ cho phép đổi lịch trước khi đến giờ đặt ít nhất 2 tiếng
            DateTime bookingDateTime = booking.NgayBooking.Date.Add(booking.start_time);
            if (bookingDateTime <= DateTime.Now.AddHours(2))
            {
                TempData["ErrorMessage"] = "Không thể đổi lịch khi chỉ còn ít hơn 2 tiếng nữa đến giờ đặt sân.";
                return RedirectToAction("BookingDetails", "User", new { id = id });
            }

            // Populate dropdowns
            PopulateBookingDropdowns(booking.IDSanBong, booking.IDPT);

            return View(booking);
        }

        // POST: Booking/ChangeBooking
        // Đổi lịch đặt sân (Logic)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeBooking(DonDatSan model, TimeSpan? startTime, TimeSpan? endTime)
        {
            int userId = (int)Session["UserID"];
            var bookingToUpdate = _unitOfWork.DonDatSanRepository.AsQueryable()
                                            .Where(b => b.IDBooking == model.IDBooking && b.IDuser == userId)
                                            .Include(b => b.TrangThaiDonDat)
                                            .FirstOrDefault();

            if (bookingToUpdate == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt sân hoặc bạn không có quyền truy cập.";
                return RedirectToAction("MyBookings", "User");
            }

            // Gán lại các giá trị từ form
            if (startTime.HasValue && endTime.HasValue)
            {
                model.start_time = startTime.Value;
                model.end_time = endTime.Value;
            }

            // Kiểm tra lại điều kiện đổi lịch
            var allowedStatuses = new[] { "Chờ duyệt", "Đã duyệt" };
            if (!allowedStatuses.Contains(bookingToUpdate.TrangThaiDonDat.Tenstatus))
            {
                ModelState.AddModelError("", "Không thể đổi lịch cho đơn đặt sân ở trạng thái: " + bookingToUpdate.TrangThaiDonDat.Tenstatus);
            }

            // VALIDATION: Kiểm tra sân có tồn tại và đang hoạt động
            var sanBong = _unitOfWork.SanBongRepository.GetById(model.IDSanBong);
            if (sanBong == null)
            {
                ModelState.AddModelError("", "Sân bóng không tồn tại.");
            }
            else if (!sanBong.TrangThaiSan_)
            {
                ModelState.AddModelError("", "Sân bóng hiện không hoạt động. Vui lòng chọn sân khác.");
            }

            // Kiểm tra thời gian đặt mới hợp lệ
            if (model.end_time <= model.start_time)
            {
                ModelState.AddModelError("", "Thời gian kết thúc phải sau thời gian bắt đầu.");
            }
            else
            {
                // VALIDATION: Kiểm tra thời gian đặt tối thiểu 1 giờ
                var duration = model.end_time - model.start_time;
                if (duration.TotalHours < 1)
                {
                    ModelState.AddModelError("", "Thời gian đặt sân tối thiểu phải là 1 giờ.");
                }
            }

            if (model.NgayBooking < DateTime.Today || (model.NgayBooking == DateTime.Today && model.start_time < DateTime.Now.TimeOfDay))
            {
                ModelState.AddModelError("", "Không thể đổi lịch về quá khứ hoặc thời gian đã qua.");
            }

            // Kiểm tra thời gian: phải đổi lịch trước ít nhất 2 tiếng
            DateTime oldBookingDateTime = bookingToUpdate.NgayBooking.Date.Add(bookingToUpdate.start_time);
            if (oldBookingDateTime <= DateTime.Now.AddHours(2))
            {
                ModelState.AddModelError("", "Không thể đổi lịch khi chỉ còn ít hơn 2 tiếng nữa đến giờ đặt sân hiện tại.");
            }

            // Kiểm tra trùng lịch (loại trừ chính đơn đang cập nhật)
            bool isConflict = _unitOfWork.DonDatSanRepository.GetAll()
                                         .Any(b => b.IDSanBong == model.IDSanBong &&
                                                   b.NgayBooking == model.NgayBooking &&
                                                   b.IDBooking != model.IDBooking && // Loại trừ đơn hiện tại
                                                   (b.IDstatus == 1 || b.IDstatus == 2) &&
                                                   ((model.start_time >= b.start_time && model.start_time < b.end_time) ||
                                                    (model.end_time > b.start_time && model.end_time <= b.end_time) ||
                                                    (model.start_time < b.start_time && model.end_time > b.end_time)));

            if (isConflict)
            {
                ModelState.AddModelError("", "Sân đã được đặt trong khoảng thời gian mới này. Vui lòng chọn khung giờ khác.");
            }

            // Tính lại tổng tiền
            if (sanBong != null)
            {
                decimal durationHours = (decimal)(model.end_time - model.start_time).TotalHours;
                model.TongTien = sanBong.GiaThue * durationHours;
            }

            if (ModelState.IsValid)
            {
                // Cập nhật thông tin đơn đặt sân
                bookingToUpdate.IDSanBong = model.IDSanBong;
                bookingToUpdate.NgayBooking = model.NgayBooking;
                bookingToUpdate.start_time = model.start_time;
                bookingToUpdate.end_time = model.end_time;
                bookingToUpdate.TongTien = model.TongTien;
                bookingToUpdate.IDPT = model.IDPT;
                bookingToUpdate.updated_at = DateTime.Now;

                // Đặt lại trạng thái về "Chờ duyệt" nếu đơn đã được duyệt trước đó
                if (bookingToUpdate.TrangThaiDonDat.Tenstatus == "Đã duyệt")
                {
                    var pendingStatus = _unitOfWork.TrangThaiDonDatRepository.GetAll()
                                                  .FirstOrDefault(s => s.Tenstatus == "Chờ duyệt");
                    if (pendingStatus != null)
                    {
                        bookingToUpdate.IDstatus = pendingStatus.IDstatus;
                    }
                }

                // Reset trạng thái thanh toán nếu có thay đổi về tiền
                if (bookingToUpdate.TTThanhToan && model.TongTien != bookingToUpdate.TongTien)
                {
                    bookingToUpdate.TTThanhToan = false;
                }

                _unitOfWork.DonDatSanRepository.Update(bookingToUpdate);
                _unitOfWork.Save();

                TempData["SuccessMessage"] = "Đơn đặt sân đã được đổi lịch thành công! Đơn đang chờ duyệt lại.";
                return RedirectToAction("BookingDetails", "User", new { id = model.IDBooking });
            }

            // Nếu có lỗi, populate lại dropdowns và trả về view
            PopulateBookingDropdowns(model.IDSanBong, model.IDPT);

            // Load lại thông tin booking để hiển thị
            var bookingForView = _unitOfWork.DonDatSanRepository.AsQueryable()
                                           .Where(b => b.IDBooking == model.IDBooking)
                                           .Include(b => b.SanBong)
                                           .Include(b => b.TrangThaiDonDat)
                                           .Include(b => b.PhuongThucThanhToan)
                                           .FirstOrDefault();

            // Copy lại các giá trị từ model để giữ dữ liệu người dùng đã nhập
            if (bookingForView != null)
            {
                bookingForView.IDSanBong = model.IDSanBong;
                bookingForView.NgayBooking = model.NgayBooking;
                bookingForView.start_time = model.start_time;
                bookingForView.end_time = model.end_time;
                bookingForView.IDPT = model.IDPT;
            }

            return View(bookingForView ?? model);
        }

        // POST: Booking/CancelBooking/5
        // Hủy đơn đặt sân
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelBooking(int id, string reason)
        {
            int userId = (int)Session["UserID"];
            var bookingToCancel = _unitOfWork.DonDatSanRepository.AsQueryable()
                                            .Where(b => b.IDBooking == id && b.IDuser == userId)
                                            .Include(b => b.TrangThaiDonDat)
                                            .FirstOrDefault();

            if (bookingToCancel == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt sân hoặc bạn không có quyền truy cập.";
                return RedirectToAction("MyBookings", "User");
            }

            // Chỉ cho phép hủy nếu đơn chưa ở trạng thái "Đã hủy" hoặc "Hoàn thành"
            var notAllowedStatuses = new[] { "Đã hủy", "Hoàn thành" };
            if (notAllowedStatuses.Contains(bookingToCancel.TrangThaiDonDat.Tenstatus))
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn đặt sân ở trạng thái: " + bookingToCancel.TrangThaiDonDat.Tenstatus;
                return RedirectToAction("BookingDetails", "User", new { id = id });
            }

            // Lấy ID của trạng thái "Đã hủy"
            var cancelledStatus = _unitOfWork.TrangThaiDonDatRepository.GetAll()
                                           .FirstOrDefault(s => s.Tenstatus == "Đã hủy");
            if (cancelledStatus == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy trạng thái 'Đã hủy' trong hệ thống.";
                return RedirectToAction("BookingDetails", "User", new { id = id });
            }

            bookingToCancel.IDstatus = cancelledStatus.IDstatus;
            bookingToCancel.LyDoHuy = reason;
            bookingToCancel.updated_at = DateTime.Now;

            _unitOfWork.DonDatSanRepository.Update(bookingToCancel);
            _unitOfWork.Save();

            TempData["SuccessMessage"] = "Đơn đặt sân đã được hủy thành công.";
            return RedirectToAction("BookingDetails", "User", new { id = id });
        }

        // Hàm hỗ trợ để điền dữ liệu cho DropDownList
        private void PopulateBookingDropdowns(int? selectedSanBongId = null, int? selectedPTId = null)
        {
            // Chỉ hiển thị các sân đang hoạt động
            ViewBag.SanBongList = new SelectList(
                _unitOfWork.SanBongRepository.AsQueryable().Where(s => s.TrangThaiSan_ == true),
                "IDSanBong", 
                "TenSanBong", 
                selectedSanBongId
            );
            ViewBag.PhuongThucThanhToanList = new SelectList(_unitOfWork.PhuongThucThanhToanRepository.GetAll(), "IDPT", "TenPT", selectedPTId);
        }
    }
}