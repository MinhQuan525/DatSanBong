using DatSanBongDa.Interfaces;
using DatSanBongDa.UnitOfWork;
using System.Web.Mvc;
using System.Web.Routing; 

namespace DatSanBongDa.Controllers
{
    // BaseController không yêu cầu đăng nhập - dùng cho trang công khai
    public class BaseController : Controller
    {
        // Sử dụng IUnitOfWork để tương tác với database
        protected readonly IUnitOfWork _unitOfWork;

        public BaseController()
        {
            // Khởi tạo UnitOfWork. Trong các dự án lớn hơn, bạn nên dùng Dependency Injection.
            _unitOfWork = new UnitOfWork.UnitOfWork();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _unitOfWork?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // AuthorizedBaseController yêu cầu đăng nhập và kiểm tra phân quyền
    public class AuthorizedBaseController : BaseController
    {
        // Constants cho Role IDs
        protected const int ROLE_CUSTOMER = 1;  // Khách hàng
        protected const int ROLE_EMPLOYEE = 2;  // Nhân viên
        protected const int ROLE_ADMIN = 3;     // Quản trị viên

        // Phương thức này được gọi trước khi một action method được thực thi
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Kiểm tra xem người dùng đã đăng nhập chưa (kiểm tra Session)
            if (Session["UserID"] == null || Session["UserRoleID"] == null)
            {
                // Nếu chưa đăng nhập, chuyển hướng về trang đăng nhập
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Account", action = "Login" }));
                return; // Ngăn không cho action method tiếp tục thực thi
            }

            // Lấy thông tin vai trò của người dùng từ Session
            int userRoleID = (int)Session["UserRoleID"]; // Sử dụng IDrole thay vì Role_name

            // Lấy tên Controller và Action hiện tại
            string currentController = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
            string currentAction = filterContext.ActionDescriptor.ActionName;

            // Logic phân quyền theo ID:
            // Chỉ Admin (ID = 3) mới được truy cập các Controller có tên bắt đầu bằng "Admin"
            if (currentController.StartsWith("Admin") && userRoleID != ROLE_ADMIN)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Account", action = "AccessDenied" }));
                return;
            }

            // Chỉ Nhân viên (ID = 2) và Admin (ID = 3) mới được truy cập các Controller có tên bắt đầu bằng "Employee"
            if (currentController.StartsWith("Employee") && userRoleID != ROLE_EMPLOYEE && userRoleID != ROLE_ADMIN)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new { controller = "Account", action = "AccessDenied" }));
                return;
            }

            // Khách hàng (ID = 1) không được truy cập các trang quản trị
            if (userRoleID == ROLE_CUSTOMER && (currentController.StartsWith("Admin") || currentController.StartsWith("Employee")))
            {
                filterContext.Result = new RedirectToRouteResult(
                   new RouteValueDictionary(new { controller = "Account", action = "AccessDenied" }));
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
