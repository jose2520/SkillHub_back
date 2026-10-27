# SkillHub — Workshop & Enrollment Management API

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C# 13](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14%2B-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-00599E)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/Tests-81%20Passing-brightgreen)](tests/SkillHub.Tests)
[![Swagger](https://img.shields.io/badge/Swagger-OpenAPI%20v1-85EA2D?logo=swagger&logoColor=black)](http://localhost:5142/swagger)
[![Scalar](https://img.shields.io/badge/Scalar-API%20Reference-111827?logo=scalar&logoColor=white)](http://localhost:5142/scalar/v1)

## 📖 Project Description

**SkillHub** is a training center offering short, in-person practical workshops in photography, cooking, public speaking, graphic design, music, digital marketing, and entrepreneurship.

Previously, registrations were collected through forms and tracked manually in spreadsheets, causing overbooked workshops, duplicate registrations, and overlapping schedules for the same participant. **SkillHub API** solves this through a **Clean Architecture** RESTful platform in **.NET 10** where:
- **Visitors** browse upcoming workshops and real-time calculated available spots without logging in.
- **Participants** self-register, enroll in future workshops under strict domain validation rules, view and cancel their own enrollments (up to 24 hours before the start time), re-enroll if spots remain, and download official **PDF Enrollment Certificates** (`Constancia de inscripción`).
- **Employees (Instructors)** sign in from first startup, publish future workshops, inspect workshop attendees, bulk-import enrollments from Excel workbooks (`.xlsx`) with row-level validation reporting, cancel enrollments before workshop start without the 24-hour restriction, download any active enrollment certificate, and consult top-enrolled workshop reports.

---

## 🛠️ Technologies Used

| Layer / Concern | Technology |
| :--- | :--- |
| **Runtime & Language** | .NET 10 SDK / ASP.NET Core Web API / C# 13 |
| **Architecture** | Clean Architecture (`Domain`, `Application`, `Infrastructure`, `API`, `Tests`) |
| **Database & ORM** | PostgreSQL 14+ & Entity Framework Core 10 (Code-First Migrations via `Npgsql`) |
| **Identity & Security** | ASP.NET Core Identity (`IdentityDbContext`) + JWT Bearer (`HMAC-SHA256`) + Role-Based & Ownership Authorization |
| **Input Validation** | FluentValidation 11 + Rich Domain Value Objects & Entities |
| **Excel Seeding & Import** | EPPlus 8 (`SkillHub_Data.xlsx` initial seeding + `/api/v1/enrollments/import`) |
| **PDF Generation** | iText 7 / 9 (`ICertificatePdfService` in Application, `CertificatePdfService` in Infrastructure) |
| **Error Handling** | Centralized `GlobalExceptionHandler` (`IExceptionHandler` / RFC 7807 `ProblemDetails`) |
| **API Documentation** | Swagger UI (`/swagger`) with JWT Bearer Authorize support & Scalar (`/scalar/v1`) |
| **Automated Testing** | xUnit + Moq (`81` unit and integration tests) |

---

## 🏛️ Clean Architecture Structure

```text
SkillHub/
├── README.md                             # Project documentation, setup, credentials, and Author
├── SkillHub.sln                          # .NET solution file
├── database/
│   └── skillhub_dump.sql                 # Exported PostgreSQL final schema + seeded/imported data
├── src/
│   ├── SkillHub.Domain/                  # Core Layer (Zero external dependencies)
│   │   ├── Abstractions/                 # Repository, UnitOfWork, Clock & Event contracts
│   │   ├── Common/                       # Entity, AggregateRoot, ValueObject, IDomainEvent
│   │   ├── Entities/                     # Instructor, Participant, Workshop, Enrollment
│   │   ├── Enums/                        # UserRole / RoleNames, EnrollmentStatus
│   │   ├── Errors/ & Exceptions/         # Capacity, duplicate, schedule conflict & cancellation rules
│   │   ├── Events/                       # Domain events (WorkshopCreated, EnrollmentCreated/Cancelled)
│   │   └── ValueObjects/                 # EmailAddress, WorkshopCode, TimeSlot
│   │
│   ├── SkillHub.Application/             # Use-Case Layer (Depends only on Domain)
│   │   ├── DTOs/                         # Request/Response records (never exposes entities)
│   │   ├── Exceptions/                   # NotFound, Conflict, ForbiddenAccess, Validation, Unauthorized
│   │   ├── Interfaces/                   # Service, JWT, Excel & PDF generation abstractions
│   │   ├── Services/                     # Cross-entity domain orchestration (Workshop, Enrollment, Report)
│   │   ├── Validators/                   # FluentValidation input validators
│   │   └── DependencyInjection.cs
│   │
│   ├── SkillHub.Infrastructure/          # Technology & Persistence Layer
│   │   ├── Persistence/
│   │   │   ├── Configurations/           # EF Core Fluent API & partial unique active index
│   │   │   ├── Migrations/               # EF Core Code-First migrations
│   │   │   ├── Repositories/             # EF Core repositories & UnitOfWork
│   │   │   ├── Seeders/                  # DbSeeder: migrates DB & loads Instructors, Participants, Workshops
│   │   │   └── AppDbContext.cs           # Extends IdentityDbContext<IdentityUser>
│   │   ├── Resources/Data/               # Embedded SkillHub_Data.xlsx copied on build
│   │   ├── Services/                     # AuthService, JwtTokenService, ExcelImportService, CertificatePdfService
│   │   └── DependencyInjection.cs
│   │
│   └── SkillHub.API/                     # HTTP Presentation Layer (Thin Controllers)
│       ├── Controllers/                  # Auth, Workshops, Enrollments, Reports (/api/v1/...)
│       ├── Middleware/                   # GlobalExceptionHandler.cs
│       ├── appsettings.json              # PostgreSQL connection & JWT settings
│       ├── Program.cs                    # DI, CORS, JWT AuthN/AuthZ, Swagger/Scalar, Startup Seeder
│       └── SkillHub.API.http             # Ready-to-execute HTTP requests
│
└── tests/
    └── SkillHub.Tests/                   # xUnit tests covering Domain invariants & Application services
```

---

## ⚙️ Setup & Execution Steps (Including Database)

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 14+](https://www.postgresql.org/) running locally (or reachable via connection string)

### 1. Clone the Repository
```bash
git clone <YOUR_GITHUB_REPO_URL>
cd SkillHub
```

### 2. Configure PostgreSQL Connection & JWT Settings
Update `ConnectionStrings:DefaultConnection` inside `src/SkillHub.API/appsettings.json` (or provide environment variable `ConnectionStrings__DefaultConnection`) with your local PostgreSQL credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=skillhub_db;Username=postgres;Password=YOUR_POSTGRES_PASSWORD"
  },
  "Jwt": {
    "Key": "SkillHub_JwtSigningKey_2026_CleanArch_Min32Bytes!",
    "Issuer": "SkillHubAPI",
    "Audience": "SkillHubClient",
    "ExpiryMinutes": "120"
  }
}
```

### 3. Build, Apply Migrations & Seed Initial Data Automatically
You do **not** need to manually insert initial records: on startup, `DbSeeder` automatically executes `Database.MigrateAsync()` (Code-First migrations) and reads `SkillHub_Data.xlsx` to seed:
- **6 Instructors (Employees)**
- **30 Participants**
- **15 Upcoming Workshops**

*(Note: If you prefer to apply EF Core migrations manually from the CLI beforehand, run:)*
```bash
dotnet ef database update --project src/SkillHub.Infrastructure --startup-project src/SkillHub.API
```

*(Optional: A complete PostgreSQL export including schema and all seeded + imported rows is also available at `database/skillhub_dump.sql`.)*

### 4. Run Automated Tests
```bash
dotnet test SkillHub.sln --verbosity normal
```

### 5. Run the API
```bash
dotnet run --project src/SkillHub.API --launch-profile http
```

Once running, open in your browser:
- **Swagger UI:** `http://localhost:5142/swagger`
- **Scalar API Reference:** `http://localhost:5142/scalar/v1`
- **Health / Info Root:** `http://localhost:5142/`

---

## 🔐 Employee & Participant Credentials

All Employee (Instructor) accounts are automatically created and ready to log in on the very first application startup via `POST /api/v1/auth/login`:

### Employee Accounts (Role: `Employee`)
| Full Name | Email | Password | Specialty |
| :--- | :--- | :--- | :--- |
| **Andrés Felipe Mejía Arango** | `andres.mejia@correo.com` | `Instructor2026*` | Fotografía |
| **Rosa Elena Barrios Polo** | `rosa.barrios@correo.com` | `Instructor2026*` | Cocina |
| **Mauricio Jiménez Cárdenas** | `mauricio.jimenez@correo.com` | `Instructor2026*` | Oratoria |
| **Natalia Herrera Castaño** | `natalia.herrera@correo.com` | `Instructor2026*` | Diseño gráfico |
| **Hernán Darío Quintero Gil** | `hernan.quintero@correo.com` | `Instructor2026*` | Música |
| **Paola Andrea Vargas Mora** | `paola.vargas@correo.com` | `Instructor2026*` | Marketing digital |

### Sample Seeded Participant Accounts (Role: `Participant`)
| Full Name | Email | Password |
| :--- | :--- | :--- |
| **Jorge Andrés Pérez Rojas** | `jorge.perez@correo.com` | `Skillhub2026*` |
| **Luz Marina Ospina Ríos** | `luz.ospina@correo.com` | `Skillhub2026*` |

*(Any visitor can also register a new Participant account at `POST /api/v1/auth/register`.)*

---

## 🧪 How to Authenticate & Test (Swagger UI / Scalar)

### 1. Sign In (`POST /api/v1/auth/login`)
Open `http://localhost:5142/swagger` (or `http://localhost:5142/scalar/v1`), expand **`POST /api/v1/auth/login`**, click **Try it out**, and send either an **Employee** or **Participant** payload:

```json
{
  "email": "andres.mejia@correo.com",
  "password": "Instructor2026*"
}
```

Copy the `"token"` value returned in the `200 OK` JSON response.

### 2. Authorize with JWT Bearer 🔒
1. Click the **`Authorize`** button at the top-right of Swagger UI (or the Authentication panel in Scalar).
2. Paste **only the raw JWT token** into the **Value** input *(do not prepend `Bearer `, Swagger adds it automatically)*.
3. Click **Authorize** → **Close**. All protected endpoints will now include `Authorization: Bearer <token>`.

### 3. Test Bulk Excel Import (`POST /api/v1/enrollments/import`)
1. Authenticate with an **`Employee`** token (`andres.mejia@correo.com`).
2. Expand **`POST /api/v1/enrollments/import`** → **Try it out**.
3. Upload `src/SkillHub.Infrastructure/Resources/Data/SkillHub_Data.xlsx` (automatically targets the `Enrollments` worksheet) or any `.xlsx` workbook containing `ParticipantEmail` and `WorkshopCode` headers.
4. Click **Execute**. Valid rows are persisted even when other rows fail, returning a detailed summary:
   ```json
   {
     "enrolledCount": 70,
     "rejectedCount": 0,
     "totalProcessed": 70,
     "rejectedRows": []
   }
   ```

### 4. Test Role Permissions & PDF Certificate
- Switch to a **`Participant`** token (`jorge.perez@correo.com` / `Skillhub2026*`) via **Authorize → Logout** to test `POST /api/v1/enrollments`, `GET /api/v1/enrollments/mine`, `PATCH /api/v1/enrollments/{id}/cancel`, and `GET /api/v1/enrollments/{id}/certificate` (downloads the official PDF `Constancia de inscripción`).
- Ready-to-run local requests are also included in `src/SkillHub.API/SkillHub.API.http`.

---

## 📏 Domain Business Rules (Enforced Outside Controllers)

All business invariants live in `SkillHub.Domain` and `SkillHub.Application`:
1. **Capacity Control:** Never exceeds `Workshop.Capacity`; only `Active` enrollments count toward occupied spots.
2. **No Duplicate Active Enrollments:** Prevented in domain/application logic and backed by a PostgreSQL partial unique index (`WHERE "Status" = 'Active'`).
3. **No Schedule Overlaps:** A participant cannot enroll in a workshop whose `TimeSlot` overlaps any of their existing `Active` enrollments.
4. **Future Workshops Only:** Workshops can only be created with future `StartDateTime` (`EndDateTime > StartDateTime`), and enrollment is blocked once a workshop has started.
5. **Cancellation & Audit Trail:** Participants can cancel up to **24 hours** before workshop start; Employees can cancel **anytime before start**. Every cancellation frees the spot and records `CancelledBy` and `CancelledAt`.
6. **Re-Enrollment Allowed:** Participants who previously cancelled an enrollment can re-enroll in the same workshop as long as spots remain and all rules pass.

---

## 📡 API Endpoints Summary (`/api/v1`)

| Method | Route | Access | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/auth/register` | Public | Registers a new participant and returns JWT |
| `POST` | `/api/v1/auth/login` | Public | Signs in and returns JWT token + role |
| `GET` | `/api/v1/workshops` | Public | Lists future workshops with calculated available spots |
| `GET` | `/api/v1/workshops/{id}` | Public | Workshop detail with calculated available spots |
| `POST` | `/api/v1/workshops` | `Employee` | Publishes a new future workshop by instructor email |
| `GET` | `/api/v1/workshops/{id}/enrollments` | `Employee` | Lists enrollments of a workshop |
| `POST` | `/api/v1/enrollments` | `Participant` | Enrolls authenticated participant in a workshop |
| `GET` | `/api/v1/enrollments/mine` | `Participant` | Lists authenticated participant's own enrollments |
| `PATCH` | `/api/v1/enrollments/{id}/cancel` | Owner `Participant` or `Employee` | Cancels enrollment (Participant ≥ 24h before; Employee anytime before start) |
| `POST` | `/api/v1/enrollments/import` | `Employee` | Bulk imports `Enrollments` sheet from `.xlsx` file |
| `GET` | `/api/v1/enrollments/{id}/certificate` | Owner `Participant` or `Employee` | Downloads PDF enrollment certificate (active enrollments only) |
| `GET` | `/api/v1/reports/top-workshops` | `Employee` | Lists workshops ordered descending by active enrollments |

---

## 👤 Author

- **Full Name:** Jose Diaz
- **Clan:** Esthersita
- **Email:** josedelcarmen_diaz@outlook.com
- **Identity Document:** 1045755521
