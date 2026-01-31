# ?? BUGFIX - AccessDenied Redirect Issue

## ?? Date: 2024
## ?? Issue: Employee redirected to Customer homepage after Access Denied

---

## ?? **PROBLEM DESCRIPTION:**

### **Issue:**
Khi Employee truy c?p vào trang không có quy?n và b? redirect ??n AccessDenied, sau khi ?n nút "Quay v? trang ch?", h? b? chuy?n ??n `Home/Index` (trang c?a Customer) thay vì `Employee/Index`.

### **User Flow:**
```
1. Employee ??ng nh?p
2. C? truy c?p /Admin/Users (không có quy?n)
3. Redirect to /Account/AccessDenied
4. Click "Quay v? trang ch?"
5. ? Redirect to /Home/Index (WRONG!)
   ? Should redirect to /Employee/Index
```

### **Impact:**
- ? Employee confused (t?i sao l?i vào trang Customer?)
- ? Poor user experience
- ? Inconsistent navigation flow

---

## ?? **ROOT CAUSE:**

### **AccessDenied.cshtml - BEFORE:**

```razor
@{
    ViewBag.Title = "Truy c?p b? t? ch?i";
    Layout = "~/Views/Shared/_Layout.cshtml";
}

<div class="jumbotron">
    <h1 class="text-danger">Truy c?p b? t? ch?i!</h1>
    <p class="lead">B?n không có quy?n truy c?p vào tài nguyên này.</p>
    
    <!-- ? PROBLEM: Always redirect to Home/Index -->
    <p><a href="@Url.Action("Index", "Home")" class="btn btn-primary btn-lg">
        Quay v? trang ch? &raquo;
    </a></p>
    
    <p><a href="@Url.Action("Logout", "Account")" class="btn btn-warning btn-lg">
        ??ng xu?t &raquo;
    </a></p>
</div>
```

**Problem:**
- Hard-coded redirect to `Home/Index`
- Không ki?m tra role c?a user
- T?t c? roles ??u b? redirect v? trang Customer

---

## ? **SOLUTION:**

### **AccessDenied.cshtml - AFTER:**

```razor
@{
    ViewBag.Title = "Truy c?p b? t? ch?i";
    Layout = "~/Views/Shared/_Layout.cshtml";
    
    // ? FIX: Xác ??nh trang ch? phù h?p v?i role
    string homeController = "Home";
    string homeAction = "Index";
    
    if (Session["UserRoleID"] != null)
    {
        int userRoleID = (int)Session["UserRoleID"];
        
        if (userRoleID == 3) // Admin
        {
            homeController = "Admin";
        }
        else if (userRoleID == 2) // Employee
        {
            homeController = "Employee";
        }
        // else: Customer (default Home)
    }
}

<!-- ? FIXED: Dynamic redirect based on role -->
<a href="@Url.Action(homeAction, homeController)" class="btn-action btn-primary">
    <i class="fas fa-home"></i>
    Quay v? trang ch?
</a>
```

---

## ?? **REDIRECT LOGIC:**

### **Decision Table:**

| UserRoleID | Role | Redirect To | URL |
|------------|------|-------------|-----|
| **3** | Admin | Admin/Index | `/Admin/Index` |
| **2** | Employee | Employee/Index | `/Employee/Index` |
| **1** | Customer | Home/Index | `/Home/Index` |
| **null** | Not logged in | Home/Index | `/Home/Index` |

---

## ?? **TEST SCENARIOS:**

### **Test Case 1: Employee Access Denied**
```
GIVEN: User logged in as Employee (IDrole = 2)
WHEN: Try to access /Admin/Users
THEN: 
  1. Redirect to /Account/AccessDenied
  2. Click "Quay v? trang ch?"
  3. ? Redirect to /Employee/Index
```

### **Test Case 2: Customer Access Denied**
```
GIVEN: User logged in as Customer (IDrole = 1)
WHEN: Try to access /Admin/Index
THEN:
  1. Redirect to /Account/AccessDenied
  2. Click "Quay v? trang ch?"
  3. ? Redirect to /Home/Index
```

### **Test Case 3: Admin Access Denied (Edge Case)**
```
GIVEN: User logged in as Admin (IDrole = 3)
WHEN: Try to access restricted page (if any)
THEN:
  1. Redirect to /Account/AccessDenied
  2. Click "Quay v? trang ch?"
  3. ? Redirect to /Admin/Index
```

### **Test Case 4: Not Logged In**
```
GIVEN: User not logged in (Session["UserRoleID"] = null)
WHEN: Try to access restricted page
THEN:
  1. Redirect to /Account/Login (by OnActionExecuting)
  2. If somehow reach AccessDenied:
  3. ? Redirect to /Home/Index (default)
```

---

## ?? **UI IMPROVEMENTS:**

### **Modern Design:**

