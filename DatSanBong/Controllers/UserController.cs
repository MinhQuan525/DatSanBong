// Controllers/UserController.cs
using DatSanBong.Models;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System;
using System.Collections.Generic;

namespace DatSanBongDa.Controllers
{
    public class UserController : AuthorizedBaseController // THAY ĐỔI: Kế thừa AuthorizedBaseController
    {
        // GET: User/Profile
        // Cập nhật thông tin cá nhân
        [HttpGet]
        public ActionResult Profile()
        {
            int userId = (int)Session["UserID"];
            var user = _unitOfWork.NguoiDungRepository.GetById(userId);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: User/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(NguoiDung model)
        {
            if (ModelState.IsValid)
            {
                int userId = (int)Session["UserID"];
                var userToUpdate = _unitOfWork.NguoiDungRepository.GetById(userId);

                if (userToUpdate != null)
                {
                    userToUpdate.fullname = model.fullname;
                    userToUpdate.sdt = model.sdt;
                    userToUpdate.DiaChi = model.DiaChi;
                    userToUpdate.update_at = System.DateTime.Now;

                    _unitOfWork.NguoiDungRepository.Update(userToUpdate);
                    _unitOfWork.Save();
                    ViewBag.SuccessMessage = "Cập nhật thông tin thành công!";
                    // Cập nhật lại Fullname trong Session nếu có thay đổi
                    Session["Fullname"] = userToUpdate.fullname;
                    return View(userToUpdate);
                }
                ModelState.AddModelError("", "Không tìm thấy người dùng.");
            }
            return View(model);
        }

        // GET: User/MyBookings
        public ActionResult MyBookings()
        {
            int userId = (int)Session["UserID"];
            var bookings = _unitOfWork.DonDatSanRepository.AsQueryable()
                                      .Where(b => b.IDuser == userId)
                                      .Include(b => b.SanBong)
                                      .Include(b => b.TrangThaiDonDat)
                                      .OrderByDescending(b => b.NgayBooking)
                                      .ThenByDescending(b => b.start_time)
                                      .ToList();
            return View(bookings);
        }

        // GET: User/BookingDetails/5
        // Xem chi tiết đơn đã đặt
        public ActionResult BookingDetails(int id)
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
                return HttpNotFound();
            }
            return View(booking);
        }

