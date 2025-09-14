# Web Based Claims Processing System (WCPS)

## ✅ Completed
- ASP.NET Core MVC project scaffolded.
- Identity setup with Register/Login/Logout.
- Claims system:
  - Employee can create claim with PDF upload.
  - Employees can view their own claims.
  - Claims list polished (badges, search, preview modal).
- Admin system:
  - CPD Admin role added.
  - AdminEmployees CRUD (with BankAccountNumber).
  - AdminClaims (pending claims list, process page with audit trail).
- Audit Trail tracking for claim actions.
- BankAccountNumber added to employee model and views.

## ⚠️ Fixes Applied
- `AdminProcessViewModel` updated so display-only fields (`Title`, `ClaimRef`, `EmployeeName`) are optional, fixing validation issues on Process POST.
- Confirm download modals added for receipts.
- Polished employee & admin claim tables.



---
