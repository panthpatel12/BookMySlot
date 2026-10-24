# How to Run BookMySlot on a Different PC

Follow these simple instructions to run this project on any other laptop or PC (college lab, examiner laptop, or home computer).

---

### Step 1: Install .NET SDK (Only requirement!)
If the other computer does not already have .NET installed:
1. Go to: **[https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)**
2. Download and install **.NET 10.0 SDK** (or **.NET 8.0 SDK**).
3. Restart your terminal or command prompt.

> **Note on Database:** You do **NOT** need to install MySQL, XAMPP, or Docker! The application uses **SQLite**, which is self-contained. The database file `bookmyslot.db` and all sample accounts/services are generated automatically on first launch.

---

### Step 2: Copy the Project Folder
Copy the entire `BookMySlot` folder (via pen drive, zip file, or GitHub clone) to any directory on the new PC.

---

### Step 3: Run the Application (One-Click)

#### On Windows:
Just **double-click** the file:
```cmd
run.bat
```
*(Or right-click `run.ps1` &rarr; "Run with PowerShell")*

#### Or via Command Line (Windows / Mac / Linux):
```bash
cd BookMySlot/src/BookMySlot
dotnet run --urls "http://localhost:5100"
```

The browser will open automatically at:
👉 **[http://localhost:5100](http://localhost:5100)**

---

### Step 4: Login & Test Roles

On the login page, you can use the **One-Click Demo Buttons** or log in manually:

| Role | Email | Password |
| :--- | :--- | :--- |
| **Administrator** | `admin@bookmyslot.com` | `Admin123!` |
| **Service Provider** | `provider@bookmyslot.com` | `Provider123!` |
| **Customer** | `customer@bookmyslot.com` | `Customer123!` |

---

### Step 5: Run Automated Tests
To run the automated verification test suite on the new PC:
- On Windows: Double-click **`test.bat`**
- Or run: `dotnet test BookMySlot.slnx`

All 8 integration and concurrency tests will execute and pass.
