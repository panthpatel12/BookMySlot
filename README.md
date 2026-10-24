# BookMySlot — Appointment Booking System

> An Academic Mini-Project developed in **ASP.NET Core MVC with C#** and **SQLite**, designed to deliver real-time appointment booking, role-based scheduling, and concurrency-safe double-booking prevention.

---

## 1. Problem Statement
In traditional consultation and service centres (career counselling, educational advising, IT troubleshooting), appointments are booked manually over phone calls or maintained across unstructured spreadsheets. This leads to frequent scheduling conflicts, double-booking, lack of real-time schedule visibility, and customer dissatisfaction.

**BookMySlot** solves this by providing:
- A self-service portal where customers browse real-time open slots and reserve them instantly.
- An availability management system where service specialists create future slots without overlaps.
- A central administrator control hub to oversee services, specialists, and platform revenue.
- Hardware-backed relational database locks preventing concurrent double-booking.

---

## 2. Key Features by Role

### 👤 Customer
- **Public Discovery:** Browse active services, filter available slots by specialist or date.
- **Instant Booking:** Reserve any open future slot with optional session notes.
- **Customer Dashboard:** Metric cards for Upcoming, Completed, and Cancelled appointments.
- **Reschedule & Cancel:** One-click atomic cancellation (instantly releasing the slot) or rescheduling to another slot.
- **Audit History:** Comprehensive past appointment logs with timestamps.

### 🩺 Service Provider (Specialist)
- **Schedule Management:** Daily schedule view with date navigator and real-time status badges.
- **Slot Creation:** Open future availability with automatic overlap prevention.
- **Attendance Actions:** Mark booked sessions as **Completed** or **No-Show**.
- **Slot Removal:** Delete unbooked open slots to close personal availability.

### 🛡️ Administrator
- **Platform Analytics:** Real-time metrics for registered customers, providers, services, appointment distributions, and total completed revenue.
- **Service Management:** Full CRUD (Create, Read, Update, Soft-Deactivate) for service offerings with automatic memory-cache invalidation.
- **Provider Management:** Create provider accounts without exposed credentials; assign services via checkbox matrices; soft-deactivate providers.
- **Appointment Auditing:** Filter all bookings across the platform with **Session State** remembrance.

---

## 3. Technology Stack & Selected Versions

| Technology / Component | Version / Details | Purpose |
| :--- | :--- | :--- |
| **.NET SDK** | `10.0.401` (`net10.0`) | Modern C# 14 & high-performance runtime |
| **Framework** | ASP.NET Core MVC | Model-View-Controller presentation & controllers |
| **Database** | SQLite (`bookmyslot.db`) | Self-contained, zero-configuration local persistence |
| **ORM** | EF Core `10.0.13` | Code-First migrations and relational data access |
| **Identity & Security** | ASP.NET Core Identity `10.0.13` | PBKDF2 password hashing & secure cookie auth |
| **Frontend Styling** | Bootstrap 5.3 & Bootstrap Icons | Responsive Navy & Teal palette |
| **Caching** | `IMemoryCache` | Caching service catalog with cache invalidation |
| **Testing** | xUnit `2.9.3` & SQLite In-Memory | Automated integration and concurrency tests |

---

## 4. Prerequisites
- **.NET SDK 10.0** (or compatible .NET 8 / 10 runtime installed on Windows, macOS, or Linux).
- Visual Studio 2022 / Visual Studio Code / JetBrains Rider.

---

## 5. Quick Start & Execution

### Step 1: Open Terminal in Project Directory
```powershell
cd "e:\utu sem 5\BookMySlot\src\BookMySlot"
```

### Step 2: Run the Web Application
```powershell
dotnet run --urls "http://localhost:5100"
```

> **Note:** On the first run in `Development` mode, `DbInitializer.cs` automatically executes EF Core migrations to create `bookmyslot.db` and seeds all roles, services, providers, customers, and appointment slots. No manual SQL commands required!

### Step 3: Open in Browser
Visit: **[http://localhost:5100](http://localhost:5100)**

---

## 6. Pre-Configured Demo Accounts & One-Click Login

On the **Sign In** page (`/Account/Login`), **One-Click Academic Demo Buttons** are provided for instant evaluation:

| Role | Email Address | Password | Direct Dashboard |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin@bookmyslot.com` | `Admin123!` | `/Admin/Dashboard` |
| **Service Provider** | `provider@bookmyslot.com` | `Provider123!` | `/Provider/Dashboard` |
| **Customer** | `customer@bookmyslot.com` | `Customer123!` | `/Customer/Dashboard` |

*Additional customer demo accounts: `customer.arun@bookmyslot.local`, `customer.neha@bookmyslot.local`.*

---

## 7. Public REST API Endpoints

The system provides two public JSON endpoints documenting the configured time zone (`Asia/Kolkata` / UTC+05:30):

1. **Active Services Catalogue:**
   ```http
   GET http://localhost:5100/api/services
   ```
2. **Provider Available Slots for a Given Date:**
   ```http
   GET http://localhost:5100/api/providers/1/available-slots?date=2026-10-22
   ```

---

## 8. Automated Testing & Verification Status

Run the automated test suite from the solution root:
```powershell
cd "e:\utu sem 5\BookMySlot"
dotnet test BookMySlot.slnx
```

### Test Results
```text
Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 2 s
```
- ✅ `SuccessfulBooking_CreatesAppointment_AndSlotReservation`
- ✅ `ConcurrentBooking_SameSlot_ExactlyOneSucceeds` (SQLite double-booking concurrency test)
- ✅ `CustomerAppointmentOverlap_IsRejected`
- ✅ `ProviderSlotOverlap_IsRejected`
- ✅ `Cancellation_ReleasesSlotReservation_WhileRetainingAppointmentHistory`
- ✅ `FailedRescheduling_PreservesOriginalBooking`
- ✅ `UnauthorizedAccess_Rejected`
- ✅ `PastDateAndInactive_Rejected`

---

## 9. Documentation & Academic Poster Assets

- **Architecture Guide:** [`docs/architecture.md`](docs/architecture.md)
- **Data Dictionary:** [`docs/data-dictionary.md`](docs/data-dictionary.md)
- **Student Viva Guide:** [`docs/viva-guide.md`](docs/viva-guide.md)
- **Technical Poster Source (A2 HTML):** [`docs/poster.html`](docs/poster.html)
- **Technical Poster (A2 Print PDF):** [`docs/BookMySlot_A2_Poster.pdf`](docs/BookMySlot_A2_Poster.pdf)
- **Application Screenshots:** [`docs/screenshots/`](docs/screenshots/)

---

## 10. How to Push Code to GitHub

```bash
cd "e:\utu sem 5\BookMySlot"
git init
git add .
git commit -m "Initial commit: BookMySlot Appointment Booking System"
git branch -M main
git remote add origin https://github.com/your-username/BookMySlot.git
git push -u origin main
```
