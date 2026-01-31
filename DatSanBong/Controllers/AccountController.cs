// Controllers/AccountController.cs
using DatSanBong.Models;
using DatSanBongDa.UnitOfWork;
using System.Linq;
using System.Web.Mvc;
using BCrypt.Net;
using DatSanBongDa.Interfaces;
using System;

namespace DatSanBongDa.Controllers
{
    // AccountController kế thừa BaseController vì các trang đăng nhập/đăng ký là công khai
    public class AccountController : BaseController // THAY ĐỔI: Kế thừa BaseController
    {
        // GET: Account/Register
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string username, string email, string password, string confirmPassword)
        {
            // Trim whitespace để tránh lỗi
            username = username?.Trim();
            email = email?.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || password != confirmPassword)
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin và đảm bảo mật khẩu khớp nhau.");
                return View();
            }

            // DEBUG: Log giá trị đang check
            System.Diagnostics.Debug.WriteLine("=== REGISTER ATTEMPT ===");
            System.Diagnostics.Debug.WriteLine($"Username: '{username}'");
            System.Diagnostics.Debug.WriteLine($"Email: '{email}'");

            // Kiểm tra xem username đã tồn tại chưa (case-insensitive)
            var existingUsername = _unitOfWork.NguoiDungRepository.GetAll()
                .FirstOrDefault(u => u.username.ToLower() == username.ToLower());
            
            if (existingUsername != null)
            {
                System.Diagnostics.Debug.WriteLine($"USERNAME CONFLICT: Found existing user with ID {existingUsername.IDuser}");
                System.Diagnostics.Debug.WriteLine($"  Existing: '{existingUsername.username}'");
                System.Diagnostics.Debug.WriteLine($"  Attempted: '{username}'");
                
                ModelState.AddModelError("username", $"Tên đăng nhập '{username}' đã được sử dụng.");
                return View();
            }

            // Kiểm tra xem email đã tồn tại chưa (case-insensitive)
            var existingEmail = _unitOfWork.NguoiDungRepository.GetAll()
                .FirstOrDefault(u => u.email.ToLower() == email.ToLower());
            
            if (existingEmail != null)
            {
                System.Diagnostics.Debug.WriteLine($"EMAIL CONFLICT: Found existing user with ID {existingEmail.IDuser}");
                System.Diagnostics.Debug.WriteLine($"  Existing: '{existingEmail.email}'");
                System.Diagnostics.Debug.WriteLine($"  Attempted: '{email}'");
                
                ModelState.AddModelError("email", $"Email '{email}' đã được sử dụng.");
                return View();
            }

            System.Diagnostics.Debug.WriteLine("✓ Username and email are available");

            try
            {
                // Tạo người dùng mới
                var newUser = new NguoiDung
                {
                    username = username,
                    email = email,
                    pass_hash = BCrypt.Net.BCrypt.HashPassword(password),
                    fullname = username,
                    trangthaiTK = true,
                    IDrole = 1,
                    create_at = DateTime.Now,
                    update_at = DateTime.Now,
                    last_log = null,
                    sdt = null,
                    DiaChi = null
                };

                System.Diagnostics.Debug.WriteLine("Attempting to insert user...");
                _unitOfWork.NguoiDungRepository.Insert(newUser);
                
                System.Diagnostics.Debug.WriteLine("Attempting to save changes...");
                _unitOfWork.Save();
                
                System.Diagnostics.Debug.WriteLine($"✓ User created successfully with ID: {newUser.IDuser}");

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                // Log chi tiết lỗi
                System.Diagnostics.Debug.WriteLine("=== REGISTER ERROR ===");
                System.Diagnostics.Debug.WriteLine($"Error Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    if (ex.InnerException.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Inner Inner Exception: {ex.InnerException.InnerException.Message}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Hiển thị lỗi thân thiện hơn cho user
                string errorMessage = "Đăng ký thất bại. ";
                
                if (ex.Message.Contains("UNIQUE KEY constraint"))
                {
                    if (ex.Message.Contains("username"))
                    {
                        errorMessage = $"Tên đăng nhập '{username}' đã tồn tại trong hệ thống.";
                    }
                    else if (ex.Message.Contains("email"))
                    {
                        errorMessage = $"Email '{email}' đã tồn tại trong hệ thống.";
                    }
                    else
                    {
                        errorMessage += "Tên đăng nhập hoặc email đã tồn tại. Vui lòng thử lại.";
                    }
                }
                else if (ex.Message.Contains("FOREIGN KEY constraint"))
                {
                    errorMessage += "Lỗi hệ thống (vai trò không tồn tại). Vui lòng liên hệ quản trị viên.";
                }
                else if (ex.Message.Contains("sdt"))
                {
                    errorMessage = "Lỗi số điện thoại. Vui lòng liên hệ quản trị viên để kiểm tra cấu hình database.";
                }
                else
                {
                    errorMessage += $"Chi tiết: {ex.Message}";
                }
                
                ModelState.AddModelError("", errorMessage);
                return View();
            }
        }

        // GET: Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            // Nếu người dùng đã đăng nhập, chuyển hướng về trang chủ
            if (Session["UserID"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string usernameOrEmail, string password)
        {
            if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập tên đăng nhập/email và mật khẩu.");
                return View();
            }

            // Tìm người dùng theo username hoặc email
            var user = _unitOfWork.NguoiDungRepository.GetAll()
                                  .FirstOrDefault(u => u.username == usernameOrEmail || u.email == usernameOrEmail);

            if (user != null)
            {
                // Kiểm tra mật khẩu đã hash
                if (BCrypt.Net.BCrypt.Verify(password, user.pass_hash))
                {
                    // Đăng nhập thành công
                    Session["UserID"] = user.IDuser;
                    Session["Username"] = user.username;
                    Session["Fullname"] = user.fullname;

                    // Lưu CẢHAI Role_name VÀ IDrole vào Session
                    var role = _unitOfWork.RolesRepository.GetById(user.IDrole);
                    Session["UserRole"] = role?.Role_name;      // Giữ lại cho backward compatibility
                    Session["UserRoleID"] = user.IDrole;        // THÊM: Sử dụng IDrole để phân quyền chính xác

                    // Cập nhật last_log
                    user.last_log = System.DateTime.Now;
                    _unitOfWork.NguoiDungRepository.Update(user);
                    _unitOfWork.Save();

                    // Chuyển hướng tùy theo IDrole
                    // 1 = Customer, 2 = Employee, 3 = Admin
                    if (user.IDrole == 3) // Admin
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else if (user.IDrole == 2) // Employee
                    {
                        return RedirectToAction("Index", "Employee");
                    }
                    else // Customer (IDrole = 1) hoặc các vai trò khác
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
            }

            ModelState.AddModelError("", "Tên đăng nhập/email hoặc mật khẩu không đúng.");
            return View();
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login", "Account");
        }

        // GET: Account/ForgotPassword
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email)
        {
            if (!string.IsNullOrEmpty(email))
            {
                var user = _unitOfWork.NguoiDungRepository.GetAll().FirstOrDefault(u => u.email == email);
                if (user != null)
                {
                    // Trong thực tế: Tạo token, lưu DB, gửi email
                    ViewBag.Message = "Nếu email của bạn tồn tại trong hệ thống, một liên kết đặt lại mật khẩu đã được gửi đến email của bạn.";
                }
                else
                {
                    ViewBag.Message = "Nếu email của bạn tồn tại trong hệ thống, một liên kết đặt lại mật khẩu đã được gửi đến email của bạn.";
                }
            }
            else
            {
                ModelState.AddModelError("", "Vui lòng nhập địa chỉ email.");
            }
            return View();
        }

        // GET: Account/ResetPassword
        [HttpGet]
        public ActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }
            ViewBag.Token = token;
            return View();
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu mới không khớp hoặc không hợp lệ.");
                ViewBag.Token = token;
                return View();
            }

            // Trong thực tế: Tìm user theo token
            var user = _unitOfWork.NguoiDungRepository.GetAll().FirstOrDefault();

            if (user != null)
            {
                user.pass_hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _unitOfWork.NguoiDungRepository.Update(user);
                _unitOfWork.Save();
                ViewBag.SuccessMessage = "Mật khẩu của bạn đã được đặt lại thành công. Vui lòng đăng nhập.";
                return View("Login");
            }

            ModelState.AddModelError("", "Yêu cầu đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
            ViewBag.Token = token;
            return View();
        }

        // GET: Account/ChangePassword (Yêu cầu đăng nhập)
        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin và đảm bảo mật khẩu mới khớp nhau.");
                return View();
            }

            int userId = (int)Session["UserID"];
            var user = _unitOfWork.NguoiDungRepository.GetById(userId);

            if (user != null && BCrypt.Net.BCrypt.Verify(currentPassword, user.pass_hash))
            {
                user.pass_hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _unitOfWork.NguoiDungRepository.Update(user);
                _unitOfWork.Save();
                ViewBag.SuccessMessage = "Mật khẩu của bạn đã được thay đổi thành công.";
                return View();
            }

            ModelState.AddModelError("", "Mật khẩu hiện tại không đúng.");
            return View();
        }

        // GET: Account/AccessDenied
        public ActionResult AccessDenied()
        {
            return View();
        }
    }
}
