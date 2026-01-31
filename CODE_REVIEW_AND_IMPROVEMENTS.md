# BÁO CÁO KI?M TRA VÀ C?I THI?N H? TH?NG ??T SÂN BÓNG

## I. CÁC V?N ?? NGHIÊM TR?NG PHÁT HI?N

### 1. **V?N ?? AUTHORIZATION BLOCKING PUBLIC PAGES** ?? **CRITICAL**

**V?n ??:**
- `BaseController` yêu c?u ??ng nh?p cho T?T C? controllers k? th?a nó
- `HomeController` (trang công khai) k? th?a `BaseController` 
- ? Ng??i dùng ch?a ??ng nh?p KHÔNG TH? xem trang ch?, danh sách sân, chi ti?t sân

**File affected:**
- `DatSanBong\Controllers\BaseController.cs`
- `DatSanBong\Controllers\HomeController.cs`

**Gi?i pháp ?ã tri?n khai:**
```csharp
// Tách thành 2 class:
// 1. BaseController - Không yêu c?u ??ng nh?p (cho trang công khai)
public class BaseController : Controller 
{
    protected readonly IUnitOfWork _unitOfWork;
    // Ch? kh?i t?o UnitOfWork, không check authentication
}

// 2. AuthorizedBaseController - Yêu c?u ??ng nh?p (cho trang c?n b?o v?)
public class AuthorizedBaseController : BaseController
{
    protected override void OnActionExecuting(...)
    {
        // Check authentication và authorization
    }
}
```

**Controllers c?n c?p nh?t:**
- ? `BaseController` - ?ã s?a
- ?? `HomeController` - K? th?a `BaseController` (công khai)
- ?? `BookingController` - K? th?a `AuthorizedBaseController` (yêu c?u ??ng nh?p)
- ?? `UserController` - K? th?a `AuthorizedBaseController`
- ?? `AdminController` - K? th?a `AuthorizedBaseController`
- ?? `EmployeeController` - K? th?a `AuthorizedBaseController`
- ?? `AccountController` - K? th?a `BaseController` (công khai)

---

### 2. **BUSINESS VALIDATION THI?U SÓT**

#### 2.1. Không ki?m tra sân có ?ang ho?t ??ng
**File:** `BookingController.cs`, `HomeController.cs`

**V?n ??:**
- Ng??i dùng có th? ??t sân ?ang b?o trì (`TrangThaiSan_ = false`)
- Không filter sân inactive trong dropdown

**Gi?i pháp:**
```csharp
// BookField POST action
var sanBong = _unitOfWork.SanBongRepository.GetById(model.IDSanBong);
if (sanBong == null || !sanBong.TrangThaiSan_)
{
    ModelState.AddModelError("", "Sân bóng hi?n không ho?t ??ng.");
}

// Dropdown ch? hi?n sân active
ViewBag.SanBongList = new SelectList(
    _unitOfWork.SanBongRepository.AsQueryable().Where(s => s.TrangThaiSan_ == true),
    "IDSanBong", "TenSanBong"
);
```

#### 2.2. Không ki?m tra th?i gian ??t t?i thi?u
**File:** `BookingController.cs`

**V?n ??:**
- Cho phép ??t sân d??i 1 gi? (vd: 15 phút)

**Gi?i pháp:**
```csharp
var duration = model.end_time - model.start_time;
if (duration.TotalHours < 1)
{
    ModelState.AddModelError("", "Th?i gian ??t sân t?i thi?u ph?i là 1 gi?.");
}
```

#### 2.3. Không validate gi? ho?t ??ng c?a sân
**V?n ??:**
- Cho phép ??t sân lúc 2h sáng (n?u không có ??n nào trùng)

**Gi?i pháp ?? xu?t:**
```csharp
// Thêm vào SanBong model ho?c system config
const TimeSpan OPENING_TIME = new TimeSpan(6, 0, 0);  // 6:00 AM
const TimeSpan CLOSING_TIME = new TimeSpan(23, 0, 0); // 11:00 PM

if (model.start_time < OPENING_TIME || model.end_time > CLOSING_TIME)
{
    ModelState.AddModelError("", "Sân ch? ho?t ??ng t? 6:00 - 23:00");
}
```

---

### 3. **V?N ?? V?I VIEW LOGIC**

#### 3.1. ChangeBooking.cshtml - Logic l?n x?n
**File:** `DatSanBong\Views\Booking\ChangeBooking.cshtml`

