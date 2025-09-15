# 💼 WCPS — Web-based Claims Processing System  

A **capstone project** built during internship training — this system streamlines employee expense claim submission, admin approval/rejection, and audit tracking.  

Employees can easily submit receipts for expenses, while admins get a dashboard to process claims with full visibility into history.  

---

## ✨ Features  

### 👨‍💼 Employee Portal  
- Submit claims with receipt uploads  
- Track pending, approved, or rejected claims  
- View recent activity  

### 🛡️ Admin Portal  
- Process pending claims (approve/reject with amount adjustment)  
- Manage employees and roles  
- Review audit trail of actions  
- Dashboard with claim statistics (powered by Chart.js)  

### 📜 Audit Trail  
- Every action (claim submission, approval, rejection) logged  
- Transparent history for compliance  

### 📂 Secure File Uploads  
- Uploaded receipts stored outside webroot  
- Old files cleaned up automatically  

---

## 🛠️ Tech Stack  
- **Backend:** ASP.NET Core 8.0 (MVC + Identity)  
- **Frontend:** Razor Pages + Bootstrap + Chart.js  
- **Database:** SQL Server LocalDB.  
- **Authentication:** ASP.NET Core Identity (with role-based access: Employee, CpdAdmin, Finance)  

---

## ⚡ Getting Started  

### Prerequisites  
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)  
- SQL Server LocalDB **OR** Docker with SQL Server image  

### Setup  

1. **Clone repo**  
   ```bash
   git clone https://github.com/wabi-sabi-ux/WCPS.git
   cd WCPS/WCPS.WebApp
2. **Configure database connection**
    ```json
    "ConnectionStrings": {
    "DefaultConnection": "Server=            (localdb)\\MSSQLLocalDB;Database=WCPSDb;Trusted_Connection=True;MultipleActiveResultSets=true"
    }
3. **Add Migrations**
   ```bash
   dotnet ef database update --project WCPS.WebApp.csproj

4. **Run the App**
   ```bash
   dotnet run

##👤 Default Admin User
-At first run, seed data creates an admin account:
-Email: admin@wcps.local
-Password: Admin@1234
-Role: CpdAdmin

## 📁 Project Structure
WCPS.WebApp/
 ├── Controllers/        # MVC controllers (Claims, Admin, Employees)
 ├── Models/             # Entity models (ClaimRequest, AuditTrail, ApplicationUser)
 ├── Data/               # DbContext + SeedData
 ├── Views/              # Razor views (Dashboard, Claims, Admin)
 ├── wwwroot/            # Static assets
 └── Uploads/            # Secure folder for receipts (ignored by git)
