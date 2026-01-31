# ?? LOGIC REVIEW - STAFF ASSIGNMENT & EMPLOYEE PERMISSIONS

## ?? Date: 2024
## ?? Scope: Employee Controller & Admin Staff Assignment Logic

---

## ? **LOGIC PHÂN CÔNG NHÂN VIÊN - ?ÁNH GIÁ**

### **1. T?NG QUAN NGHI?P V?:**

**Quy trình:**
```
Admin ? Phân công nhân viên cho sân ? Nhân viên qu?n lý sân ? 
X? lý ??n ??t sân & s? c? c?a sân ???c phân công
```

**B?ng liên quan:**
- `PhanCongNhanVien` (Staff Assignments)
  - `IDPhanCong` (PK)
  - `IDuser` (FK ? NguoiDung) - Nhân viên ???c phân công
  - `IDSanBong` (FK ? SanBong) - Sân ???c phân công
  - `IDAdmin` (FK ? NguoiDung) - Admin phân công
  - `NgayPhanCong` - Ngày phân công
  - `ChiTiet` - Chi ti?t nhi?m v?

---

## ? **?I?M M?NH - LOGIC HI?N T?I:**

### **1. Employee Controller - Data Filtering:**

#### **? Bookings (??n ??t sân):**
```csharp
// ?ÚNG: L?c ??n ??t sân theo sân ???c phân công
var managedFieldIds = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
    .Where(pc => pc.IDuser == employeeId)
    .Select(pc => pc.IDSanBong)
    .Distinct()
    .ToList();

var bookings = _unitOfWork.DonDatSanRepository.AsQueryable()
    .Where(b => managedFieldIds.Contains(b.IDSanBong)) // ? L?c theo sân
    .Include(...)
```

**K?t qu?:**
- ? Nhân viên CH? xem ???c ??n ??t sân c?a các sân h? qu?n lý
- ? Không th? xem ??n c?a sân khác
- ? Empty list n?u ch?a ???c phân công sân nào

---

#### **? Incidents (S? c?):**
```csharp
// ?ÚNG: L?c s? c? theo sân ???c phân công
var incidents = _unitOfWork.SuCoRepository.AsQueryable()
    .Where(s => managedFieldIds.Contains(s.IDSanBong.Value)) // ? L?c theo sân
    .Include(...)
```

**K?t qu?:**
- ? Nhân viên CH? xem ???c s? c? c?a các sân h? qu?n lý
- ? Không th? xem s? c? c?a sân khác

---

#### **? BookingDetails & IncidentDetails:**
```csharp
// ?ÚNG: Ki?m tra quy?n truy c?p chi ti?t
var booking = _unitOfWork.DonDatSanRepository.AsQueryable()
    .Where(b => b.IDBooking == id && managedFieldIds.Contains(b.IDSanBong))
    .FirstOrDefault();

if (booking == null) {
    return HttpNotFound(); // ? B?o m?t t?t
}
```

**K?t qu?:**
- ? Nhân viên KHÔNG th? truy c?p tr?c ti?p vào URL c?a ??n/s? c? không thu?c quy?n
- ? Return 404 n?u c? tình truy c?p

---

### **2. Update Permissions:**

#### **? UpdateBookingStatus:**
```csharp
// ?ÚNG: Ki?m tra quy?n tr??c khi c?p nh?t
var booking = _unitOfWork.DonDatSanRepository.AsQueryable()
    .Where(b => b.IDBooking == id && managedFieldIds.Contains(b.IDSanBong))
    .FirstOrDefault();

if (booking == null) {
    TempData["ErrorMessage"] = "B?n không có quy?n c?p nh?t ??n ??t sân này.";
    return RedirectToAction("Bookings");
}
```

---

## ?? **V?N ?? ?Ã PHÁT HI?N & ?Ã S?A:**

### **1. ? Employee Index - Hi?n th? sai s? li?u:**