**V?n ??:**
- Hi?n th? nút "??i l?ch" TRONG chính trang ??i l?ch
- Hi?n th? nút "H?y ??n" trong trang ??i l?ch
- Hi?n th? logic ?ánh giá sân (không liên quan)
- Code l?p l?i t? BookingDetails

**Gi?i pháp:**
```razor
<!-- Nên ??n gi?n hóa: -->
@* Ch? hi?n th? form ??i l?ch *@
@using (Html.BeginForm("ChangeBooking", "Booking", FormMethod.Post))
{
    @* Form fields *@
    <input type="submit" value="C?p nh?t l?ch" />
    @Html.ActionLink("H?y", "BookingDetails", new { id = Model.IDBooking })
}

@* Xóa t?t c? logic ki?m tra rating, cancel button *@
@* Các ch?c n?ng ?ó thu?c v? BookingDetails.cshtml *@
```

#### 3.2. StaffAssignments.cshtml - Thi?u Include
**File:** `DatSanBong\Views\Admin\StaffAssignments.cshtml`

**V?n ??:**
```razor
<td>@(item.NguoiDung1?.fullname ?? "N/A")</td>  
<!-- NguoiDung1 có th? null n?u không Include -->
```

**Gi?i pháp trong AdminController:**
```csharp
var assignments = _unitOfWork.PhanCongNhanVienRepository.AsQueryable()
    .Include(pc => pc.NguoiDung)   // Nhân viên
    .Include(pc => pc.SanBong)     // Sân
    .Include(pc => pc.NguoiDung1)  // ?? C?N THÊM: Admin phân công
    .OrderByDescending(pc => pc.NgayPhanCong)
    .ToList();
```

---

### 4. **HI?U N?NG VÀ DATABASE ISSUES**

#### 4.1. Recalculating Reviews m?i request
**File:** `HomeController.cs` - Index, FieldDetails

**V?n ?? hi?n t?i:**
```csharp
// Tính toán l?i m?i request
foreach (var sanBong in sanBongList)
{
    sanBong.AverageDanhGia = (decimal)sanBong.Reviews.Average(r => r.rating);
    sanBong.TongLuotDanhGia = sanBong.Reviews.Count();
}
```

**V?n ??:**
- Tính toán l?i m?i l?n load trang
- N+1 query problem n?u không Include Reviews
- Không hi?u qu? v?i dataset l?n

**Gi?i pháp t?t h?n:**

**Option 1: Update khi có review m?i (Recommended)**
```csharp
// UserController.cs - RateField POST
[HttpPost]
public ActionResult RateField(int bookingId, int rating, string comment)
{
    // ... validation ...
    
    _unitOfWork.ReviewsRepository.Insert(review);
    _unitOfWork.Save();
    
    // C?P NH?T NGAY VÀO DB
    UpdateFieldRating(booking.IDSanBong);
    
    return RedirectToAction("BookingDetails", new { id = bookingId });
}

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
```

**Option 2: Stored Procedure (Advanced)**
```sql
CREATE PROCEDURE UpdateFieldRating
    @IDSanBong INT
AS
BEGIN
    UPDATE SanBong
    SET AverageDanhGia = (SELECT AVG(CAST(rating AS DECIMAL(3,2))) FROM Reviews WHERE IDSanBong = @IDSanBong),
        TongLuotDanhGia = (SELECT COUNT(*) FROM Reviews WHERE IDSanBong = @IDSanBong)
    WHERE IDSanBong = @IDSanBong
END
```

**Option 3: Database Trigger (Most Automatic)**
```sql
CREATE TRIGGER trg_UpdateFieldRating
ON Reviews
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    UPDATE SanBong
    SET AverageDanhGia = r.AvgRating,
        TongLuotDanhGia = r.TotalReviews
    FROM SanBong s
    INNER JOIN (
        SELECT IDSanBong, 
               AVG(CAST(rating AS DECIMAL(3,2))) as AvgRating,
               COUNT(*) as TotalReviews
        FROM Reviews
        WHERE IDSanBong IN (SELECT DISTINCT IDSanBong FROM inserted UNION SELECT DISTINCT IDSanBong FROM deleted)
        GROUP BY IDSanBong
    ) r ON s.IDSanBong = r.IDSanBong
END
```

#### 4.2. Không s? d?ng AsNoTracking cho read-only queries
**Nhi?u file Controllers**

**V?n ??:**
```csharp
// Không c?n tracking n?u ch? ??c
var bookings = _unitOfWork.DonDatSanRepository.AsQueryable()
    .Include(b => b.SanBong)
    .ToList();
```

