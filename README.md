# SkillHub_back — Workshop & Enrollment Management API

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C# 13](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16%20Alpine-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker Compose](https://img.shields.io/badge/Docker%20Compose-Ready-2496ED?logo=docker&logoColor=white)](docker-compose.yml)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-00599E)](docs/SUSTENTACION.md)
[![Tests](https://img.shields.io/badge/Tests-88%20Passing-brightgreen)](tests/SkillHub.Tests)
[![Swagger](https://img.shields.io/badge/Swagger-OpenAPI%20v1-85EA2D?logo=swagger&logoColor=black)](http://localhost:5142/swagger)
[![Scalar](https://img.shields.io/badge/Scalar-API%20Reference-111827?logo=scalar&logoColor=white)](http://localhost:5142/scalar/v1)
[![PDF](https://img.shields.io/badge/iText-9.7.0%20(PDF%20%26%20Watermark)-E02424)](https://itextpdf.com/)
[![Excel](https://img.shields.io/badge/EPPlus-8.0.2%20(.xlsx%20Branding)-107C41?logo=microsoftexcel&logoColor=white)](https://epplussoftware.com/)

---

## 📖 Project Overview

**SkillHub** is a modern training center that offers short, in-person practical workshops across diverse creative and professional disciplines: photography, culinary arts, public speaking, graphic design, music production, digital marketing, and entrepreneurship.

Historically, registrations were handled through disparate forms and manual spreadsheets, resulting in frequent overbooking, duplicate active enrollments, and uncoordinated schedule clashes. **SkillHub API** resolves these challenges with a production-grade **Clean Architecture** backend in **.NET 10**:

- **Visitors:** Browse upcoming workshops with real-time calculated available capacity without authentication.
- **Participants:** Self-register, log in, enroll in workshops under strict domain invariant rules (no capacity overflow, no duplicates, no time conflicts, future dates only), view and cancel their own enrollments (up to 24 hours prior to workshop start), re-enroll when spots are freed, and download an official **PDF Enrollment Certificate** (`Constancia de inscripción`) with embedded brand assets and a **`logo1.png` background watermark**.
- **Employees (Instructors):** Sign in on first startup, publish upcoming workshops, inspect attendee rosters, download branded Excel import templates, bulk-import enrollments from `.xlsx` spreadsheets (saving valid rows while reporting failed ones), cancel participant enrollments anytime before workshop start, download any participant's enrollment certificate, and export top-enrolled workshops reports in both JSON and executive `.xlsx` formats.

---

## 🛠️ Technology Stack

| Layer / Capability | Technology & Version | Purpose |
| :--- | :--- | :--- |
| **Runtime & Language** | .NET 10 SDK / C# 13 / ASP.NET Core Web API | Modern cross-platform runtime and latest language features |
| **Architectural Style** | Clean Architecture (DDD & SOLID Principles) | Strict layer boundaries: `Domain` → `Application` ← `Infrastructure` / `API` |
| **Database & ORM** | PostgreSQL 14+ / EF Core 10 (`Npgsql.EntityFrameworkCore.PostgreSQL`) | Relational persistence, Code-First migrations, partial unique indexes |
| **Identity & Security** | ASP.NET Core Identity + JWT Bearer (`HMAC-SHA256`) | Role-based authorization (`Employee`, `Participant`) and ownership guards |
| **Input Validation** | FluentValidation 11 + Domain Value Objects | Request DTO validation decoupled from controllers |
| **Excel Processing** | EPPlus 8 (Non-commercial license) | Seeder parsing, flexible bulk import (`rows 1..15` header detection), and branded workbooks |
| **PDF Generation** | iText 9.7.0 (`itext`, `itext.bouncy-castle-connector`) | Single-page Letter enrollment certificate with `logo1.png` watermark and 2D QR |
| **Error Standardization** | Centralized `GlobalExceptionHandler` (`IExceptionHandler`) | RFC 7807 `ProblemDetails` with safe payloads (no internal DB leaks) |
| **API Documentation** | Swagger UI (`/swagger`) & Scalar (`/scalar/v1`) | Interactive OpenAPI reference with JWT authentication and exact `01..12` ordering |
| **Automated Testing** | xUnit + Moq (85 automated tests) | Comprehensive unit, domain invariant, and integration test suite |

---

## 🏛️ Clean Architecture Design

```
                     ┌────────────────────────────────┐
                     │          SkillHub.API          │
                     │  (Controllers, Middleware,     │
                     │   Swagger/Scalar, Program.cs)  │
                     └───────────────┬────────────────┘
                                     │ depends on
                                     ▼
                     ┌────────────────────────────────┐
                     │      SkillHub.Application      │
                     │  (Services, DTOs, Validators,  │
                     │   Service & Document Interfaces)│
                     └───────────────┬────────────────┘
                                     │ depends on
                                     ▼
                     ┌────────────────────────────────┐
                     │        SkillHub.Domain         │
                     │  (Entities, Value Objects,     │
                     │   Invariants, Domain Events)   │
                     └────────────────────────────────┘
                                     ▲
                                     │ implements interfaces of
                     ┌───────────────┴────────────────┐
                     │    SkillHub.Infrastructure     │
                     │  (AppDbContext, Repositories,  │
                     │   EF Migrations, EPPlus Excel, │
                     │   iText PDF, BrandAssetResolver)│
                     └────────────────────────────────┘
```

### Solution Structure
```text
SkillHub/
├── README.md                             # Project documentation, credentials, and author
├── SkillHub.sln                          # .NET solution file
├── database/
│   └── skillhub_dump.sql                 # Complete PostgreSQL schema & seeded data dump
├── src/
│   ├── SkillHub.Domain/                  # Core Layer (Zero external dependencies)
│   │   ├── Abstractions/                 # Repository & UnitOfWork contracts
│   │   ├── Common/                       # Entity, AggregateRoot, ValueObject, IDomainEvent
│   │   ├── Entities/                     # Instructor, Participant, Workshop, Enrollment
│   │   ├── Enums/                        # EnrollmentStatus, RoleNames
│   │   ├── Exceptions/                   # CapacityExceeded, DuplicateEnrollment, ScheduleConflict
│   │   ├── Events/                       # Domain events (WorkshopCreated, EnrollmentCreated/Cancelled)
│   │   └── ValueObjects/                 # EmailAddress, WorkshopCode, TimeSlot
│   │
│   ├── SkillHub.Application/             # Use-Case Layer
│   │   ├── DTOs/                         # Request, Response, and Report records
│   │   ├── Exceptions/                   # NotFound, Conflict, ForbiddenAccess, Validation
│   │   ├── Interfaces/                   # Business services, JWT, Excel & PDF generation
│   │   ├── Services/                     # WorkshopService, EnrollmentService, ReportService
│   │   ├── Validators/                   # FluentValidation request validators
│   │   └── DependencyInjection.cs
│   │
│   ├── SkillHub.Infrastructure/          # Technology & Persistence Layer
│   │   ├── Persistence/
│   │   │   ├── Configurations/           # EF Core configurations (partial unique active index)
│   │   │   ├── Migrations/               # EF Core Code-First migrations
│   │   │   ├── Repositories/             # EF Core repositories & UnitOfWork
│   │   │   ├── Seeders/                  # DbSeeder: auto-migrates and seeds initial data
│   │   │   └── AppDbContext.cs           # EF Core context extending IdentityDbContext
│   │   ├── Resources/
│   │   │   ├── Assets/                   # logo1.png, skillhub-logo-horizontal.png, emblem
│   │   │   └── Data/                     # SkillHub_Data.xlsx source workbook
│   │   ├── Services/                     # AuthService, JwtTokenService, ExcelImportService,
│   │   │                                 # CertificatePdfService, ExcelReportService, BrandAssetResolver
│   │   └── DependencyInjection.cs
│   │
│   └── SkillHub.API/                     # Presentation Layer (Thin Controllers)
│       ├── Controllers/                  # Auth, Workshops, Enrollments, Reports (/api/v1/...)
│       ├── Middleware/                   # GlobalExceptionHandler.cs (RFC 7807)
│       ├── OpenApi/                      # Swagger/Scalar ordering filters & example providers
│       ├── appsettings.json              # Connection strings and JWT configuration
│       ├── Program.cs                    # Dependency injection, CORS, Auth, Scalar & Swagger
│       └── SkillHub.API.http             # Pre-configured HTTP requests for testing
│
└── tests/
    └── SkillHub.Tests/                   # 85 automated xUnit & Moq tests
        ├── Domain/                       # Aggregate invariants and value object unit tests
        ├── Application/                  # Use-case, cancellation window, and validator tests
        ├── Infrastructure/               # PDF layout, logo watermark, Excel roundtrip tests
        └── API/                          # Security, role authorization, and exception mapping tests
```

---

## 🎨 Official SkillHub Branding & UI Features

### 1. PDF Enrollment Certificate (`Constancia de inscripción`)
- **Route:** `GET /api/v1/enrollments/{id}/certificate`
- **Access:** Owner `Participant` or any `Employee` (active enrollments only).
- **Official Institutional Letterhead Format:** Designed strictly as an authentic academic enrollment certificate (*Constancia de Inscripción*):
  - **Official Academic Letterhead:** SkillHub horizontal logo ([`skillhub-logo-horizontal.png`](src/SkillHub.Infrastructure/Resources/Assets/skillhub-logo-horizontal.png)), institutional address, Academic Admissions & Records Division contact, and official radicado tracking number (`SH-XXXXXXXX`).
  - **Formal Attestation Preamble:** Authoritative certification statement (*"HACE CONSTAR QUE..."*) certifying the participant's full name, registered email, and confirmed active seat.
  - **Technical Workshop Summary:** Clean executive summary card highlighting workshop title, code badge, lead instructor, schedule (UTC), lab modality, and guaranteed active seat status.
  - **Background Watermark (`logo1.png`):** Official SkillHub emblem rendered with subtle 6% opacity (`0.06f`) centered behind text.
  - **Official Seals & Signatures:** Real-time 2D QR verification code (`BarcodeQRCode`), cryptographic SHA-256 validation digest, institutional validation seal, and dual signature lines (Academic Coordination & Lead Instructor).
  - **Strict 1-Page Guarantee:** Engineered and automated-test-asserted to fit strictly on **1 single Letter page**.

### 2. Branded Excel Workbooks (`.xlsx`)
- **Bulk Enrollment Import Template (`GET /api/v1/enrollments/import/template`):**
  - **Sheet 1 (`Enrollments`):** SkillHub horizontal logo, frozen header panes, styled sample data, and formatting ready for bulk participant uploads.
  - **Sheet 2 (`Available Workshops`):** Live catalog of upcoming workshops with current capacity, available spots, and instructor details.
- **Top Workshops Report (`GET /api/v1/reports/top-workshops/excel`):**
  - **Sheet 1 (`Top Workshops`):** Embedded logo, corporate palette, rank ordering, status badges (`HIGH DEMAND`, `FILLING FAST`, `OPEN SPOTS`), AutoFilter, and Excel `SUM` formulas.
  - **Sheet 2 (`Executive Summary`):** Executive dashboard with KPI summary cards (Total Workshops, Total Active Enrollments, Average Occupancy, Total Seats).
- **Flexible Excel Import:** [`ExcelImportService.cs`](src/SkillHub.Infrastructure/Services/ExcelImportService.cs) dynamically scans rows `1..15` for required headers (`ParticipantEmail` and `WorkshopCode`), making it compatible with both simple spreadsheets and logo-branded templates.

---

## 📡 API Endpoints Summary (Exact Specification Sequence)

Endpoints appear in the exact top-to-bottom sequence of **Page 3 of the official evaluation document**:

| Order | Tag | Method | Route | Access | Description |
| :---: | :--- | :---: | :--- | :--- | :--- |
| **01** | `01. Authentication` | `POST` | `/api/v1/auth/register` | Public | Registers a new participant account and returns JWT token |
| **02** | `01. Authentication` | `POST` | `/api/v1/auth/login` | Public | Signs in an existing user (Employee or Participant) and returns JWT |
| **03** | `02. Workshops` | `GET` | `/api/v1/workshops` | Public | Lists upcoming workshops with real-time calculated available spots |
| **04** | `02. Workshops` | `GET` | `/api/v1/workshops/{id}` | Public | Retrieves detailed workshop profile with available spots |
| **05** | `02. Workshops` | `POST` | `/api/v1/workshops` | `Employee` | Publishes a new future workshop assigned to an instructor |
| **06** | `02. Workshops` | `GET` | `/api/v1/workshops/{id}/enrollments` | `Employee` | Lists all enrollments (Active & Cancelled) for a workshop |
| **07** | `03. Enrollments` | `POST` | `/api/v1/enrollments` | `Participant` | Enrolls the authenticated participant in a future workshop |
| **08** | `03. Enrollments` | `GET` | `/api/v1/enrollments/mine` | `Participant` | Lists the authenticated participant's own enrollment history |
| **09** | `03. Enrollments` | `PATCH` | `/api/v1/enrollments/{id}/cancel` | Owner `Participant` / `Employee` | Cancels enrollment (Participant ≥ 24h before; Employee anytime before start) |
| **10** | `03. Enrollments` | `POST` | `/api/v1/enrollments/import` | `Employee` | Bulk-imports enrollments from an uploaded `.xlsx` file |
| **10b**| `03. Enrollments` | `GET` | `/api/v1/enrollments/import/template` | `Employee` | Downloads the branded bulk-enrollment Excel template with catalog |
| **11** | `03. Enrollments` | `GET` | `/api/v1/enrollments/{id}/certificate` | Owner `Participant` / `Employee` | Downloads official PDF enrollment certificate with `logo1.png` watermark |
| **12** | `04. Reports` | `GET` | `/api/v1/reports/top-workshops` | `Employee` | Consults workshops sorted descending by active enrollments (JSON) |
| **12b**| `04. Reports` | `GET` | `/api/v1/reports/top-workshops/excel` | `Employee` | Exports the Top Workshops report as a branded `.xlsx` workbook |

---

## 📏 Domain Business Invariants

All core business rules are strictly encapsulated in `SkillHub.Domain` and orchestrated in `SkillHub.Application`:

1. **Capacity Limit:** An enrollment cannot be created if active enrollments have reached the workshop's `Capacity`. Only `Active` enrollments consume spots; `Cancelled` enrollments free up capacity immediately.
2. **No Duplicate Active Enrollments:** A participant cannot hold more than one active enrollment in the same workshop. Enforced both in domain logic and at the database level via a PostgreSQL partial unique index:
   ```sql
   CREATE UNIQUE INDEX "IX_enrollments_ParticipantId_WorkshopId_Active"
   ON "enrollments" ("ParticipantId", "WorkshopId")
   WHERE "Status" = 'Active';
   ```
3. **No Schedule Overlaps:** A participant cannot enroll in a workshop whose start and end dates conflict with another workshop where they already have an `Active` enrollment.
4. **Future Workshops Only:** Workshops can only be scheduled in the future (`StartDateTime > UtcNow` and `EndDateTime > StartDateTime`). Enrollments cannot be created for workshops that have already begun.
5. **Cancellation Window & Audit Trail:**
   - **Participants** can cancel their own enrollment up to **24 hours** before workshop commencement.
   - **Employees** can cancel any participant's enrollment without the 24-hour restriction, provided the workshop has not started.
   - Every cancellation frees the reserved spot and records `CancelledBy` (email/identity) and `CancelledAtUtc`.
6. **Re-Enrollment Permitted:** A participant who previously cancelled an enrollment may re-enroll in the same workshop if capacity is available and no other restrictions are violated.
7. **Bulk Excel Processing:** Each row is validated individually against all domain rules. Valid rows are persisted even if other rows fail. The response details the number of successful enrollments and provides row numbers and rejection reasons for failed rows.

---

## ⚙️ Setup & Execution Guide

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 14+](https://www.postgresql.org/) running locally or in a container

### 1. Clone the Repository
```bash
git clone https://github.com/jose2520/SkillHub.git
cd SkillHub
```

### 2. Configure PostgreSQL Connection
Update `ConnectionStrings:DefaultConnection` inside `src/SkillHub.API/appsettings.json` (or set the environment variable `ConnectionStrings__DefaultConnection`):
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

### 3. Automatic Migration & Initial Seeding
When the API starts, `DbSeeder` automatically executes `Database.MigrateAsync()` (Code-First) and reads `SkillHub_Data.xlsx` to seed:
- **6 Instructors (Employees)** with active Identity accounts and `Employee` role.
- **30 Seeded Participants** with active Identity accounts and `Participant` role.
- **15 Upcoming Workshops** with assigned instructors.

*(Alternative: You may also restore the provided PostgreSQL dump directly: `psql -h localhost -U postgres -d skillhub_db < database/skillhub_dump.sql`)*

### 4. Run Automated Test Suite
```bash
dotnet test SkillHub.sln -c Release --verbosity normal
```
All **85 tests** pass with **0 warnings and 0 errors**.

### 5. Launch the API
```bash
dotnet run --project src/SkillHub.API --launch-profile http
```

Once running, navigate to:
- **Swagger UI:** [http://localhost:5142/swagger](http://localhost:5142/swagger)
- **Scalar API Reference:** [http://localhost:5142/scalar/v1](http://localhost:5142/scalar/v1)
- **Health / Info Root:** [http://localhost:5142/](http://localhost:5142/)

---

## 🔐 Credentials for Testing

All instructor and participant accounts are automatically created and ready to use immediately:

### Employee Accounts (Role: `Employee`)
| Full Name | Email | Password | Specialty |
| :--- | :--- | :--- | :--- |
| **Andrés Felipe Mejía Arango** | `andres.mejia@correo.com` | `Instructor2026*` | Fotografía |
| **Rosa Elena Barrios Polo** | `rosa.barrios@correo.com` | `Instructor2026*` | Cocina |
| **Mauricio Jiménez Cárdenas** | `mauricio.jimenez@correo.com` | `Instructor2026*` | Oratoria |
| **Natalia Herrera Castaño** | `natalia.herrera@correo.com` | `Instructor2026*` | Diseño gráfico |
| **Hernán Darío Quintero Gil** | `hernan.quintero@correo.com` | `Instructor2026*` | Música |
| **Paola Andrea Vargas Mora** | `paola.vargas@correo.com` | `Instructor2026*` | Marketing digital |

### Sample Participant Accounts (Role: `Participant`)
| Full Name | Email | Password |
| :--- | :--- | :--- |
| **Jorge Andrés Pérez Rojas** | `jorge.perez@correo.com` | `Skillhub2026*` |
| **Luz Marina Ospina Ríos** | `luz.ospina@correo.com` | `Skillhub2026*` |

*(New participants can also register directly via `POST /api/v1/auth/register`.)*

---

## 🧪 Testing in Swagger UI & Scalar

1. **Sign In (`POST /api/v1/auth/login`):** Send an Employee or Participant email and password to receive a JWT token.
2. **Authorize:** Click the **Authorize** button in Swagger UI (or Scalar's Authentication field) and paste **only the raw JWT token** (do not type `Bearer`).
3. **Execute Protected Operations:**
   - **As Employee:** Call `POST /api/v1/workshops`, `POST /api/v1/enrollments/import`, `GET /api/v1/enrollments/import/template`, or `GET /api/v1/reports/top-workshops/excel`.
   - **As Participant:** Call `POST /api/v1/enrollments`, `GET /api/v1/enrollments/mine`, `PATCH /api/v1/enrollments/{id}/cancel`, or `GET /api/v1/enrollments/{id}/certificate` to download your PDF certificate.

---

## 👤 Author Information

- **Full Name:** Jose Diaz
- **Clan:** Esthersita
- **Email:** josedelcarmen_diaz@outlook.com
- **Identity Document:** 1045755521
- **Repository:** [https://github.com/jose2520/SkillHub_back.git](https://github.com/jose2520/SkillHub_back.git)
- **Oral Defense Guide:** [docs/SUSTENTACION.md](docs/SUSTENTACION.md)