**V?N ??:**
```csharp
// ? SAI: ??m T?T C? ??n "Ch? duy?t", không l?c theo sân
ViewBag.PendingBookings = _unitOfWork.DonDatSanRepository.AsQueryable()
    .Count(b => b.TrangThaiDonDat.Tenstatus == "Ch? duy?t");
```

**H?u qu?:**
- Nhân viên qu?n lý sân A nhìn th?y s? li?u c?a sân B, C, D...
- Dashboard không chính xác
- Gây nh?m l?n

**? ?Ã S?A:**
```csharp
// ? ?ÚNG: Ch? ??m ??n c?a các sân ???c phân công
var managedFieldIds = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
    .Where(pc => pc.IDuser == employeeId)
    .Select(pc => pc.IDSanBong)
    .Distinct()
    .ToList();

if (managedFieldIds.Any())
{
    ViewBag.PendingBookings = _unitOfWork.DonDatSanRepository.AsQueryable()
        .Where(b => managedFieldIds.Contains(b.IDSanBong)) // ? L?c theo sân
        .Count(b => b.TrangThaiDonDat.Tenstatus == "Ch? duy?t");

    ViewBag.NewIncidents = _unitOfWork.SuCoRepository.AsQueryable()
        .Where(s => managedFieldIds.Contains(s.IDSanBong.Value)) // ? L?c theo sân
        .Count(s => s.TrangThai == "M?i");

    // ? THÊM: Hi?n th? danh sách sân ???c phân công
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
```

**K?t qu?:**
- ? Dashboard hi?n th? ?úng s? li?u c?a sân ???c phân công
- ? Nhân viên bi?t rõ h? ?ang qu?n lý sân nào
- ? Empty state n?u ch?a ???c phân công

---

### **2. ? UpdateIncidentStatus - Logic phân công không rõ ràng:**

**V?N ??:**
```csharp
// ? KHÔNG RÕ RÀNG: T? ??ng chi?m quy?n x? lý
incident.IDAdmin = employeeId; // T? ??ng gán ngay

// Ki?m tra sau:
if (incident.IDAdmin != null && incident.IDAdmin != employeeId) {
    // Không cho phép
}
```

**H?u qu?:**
- Nhân viên A xem s? c? ? T? ??ng "chi?m" quy?n x? lý
- Nhân viên B không th? can thi?p (ngay c? khi nhân viên A ch?a x? lý gì)
- Không linh ho?t khi thay ca

**? ?Ã S?A:**
```csharp
// ? Ki?m tra quy?n TR??C khi c?p nh?t
if (incident.IDAdmin != null && incident.IDAdmin != employeeId)
{
    var adminName = _unitOfWork.NguoiDungRepository.GetById(incident.IDAdmin.Value)?.fullname ?? "Nhân viên khác";
    TempData["ErrorMessage"] = $"S? c? này ?ang ???c x? lý b?i {adminName}.";
    return RedirectToAction("IncidentDetails", new { id = id });
}

// ? Ch? gán khi ch?a có ai nh?n ho?c là chính ng??i này
if (incident.IDAdmin == null || incident.IDAdmin == employeeId)
{
    incident.IDAdmin = employeeId; // Nh?n vi?c ho?c ti?p t?c x? lý
}
```

**K?t qu?:**
- ? Hi?n th? tên nhân viên ?ang x? lý
- ? Không cho phép "c??p" vi?c c?a ng??i khác
- ? Linh ho?t h?n

---

### **3. ? THÊM M?I: Schedule Conflict Validation:**

**V?N ??:**
- Admin có th? phân công 1 nhân viên cho nhi?u sân trong cùng ngày
- Không có c?nh báo xung ??t l?ch

**? ?Ã THÊM (AdminController.CreateAssignment):**
```csharp
// ? Ki?m tra xung ??t l?ch
var existingAssignment = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
    .Include(pc => pc.SanBong)
    .Where(pc => pc.IDuser == assignment.IDuser 
              && pc.NgayPhanCong.Date == assignment.NgayPhanCong.Date
              && pc.IDSanBong != assignment.IDSanBong)
    .FirstOrDefault();

if (existingAssignment != null)
{
    TempData["WarningMessage"] = $"C?nh báo: Nhân viên này ?ã ???c phân công qu?n lý sân '{existingAssignment.SanBong.TenSanBong}' trong cùng ngày ({assignment.NgayPhanCong:dd/MM/yyyy}). Vui lòng ki?m tra l?i.";
    return View(assignment);
}
```