**Gi?i pháp:**
```csharp
// Thêm AsNoTracking() cho queries ch? ??c
var bookings = _unitOfWork.DonDatSanRepository.AsQueryable()
    .AsNoTracking()  // T?ng hi?u n?ng
    .Include(b => b.SanBong)
    .ToList();
```

---

### 5. **SECURITY ISSUES**

#### 5.1. Thi?u ki?m tra ownership trong ChangeBooking
**File:** `BookingController.cs`

**V?n ??:**
- ?ã có check `b.IDuser == userId` trong GET
- Nh?ng c?n ??m b?o consistency

**Status:** ? ?ã ???c implement ?úng

#### 5.2. Admin có th? t? xóa tài kho?n c?a mình
**File:** `AdminController.cs - DeleteUser`

**V?n ??:**
```csharp
// Không cho phép xóa tài kho?n Admin ?ang ??ng nh?p
if (id == (int)Session["UserID"])
{
    TempData["ErrorMessage"] = "Không th? xóa tài kho?n Admin ?ang ??ng nh?p.";
    return RedirectToAction("Users");
}
```

**Status:** ? ?ã ???c implement

#### 5.3. Không validate CSRF token ? m?t s? n?i
**Các file View**

**Ki?m tra:**
- ? BookField - Có `@Html.AntiForgeryToken()`
- ? ChangeBooking - Có
- ? CancelBooking - Có
- ? Admin forms - Có

**Status:** ? ?ã ???c implement ??y ??

---

## II. C?I TI?N ?Ã TH?C HI?N

### ? 1. Tách BaseController thành 2 l?p
- `BaseController` - Cho public pages
- `AuthorizedBaseController` - Cho protected pages

### ? 2. Thêm Dispose pattern vào BaseController
```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        _unitOfWork?.Dispose();
    }
    base.Dispose(disposing);
}
```

---

## III. C?I TI?N C?N TH?C HI?N

### ?? Priority 1 - CRITICAL

1. **C?p nh?t t?t c? Controllers ?? k? th?a ?úng BaseController**
   - ? BaseController - Done
   - ?? BookingController ? `AuthorizedBaseController`
   - ?? UserController ? `AuthorizedBaseController`
   - ?? AdminController ? `AuthorizedBaseController`
   - ?? EmployeeController ? `AuthorizedBaseController`
   - ?? AccountController ? `BaseController`
   - ?? HomeController ? `BaseController` (?ã ?úng nh?ng c?n verify)

2. **Thêm validation cho sân ho?t ??ng**
   - BookingController.BookField
   - BookingController.ChangeBooking
   - Filter dropdown ch? hi?n sân active

3. **Thêm validation th?i gian t?i thi?u (1 gi?)**
   - BookingController.BookField
   - BookingController.ChangeBooking

### ?? Priority 2 - IMPORTANT

4. **T?i ?u hóa Review Calculation**
   - Implement update-on-insert approach
   - Ho?c s? d?ng Database Trigger

5. **Cleanup ChangeBooking.cshtml**
   - Xóa logic rating, cancel button
   - Ch? gi? form ??i l?ch

6. **Thêm Include cho Admin assignments**
   - AdminController.StaffAssignments
   - Include `NguoiDung1` (Admin)

### ?? Priority 3 - ENHANCEMENT

7. **Thêm validation gi? ho?t ??ng**
   - Ki?m tra booking trong gi? m? c?a (6:00 - 23:00)

8. **Thêm AsNoTracking cho read-only queries**
   - T?t c? list/search actions
   - T?ng hi?u n?ng ~20-30%

9. **Thêm paging cho danh sách l?n**
   - Admin.Users
   - Admin.Bookings
   - User.MyBookings

10. **Logging và Error Handling**
    - Try-catch cho database operations
    - Log errors to file/database

---

## IV. WORKFLOW C?I TI?N ?? XU?T

### Current Workflow: ??t sân
```
User ? BookField (Form) ? BookField (POST) 
? Validate ? Check conflict ? Insert ? Redirect to BookingDetails
```

**V?n ??:**
- Không check sân có active không
- Không check th?i gian t?i thi?u
- Không validate gi? m? c?a

### Improved Workflow: ??t sân
```
User ? BookField (Form - ch? hi?n sân active)
     ? BookField (POST)
     ? Validate:
        ? Sân có t?n t?i?
        ? Sân có active?
        ? Th?i gian >= 1 gi??
        ? Trong gi? m? c?a (6:00-23:00)?
        ? Không quá kh??
        ? Không trùng l?ch?
     ? Calculate TongTien
     ? Insert v?i status "Ch? duy?t"
     ? Redirect to BookingDetails
```