        // GET: User/RateField/5
        // Đánh giá sân sau khi chơi
        [HttpGet]
        public ActionResult RateField(int bookingId)
        {
            int userId = (int)Session["UserID"];
            var booking = _unitOfWork.DonDatSanRepository.AsQueryable()
                                     .Where(b => b.IDBooking == bookingId && b.IDuser == userId)
                                     .Include(b => b.SanBong)
                                     .FirstOrDefault();

            if (booking == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra xem đơn đặt sân đã kết thúc chưa
            bool canRate = booking.NgayBooking < System.DateTime.Today ||
                           (booking.NgayBooking == System.DateTime.Today && booking.end_time < System.DateTime.Now.TimeOfDay);

            // Kiểm tra xem đã có đánh giá cho booking này chưa
            var existingReview = _unitOfWork.ReviewsRepository.GetAll().FirstOrDefault(r => r.IDBooking == bookingId);

            if (!canRate || existingReview != null)
            {
                ViewBag.ErrorMessage = "Bạn không thể đánh giá đơn đặt sân này (chưa hoàn thành hoặc đã đánh giá).";
                return RedirectToAction("BookingDetails", new { id = bookingId });
            }

            ViewBag.SanBongName = booking.SanBong.TenSanBong;
            ViewBag.BookingId = bookingId;
            ViewBag.SanBongId = booking.IDSanBong;

            return View();
        }

        // POST: User/RateField
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RateField(int bookingId, int rating, string comment)
        {
            int userId = (int)Session["UserID"];
            var booking = _unitOfWork.DonDatSanRepository.GetAll()
                                     .Where(b => b.IDBooking == bookingId && b.IDuser == userId)
                                     .FirstOrDefault();

            if (booking == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra lại điều kiện đánh giá
            bool canRate = booking.NgayBooking < System.DateTime.Today ||
                           (booking.NgayBooking == System.DateTime.Today && booking.end_time < System.DateTime.Now.TimeOfDay);
            var existingReview = _unitOfWork.ReviewsRepository.GetAll().FirstOrDefault(r => r.IDBooking == bookingId);

            if (!canRate || existingReview != null)
            {
                ViewBag.ErrorMessage = "Bạn không thể đánh giá đơn đặt sân này (chưa hoàn thành hoặc đã đánh giá).";
                return RedirectToAction("BookingDetails", new { id = bookingId });
            }

            if (rating < 1 || rating > 5)
            {
                ModelState.AddModelError("", "Số sao đánh giá phải từ 1 đến 5.");
                ViewBag.SanBongName = booking.SanBong.TenSanBong;
                ViewBag.BookingId = bookingId;
                ViewBag.SanBongId = booking.IDSanBong;
                return View();
            }

            var review = new Review
            {
                rating = (byte)rating,
                comment = comment,
                IDSanBong = booking.IDSanBong,
                IDuser = userId,
                IDBooking = bookingId,
                created_at = System.DateTime.Now,
                updated_at = System.DateTime.Now,
                is_approved = true // Mặc định là true, Admin có thể thay đổi
            };

            _unitOfWork.ReviewsRepository.Insert(review);
            _unitOfWork.Save();

            // CẬP NHẬT rating của sân ngay lập tức
            UpdateFieldRating(booking.IDSanBong);

            ViewBag.SuccessMessage = "Đánh giá của bạn đã được gửi thành công!";
            return RedirectToAction("BookingDetails", new { id = bookingId });
        }

        #region Báo cáo sự cố (Report Incident)

        // GET: User/ReportIncident
        [HttpGet]
        public ActionResult ReportIncident(int? bookingId, int? sanBongId)
        {
            int userId = (int)Session["UserID"];
            ViewBag.LoaiSuCoList = new SelectList(new List<string>
            {
                "Sân hư hỏng",
                "Thiết bị hỏng",
                "Vấn đề vệ sinh",
                "Thái độ nhân viên",
                "Sự cố khác"
            });

            // Lấy danh sách các đơn đặt sân của người dùng
            var userBookings = _unitOfWork.DonDatSanRepository.AsQueryable()
                                         .Where(b => b.IDuser == userId)
                                         .OrderByDescending(b => b.NgayBooking)
                                         .ToList()
                                         .Select(b => new
                                         {
                                             IDBooking = b.IDBooking,
                                             Display = "Đơn #" + b.IDBooking + " - " + b.SanBong.TenSanBong + " (" + b.NgayBooking.ToString("dd/MM/yyyy") + " " + b.start_time.ToString(@"hh\:mm") + "-" + b.end_time.ToString(@"hh\:mm") + ")"
                                         })
                                         .ToList();
            ViewBag.BookingList = new SelectList(userBookings, "IDBooking", "Display", bookingId);

            // Lấy danh sách các sân bóng
            var allSanBongs = _unitOfWork.SanBongRepository.GetAll()
                                        .Select(s => new { IDSanBong = s.IDSanBong, TenSanBong = s.TenSanBong })
                                        .ToList();
            ViewBag.SanBongList = new SelectList(allSanBongs, "IDSanBong", "TenSanBong", sanBongId);

            var model = new SuCo
            {
                IDuser = userId,
                reported_at = DateTime.Now,
                TrangThai = "Mới"
            };

            if (bookingId.HasValue)
            {
                model.IDBooking = bookingId.Value;
                var booking = _unitOfWork.DonDatSanRepository.GetById(bookingId.Value);
                if (booking != null)
                {
                    model.IDSanBong = booking.IDSanBong;
                }
            }
            else if (sanBongId.HasValue)
            {
                model.IDSanBong = sanBongId.Value;
            }

            return View(model);
        }

        // POST: User/ReportIncident
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReportIncident(SuCo suCo)
        {
            suCo.IDuser = (int)Session["UserID"];
            suCo.reported_at = DateTime.Now;
            suCo.TrangThai = "Mới";
            suCo.resolution_notes = null;
            suCo.resolved_at = null;
            suCo.IDAdmin = null;

            if (ModelState.IsValid)
            {
                _unitOfWork.SuCoRepository.Insert(suCo);
                _unitOfWork.Save();
                TempData["SuccessMessage"] = "Báo cáo sự cố của bạn đã được gửi thành công. Chúng tôi sẽ xem xét sớm nhất có thể.";
                return RedirectToAction("MyBookings");
            }

            // Nếu có lỗi validation, load lại ViewBag
            ViewBag.LoaiSuCoList = new SelectList(new List<string>
            {
                "Sân hư hỏng",
                "Thiết bị hỏng",
                "Vấn đề vệ sinh",
                "Thái độ nhân viên",
                "Sự cố khác"
            }, suCo.LoaiSuCo);

            var userBookings = _unitOfWork.DonDatSanRepository.AsQueryable()
                                         .Where(b => b.IDuser == suCo.IDuser)
                                         .OrderByDescending(b => b.NgayBooking)
                                         .Select(b => new
                                         {
                                             IDBooking = b.IDBooking,
                                             Display = "Đơn #" + b.IDBooking + " - " + b.SanBong.TenSanBong + " (" + b.NgayBooking.ToString("dd/MM/yyyy") + " " + b.start_time.ToString(@"hh\:mm") + "-" + b.end_time.ToString(@"hh\:mm") + ")"
                                         })
                                         .ToList();
            ViewBag.BookingList = new SelectList(userBookings, "IDBooking", "Display", suCo.IDBooking);

            var allSanBongs = _unitOfWork.SanBongRepository.GetAll()
                                        .Select(s => new { IDSanBong = s.IDSanBong, TenSanBong = s.TenSanBong })
                                        .ToList();
            ViewBag.SanBongList = new SelectList(allSanBongs, "IDSanBong", "TenSanBong", suCo.IDSanBong);

            return View(suCo);
        }

        #endregion

        // Helper method để cập nhật rating sân
        private void UpdateFieldRating(int fieldId)
        {
            var field = _unitOfWork.SanBongRepository.AsQueryable()
                .Include(s => s.Reviews)
                .FirstOrDefault(s => s.IDSanBong == fieldId);

            if (field != null && field.Reviews.Any())
            {
                field.AverageDanhGia = (decimal)field.Reviews.Average(r => r.rating);
                field.TongLuotDanhGia = field.Reviews.Count();
                _unitOfWork.SanBongRepository.Update(field);
                _unitOfWork.Save();
            }
        }
    }
}