**K?t qu?:**
- ? C?nh báo khi phân công trùng ngày
- ? Admin có th? review tr??c khi xác nh?n
- ? Tránh nh?m l?n

---

## ?? **LU?NG D? LI?U:**

### **Employee Access Flow:**

```
???????????????????????????????????????????????????
? 1. Employee Login                                ?
?    ?                                              ?
? 2. Get employeeId from Session                   ?
?    ?                                              ?
? 3. Query PhanCongNhanVien                        ?
?    WHERE IDuser = employeeId                     ?
?    ?                                              ?
? 4. Extract managedFieldIds (List<int>)          ?
?    ?                                              ?
? 5. Filter Data by managedFieldIds               ?
?    ?? Bookings                                   ?
?    ?? Incidents                                  ?
?    ?? BookingDetails                             ?
?    ?? IncidentDetails                            ?
?    ?                                              ?
? 6. Check Permissions on Update                  ?
?    ?? UpdateBookingStatus                        ?
?    ?? UpdateIncidentStatus                       ?
???????????????????????????????????????????????????
```

---

## ?? **SECURITY CHECKS:**

### **1. Authorization Layers:**

**Layer 1: Controller Level**
```csharp
public class EmployeeController : AuthorizedBaseController
```
- ? K? th?a t? `AuthorizedBaseController`
- ? Ki?m tra Role = "Employee"

**Layer 2: Data Level**
```csharp
.Where(b => managedFieldIds.Contains(b.IDSanBong))
```
- ? L?c d? li?u theo sân ???c phân công
- ? Không cho phép truy c?p d? li?u ngoài quy?n

**Layer 3: Action Level**
```csharp
if (booking == null) {
    return HttpNotFound();
}
```
- ? Ki?m tra quy?n tr??c khi c?p nh?t
- ? Return 404 n?u không có quy?n

---

### **2. Validation Rules:**

| Rule | Check | Status |
|------|-------|--------|
| **Employee can only view assigned fields** | ? | Implemented |
| **Employee can only update assigned bookings** | ? | Implemented |
| **Employee can only update assigned incidents** | ? | Implemented |
| **Prevent direct URL access to unauthorized data** | ? | Implemented |
| **Show correct statistics in dashboard** | ? | Fixed |
| **Prevent schedule conflicts** | ? | Added |
| **Show who is handling incidents** | ? | Improved |

---

## ?? **RECOMMENDATIONS:**

### **1. ? ?Ã TH?C HI?N:**

- [x] Fix Employee Index statistics
- [x] Add managed fields list to dashboard
- [x] Improve incident assignment logic
- [x] Add schedule conflict validation
- [x] Show handler name in error messages

---

### **2. ?? ?? XU?T T??NG LAI (Optional):**

#### **A. Thêm field "IsActive" vào PhanCongNhanVien:**
```sql
ALTER TABLE PhanCongNhanVien
ADD IsActive BIT DEFAULT 1;
```

**L?i ích:**
- Phân công có th? "t?m ng?ng" mà không c?n xóa
- Filter d? dàng h?n
- L?ch s? phân công rõ ràng

**Usage:**
```csharp
var managedFieldIds = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
    .Where(pc => pc.IDuser == employeeId && pc.IsActive == true)
    .Select(pc => pc.IDSanBong)
    .ToList();
```

---

#### **B. Thêm Date Range cho phân công:**
```sql
ALTER TABLE PhanCongNhanVien
ADD StartDate DATE,
    EndDate DATE NULL;
```

**L?i ích:**
- Phân công có th?i h?n
- H?t h?n t? ??ng "vô hi?u hóa"
- L?ch làm vi?c rõ ràng