### Current Workflow: ?ánh giá sân
```
User ? RateField (Form) ? RateField (POST)
     ? Insert Review ? Redirect
     
HomeController.Index:
     ? Load SanBong + Reviews
     ? Tính toán AverageDanhGia, TongLuotDanhGia
     ? Display
```

**V?n ??:**
- Tính toán l?i m?i request
- Không hi?u qu?

### Improved Workflow: ?ánh giá sân
```
User ? RateField (Form) ? RateField (POST)
     ? Insert Review
     ? Update SanBong.AverageDanhGia, TongLuotDanhGia  ? C?I TI?N
     ? Redirect
     
HomeController.Index:
     ? Load SanBong (AverageDanhGia, TongLuotDanhGia ?ã có s?n)
     ? Display
```

**Ho?c s? d?ng Database Trigger (t?t nh?t):**
```sql
TRIGGER on Reviews INSERT/UPDATE/DELETE
  ? Auto update SanBong.AverageDanhGia, TongLuotDanhGia
```

---

## V. TESTING CHECKLIST

### Functional Testing

- [ ] **Public Access (không ??ng nh?p)**
  - [ ] Xem trang ch? (Home/Index)
  - [ ] Xem chi ti?t sân (Home/FieldDetails)
  - [ ] Ki?m tra l?ch tr?ng (Home/CheckAvailability)
  - [ ] Xem About, Contact

- [ ] **Customer Functions (??ng nh?p Customer)**
  - [ ] ??t sân v?i sân active
  - [ ] Không ??t ???c sân inactive
  - [ ] Không ??t ???c < 1 gi?
  - [ ] Không ??t ???c quá kh?
  - [ ] Không ??t ???c slot trùng
  - [ ] ??i l?ch (ch? khi còn > 2h)
  - [ ] H?y ??n
  - [ ] ?ánh giá sân (sau khi hoàn thành)
  - [ ] Báo cáo s? c?

- [ ] **Employee Functions**
  - [ ] Ch? xem ???c ??n c?a sân ???c phân công
  - [ ] C?p nh?t tr?ng thái ??n
  - [ ] X? lý s? c? c?a sân ???c phân công

- [ ] **Admin Functions**
  - [ ] Qu?n lý users
  - [ ] Qu?n lý sân (CRUD)
  - [ ] Duy?t/T? ch?i ??n ??t sân
  - [ ] Phân công nhân viên
  - [ ] Xem báo cáo

### Security Testing

- [ ] Không bypass authentication cho protected pages
- [ ] Không truy c?p ???c ??n c?a ng??i khác
- [ ] Không update ???c data c?a ng??i khác
- [ ] CSRF token ho?t ??ng
- [ ] SQL Injection prevention (Entity Framework)

### Performance Testing

- [ ] Load time < 2s cho trang danh sách sân
- [ ] Review calculation không ?nh h??ng performance
- [ ] Paging ho?t ??ng v?i > 1000 records

---

## VI. MIGRATION PLAN

### Phase 1: Critical Fixes (1-2 ngày)
1. S?a BaseController inheritance
2. Thêm validation sân active
3. Thêm validation th?i gian t?i thi?u
4. Test thoroughly

### Phase 2: Important Improvements (2-3 ngày)
5. Optimize review calculation
6. Cleanup ChangeBooking view
7. Fix Admin assignment Include
8. Add AsNoTracking

### Phase 3: Enhancements (3-5 ngày)
9. Add gi? ho?t ??ng validation
10. Implement paging
11. Add logging
12. Performance optimization

---

## VII. K?T LU?N

### ?i?m m?nh c?a h? th?ng hi?n t?i:
? C?u trúc d? án rõ ràng (Controller, Model, View)
? S? d?ng Unit of Work pattern
? Có phân quy?n c? b?n
? Có CSRF protection
? Business logic t??ng ??i ??y ??

### ?i?m y?u c?n c?i thi?n:
?? Authorization blocking public pages (CRITICAL)
?? Thi?u m?t s? business validation quan tr?ng
?? Hi?u n?ng có th? t?i ?u h?n
?? View logic có ch? l?n x?n

### T?ng quan:
H? th?ng có foundation t?t nh?ng c?n s?a m?t s? l?i nghiêm tr?ng v? authorization và b? sung validation. Sau khi áp d?ng các c?i ti?n ?? xu?t, h? th?ng s? ?n ??nh và s?n sàng cho production.

**?u tiên cao nh?t:** S?a BaseController ?? không block public pages.