**Before:**
```html
<div class="jumbotron">
    <h1 class="text-danger">Truy c?p b? t? ch?i!</h1>
    <p class="lead">B?n không có quy?n truy c?p...</p>
    <a href="..." class="btn btn-primary btn-lg">Quay v?...</a>
</div>
```

**After:**
```html
<div class="access-denied-card">
    <!-- Icon wrapper with animation -->
    <div class="icon-wrapper">
        <i class="fas fa-ban"></i>
    </div>
    
    <!-- Title -->
    <h1 class="access-denied-title">Truy C?p B? T? Ch?i!</h1>
    
    <!-- Message -->
    <p class="access-denied-message">...</p>
    
    <!-- Info box with reasons -->
    <div class="info-box">
        <div class="info-box-title">
            <i class="fas fa-info-circle"></i>
            Lý do có th?:
        </div>
        <ul class="info-box-list">
            <li>B?n không có quy?n truy c?p...</li>
            <li>Trang yêu c?u vai trò khác...</li>
            <li>Phiên ??ng nh?p h?t h?n...</li>
            <li>Truy c?p tài nguyên không thu?c quy?n...</li>
        </ul>
    </div>
    
    <!-- Action buttons -->
    <div class="action-buttons">
        <a href="@Url.Action(...)" class="btn-action btn-primary">
            <i class="fas fa-home"></i>
            Quay v? trang ch?
        </a>
        <a href="@Url.Action("Logout", "Account")" class="btn-action btn-warning">
            <i class="fas fa-sign-out-alt"></i>
            ??ng xu?t
        </a>
    </div>
</div>
```

**Features:**
- ? Modern card-based layout
- ? Large ban icon
- ? Info box with possible reasons
- ? Gradient buttons with icons
- ? Responsive design
- ? Smooth animations

---

## ?? **CODE CHANGES:**

### **File Modified:**
`DatSanBong\Views\Account\AccessDenied.cshtml`

### **Changes:**

1. **Add Role Detection:**
   ```razor
   string homeController = "Home";
   if (Session["UserRoleID"] != null)
   {
       int userRoleID = (int)Session["UserRoleID"];
       if (userRoleID == 3) homeController = "Admin";
       else if (userRoleID == 2) homeController = "Employee";
   }
   ```

2. **Dynamic Redirect:**
   ```razor
   <a href="@Url.Action(homeAction, homeController)">
   ```

3. **Modern UI:**
   - Card-based layout
   - Icon wrapper
   - Info box with reasons
   - Gradient buttons
   - Responsive CSS

---

## ? **VALIDATION:**

### **Checklist:**
- [x] Employee redirects to /Employee/Index
- [x] Admin redirects to /Admin/Index
- [x] Customer redirects to /Home/Index
- [x] Not logged in redirects to /Home/Index (default)
- [x] Modern UI design
- [x] Responsive layout
- [x] Info box with reasons
- [x] Icons on buttons

---

## ?? **BEFORE vs AFTER:**

| Aspect | Before | After |
|--------|--------|-------|
| **Redirect** | Always Home/Index | ? Role-based |
| **Employee** | ? Home/Index | ? Employee/Index |
| **Admin** | ? Home/Index | ? Admin/Index |
| **Customer** | ? Home/Index | ? Home/Index |
| **UI** | Basic jumbotron | ? Modern card |
| **Info** | Generic message | ? Detailed reasons |
| **Icons** | None | ? FontAwesome |
| **Responsive** | Basic | ? Optimized |

---

## ?? **RESULT:**

```
?? Bug: FIXED
? Employee ? Employee/Index
? Admin ? Admin/Index
? Customer ? Home/Index
?? UI: MODERN
?? Responsive: YES
?? Status: PRODUCTION READY
```

---

## ?? **RELATED FIXES:**

This fix is part of the authorization system improvements:

1. **BaseController.cs** - IDrole-based authorization
2. **AccountController.cs** - Save UserRoleID to Session
3. **AccessDenied.cshtml** - Role-based redirect ? **THIS FIX**

All three work together to provide secure, role-based navigation!

---

## ?? **FUTURE ENHANCEMENTS:**

1. **Log Access Denied Events:**
   ```csharp
   // In BaseController
   if (accessDenied)
   {
       LogAccessDenied(userId, requestedUrl, userRole);
   }
   ```

2. **Show Requested URL:**
   ```razor
   <p>B?n ?ã c? truy c?p: <code>@Request.UrlReferrer</code></p>
   ```

3. **Contact Admin Button:**
   ```html
   <a href="mailto:admin@example.com">Liên h? qu?n tr? viên</a>
   ```

4. **Back Button:**
   ```javascript
   <button onclick="history.back()">Quay l?i trang tr??c</button>
   ```

---

**Build:** ? **SUCCESS**  
**Bug:** ?? **FIXED**  
**UI:** ?? **MODERN**  
**Status:** ?? **PRODUCTION READY**

---

**Fixed by:** GitHub Copilot  
**Date:** 2024  
**Version:** 1.0 - Role-Based Redirect Fix