**Usage:**
```csharp
var today = DateTime.Today;
var managedFieldIds = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
    .Where(pc => pc.IDuser == employeeId 
              && pc.StartDate <= today 
              && (pc.EndDate == null || pc.EndDate >= today))
    .Select(pc => pc.IDSanBong)
    .ToList();
```

---

#### **C. Audit Log cho phân công:**
```csharp
// Log m?i l?n t?o/s?a/xóa phân công
public class AssignmentLog
{
    public int IDLog { get; set; }
    public int IDPhanCong { get; set; }
    public string Action { get; set; } // "Created", "Updated", "Deleted"
    public int IDAdmin { get; set; }
    public DateTime Timestamp { get; set; }
}
```

---

#### **D. Notification cho nhân viên:**
```csharp
// Khi Admin phân công ? G?i thông báo
public void CreateAssignment(PhanCongNhanVien assignment)
{
    _unitOfWork.PhanCongNhanVienRepository.Insert(assignment);
    _unitOfWork.Save();

    // T?o thông báo
    var notification = new ThongBao
    {
        IDuser = assignment.IDuser,
        NoiDung = $"B?n ?ã ???c phân công qu?n lý sân {assignment.SanBong.TenSanBong} vào ngày {assignment.NgayPhanCong:dd/MM/yyyy}",
        created_at = DateTime.Now,
        is_read = false
    };
    _unitOfWork.ThongBaoRepository.Insert(notification);
    _unitOfWork.Save();
}
```

---

## ?? **TEST SCENARIOS:**

### **1. Employee Data Access:**

**Test Case 1: View Bookings**
```
Given: Nhân viên A qu?n lý Sân 1, 2
When: Truy c?p /Employee/Bookings
Then: Ch? hi?n th? ??n c?a Sân 1, 2
```

**Test Case 2: View Other Field Booking**
```
Given: Nhân viên A qu?n lý Sân 1, 2
When: Truy c?p /Employee/BookingDetails/999 (Sân 3)
Then: Return 404 Not Found
```

**Test Case 3: Dashboard Statistics**
```
Given: Nhân viên A qu?n lý Sân 1 (có 5 ??n ch? duy?t)
      Sân 2 có 10 ??n ch? duy?t (không ph?i c?a A)
When: Truy c?p /Employee/Index
Then: ViewBag.PendingBookings = 5 (ch? c?a Sân 1)
```

---

### **2. Staff Assignment:**

**Test Case 4: Create Duplicate Assignment**
```
Given: Nhân viên A ?ã ???c phân công Sân 1 ngày 01/01/2024
When: Admin t?o phân công Sân 2 ngày 01/01/2024 cho A
Then: Hi?n th? warning "?ã ???c phân công Sân 1 trong cùng ngày"
```

**Test Case 5: Update Incident - Already Assigned**
```
Given: S? c? #123 ?ang ???c x? lý b?i Nhân viên B
When: Nhân viên A c? g?ng c?p nh?t
Then: Hi?n th? error "S? c? này ?ang ???c x? lý b?i Nhân viên B"
```

---

## ? **CONCLUSION:**

### **Security Level: ?? HIGH**

**Strengths:**
- ? Proper data filtering by assigned fields
- ? Authorization checks at multiple layers
- ? Prevent unauthorized access
- ? Correct statistics in dashboard
- ? Schedule conflict validation

**Improvements Made:**
- ? Fixed Employee Index statistics
- ? Added managed fields display
- ? Improved incident assignment logic
- ? Added schedule conflict check

**Recommendations:**
- ?? Consider adding IsActive field
- ?? Consider date range for assignments
- ?? Consider audit logging
- ?? Consider notification system

---

**Build Status:** ? SUCCESS  
**Logic Review:** ? PASSED  
**Security:** ? SECURE  
**Ready for:** ?? PRODUCTION

---

**Reviewed by:** GitHub Copilot  
**Date:** 2024  
**Version:** 1.0
