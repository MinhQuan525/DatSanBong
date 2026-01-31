// Controllers/HomeController.cs
using DatSanBong.Models;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity; // Cần cho .Include()
using System.Collections.Generic;
using System;

namespace DatSanBongDa.Controllers
{
    public class HomeController : BaseController
    {
        // GET: Home/Index
        // Tìm kiếm sân bóng (hiển thị danh sách sân)
        public ActionResult Index(string searchName, int? loaiSanId)
        {
            // Bắt đầu với IQueryable để có thể thêm các mệnh đề Where, Include trước khi thực thi truy vấn
            var sanBongs = _unitOfWork.SanBongRepository.AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                sanBongs = sanBongs.Where(s => s.TenSanBong.Contains(searchName) || s.DiaChi.Contains(searchName));
            }
            if (loaiSanId.HasValue)
            {
                sanBongs = sanBongs.Where(s => s.IDLoaiSan == loaiSanId.Value);
            }

            // Tải thông tin loại sân, tiện ích VÀ REVIEWS
            sanBongs = sanBongs.Include(s => s.LoaiSanBong)
                               .Include(s => s.TienIchSanBongs)
                               .Include(s => s.Reviews); 

            // Thực thi truy vấn và tải dữ liệu vào bộ nhớ
            var sanBongList = sanBongs.ToList();

            // Tính toán đánh giá cho từng sân
            foreach (var sanBong in sanBongList)
            {
                if (sanBong.Reviews != null && sanBong.Reviews.Any())
                {
                    sanBong.AverageDanhGia = (decimal)sanBong.Reviews.Average(r => r.rating);
                    sanBong.TongLuotDanhGia = sanBong.Reviews.Count();
                }
                else
                {
                    sanBong.AverageDanhGia = 0;
                    sanBong.TongLuotDanhGia = 0;
                }
            }

            // Lấy danh sách loại sân cho DropDownList
            ViewBag.LoaiSanList = new SelectList(_unitOfWork.LoaiSanBongRepository.GetAll(), "IDLoaiSan", "LoaiSan", loaiSanId);
            ViewBag.CurrentSearchName = searchName;
            ViewBag.CurrentLoaiSanId = loaiSanId;

            return View(sanBongList);
        }

        // GET: Home/FieldDetails/5
        // Xem thông tin chi tiết sân bóng
        public ActionResult FieldDetails(int id)
        {
            // Bắt đầu với IQueryable để có thể thêm các mệnh đề Include
            var sanBong = _unitOfWork.SanBongRepository.AsQueryable() // <--- THAY ĐỔI TẠI ĐÂY
                                     .Include(s => s.LoaiSanBong)
                                     .Include(s => s.TienIchSanBongs) // Tải tiện ích
                                     .Include(s => s.Reviews.Select(r => r.NguoiDung)) // Tải đánh giá và người đánh giá
                                     .FirstOrDefault(s => s.IDSanBong == id); // Thực thi truy vấn và lấy một bản ghi
            if (sanBong == null)
            {
                return HttpNotFound();
            }
            // Tính toán lại đánh giá trung bình và tổng số lượt
            if (sanBong.Reviews != null && sanBong.Reviews.Any())
            {
                sanBong.AverageDanhGia = (decimal)sanBong.Reviews.Average(r => r.rating);
                sanBong.TongLuotDanhGia = sanBong.Reviews.Count();
            }
            else
            {
                sanBong.AverageDanhGia = 0;
                sanBong.TongLuotDanhGia = 0;
            }
            return View(sanBong);
        }

        // GET: Home/CheckAvailability
        // Kiểm tra lịch trống từng sân (Form)
        [HttpGet]
        public ActionResult CheckAvailability(int? sanBongId, DateTime? checkDate)
        {
            ViewBag.SanBongList = new SelectList(_unitOfWork.SanBongRepository.GetAll(), "IDSanBong", "TenSanBong", sanBongId);
            ViewBag.SelectedSanBongId = sanBongId;
            ViewBag.SelectedDate = checkDate?.ToString("yyyy-MM-dd"); // Định dạng cho input type="date"

            // Nếu có sân và ngày được chọn, hiển thị lịch trống
            if (sanBongId.HasValue && checkDate.HasValue)
            {
                var availableSlots = GetAvailableSlots(sanBongId.Value, checkDate.Value);
                ViewBag.AvailableSlots = availableSlots;
            }

            return View();
        }

        // AJAX POST: Home/GetAvailableSlots (hoặc có thể là một action riêng)
        // Kiểm tra lịch trống từng sân (Logic)
        [HttpPost]
        public ActionResult GetAvailableSlotsAjax(int sanBongId, DateTime checkDate)
        {
            var availableSlots = GetAvailableSlots(sanBongId, checkDate);
            return Json(availableSlots, JsonRequestBehavior.AllowGet);
        }

        // Hàm hỗ trợ để lấy các khung giờ trống
        private List<TimeSlot> GetAvailableSlots(int sanBongId, DateTime checkDate)
        {
            var bookedSlots = _unitOfWork.DonDatSanRepository.AsQueryable() // <--- THAY ĐỔI TẠI ĐÂY
                                         .Where(b => b.IDSanBong == sanBongId && b.NgayBooking == checkDate &&
                                                     (b.IDstatus == 1 || b.IDstatus == 2)) // Giả sử 1: Đã duyệt, 2: Chờ duyệt
                                         .Select(b => new { b.start_time, b.end_time })
                                         .ToList();

            var allTimeSlots = new List<TimeSlot>();
            // Giả sử sân hoạt động từ 6:00 đến 23:00, mỗi ca 1 tiếng
            TimeSpan startTime = new TimeSpan(6, 0, 0);
            TimeSpan endTime = new TimeSpan(23, 0, 0);
            TimeSpan duration = new TimeSpan(1, 0, 0); // Mỗi ca 1 tiếng

            for (TimeSpan current = startTime; current < endTime; current = current.Add(duration))
            {
                TimeSpan slotEnd = current.Add(duration);
                bool isBooked = bookedSlots.Any(bs =>
                    (current >= bs.start_time && current < bs.end_time) || // Bắt đầu trong khoảng đã đặt
                    (slotEnd > bs.start_time && slotEnd <= bs.end_time) || // Kết thúc trong khoảng đã đặt
                    (current < bs.start_time && slotEnd > bs.end_time)    // Khoảng đặt nằm trong slot
                );

                // Nếu là ngày hiện tại, không hiển thị các slot đã qua
                if (checkDate.Date == DateTime.Today && slotEnd <= DateTime.Now.TimeOfDay)
                {
                    isBooked = true; // Đánh dấu là đã qua
                }

                allTimeSlots.Add(new TimeSlot
                {
                    StartTime = current,
                    EndTime = slotEnd,
                    IsAvailable = !isBooked
                });
            }
            return allTimeSlots;
        }

        // ViewModel cho TimeSlot (có thể định nghĩa trong Models/ViewModels)
        public class TimeSlot
        {
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public bool IsAvailable { get; set; }
        }
    }
}
