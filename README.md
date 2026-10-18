# 🅿️ ParkPulse - Smart Parking Reservation System (SPR-System)

A modern, enterprise-grade full-stack Smart Parking Reservation & Management System built with **ASP.NET Core 8 Web API**, **PostgreSQL**, and **Angular 18**.

---

## 🚀 Key Features

- **Multi-Role RBAC**: Built-in access control for **Admin**, **Parking Attendant**, and **Customer**.
- **Real-Time Slot Booking**: Visual slot selection, temporal collision prevention, and server-side pricing.
- **Secure Payments**: Idempotent payment processing supporting Credit/Debit Cards and Mobile Money.
- **QR Code Gate Passes**: Automated Base64 QR code generation upon payment for paperless entry.
- **Gate Operations**: 15-minute grace period, live check-in/check-out scanning, and overstay calculation.
- **Analytics Dashboards**: Real-time occupancy, revenue reporting, peak hours, and audit logs.

---

## 🛠️ Tech Stack

| Tier | Technologies |
| :--- | :--- |
| **Backend** | .NET 8 Web API, Clean Architecture (DDD), EF Core 8, Npgsql, FluentValidation |
| **Database** | PostgreSQL with 17 foreign key constraints, indexes, and automated migrations |
| **Security** | PBKDF2 (100,000 iterations) password hashing, JWT Bearer tokens with refresh rotation |
| **Frontend** | Angular 18, Angular Material, RxJS, Chart.js, SCSS Responsive Design |

---

## 🔑 Default Seed Accounts

The system automatically seeds the database on initial startup:

| Role | Email | Password | Access Level |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@smartparking.com` | `Admin@Password123!` | System-wide management & reports |
| **Attendant** | `attendant@smartparking.com` | `Attendant@123456` | Gate scanner, check-in & check-out |
| **Customer** | `customer@smartparking.com` | `Customer@123456` | Vehicle registry, reservations & payments |

---

## 🏃 Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js (v18+) & npm](https://nodejs.org/)
- [PostgreSQL](https://www.postgresql.org/) running on `localhost:5432`

### 1. Backend API
```powershell
# Update connection string in backend/SmartParkingSystem.Api/appsettings.json if needed
dotnet run --project backend/SmartParkingSystem.Api --launch-profile http
```
*API will run at `http://localhost:5057` (Swagger documentation at `http://localhost:5057/swagger`).*

### 2. Frontend UI
```powershell
cd frontend
npm install
npm start
```
*Web application will open at `http://localhost:4200`.*

---

## 🧪 Testing & Verification

- **Backend Unit Tests**: **809 passed, 0 failed (100%)** across 15 domain and business rule test suites.
  ```powershell
  dotnet run --project backend/SmartParkingSystem.UnitTests
  ```
- **Full-Stack 20-Step Verification Plan**: **20/20 passed (100%)** testing builds, database, APIs, authentication, gate operations, and UI workflows.
  ```powershell
  powershell -ExecutionPolicy Bypass -File scratch/execute_20_step_testing_plan.ps1
  ```

---

## 📄 License
This project is licensed under the [MIT License](LICENSE).