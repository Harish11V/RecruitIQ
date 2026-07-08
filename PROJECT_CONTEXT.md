# RecruitIQ Project Context & Sprint History

RecruitIQ is a production-style enterprise multi-tenant Applicant Tracking System (ATS). This document provides a complete guide to the project architecture, database schemas, API structures, frontend code layouts, completed sprints, and the necessary context to understand or bootstrap the application from scratch.

---

## Project Reference Map
- **Backend Solution**: [RecruitIQ.slnx](file:///C:/Projects/RecruitIQ/RecruitIQ.slnx)
- **Frontend App Root**: [recruitiq-ui](file:///C:/Projects/RecruitIQ/recruitiq-ui)

---

## 1. Core Technology Stack

### Backend (.NET 8 Core Web API)
- **Architecture**: Clean Architecture / CQRS (MediatR) pattern.
- **Database**: Entity Framework Core. SQL Server in production; SQLite (in-memory) for integration testing.
- **Value Objects**: Immutable custom record types (`EmailAddress`, `PhoneNumber`, `Address`) to prevent primitive obsession.
- **Auditing & Mutability**: Interceptors handle soft-deletes (`IsDeleted = true`) and auto-stamp metadata (`CreatedAt`, `CreatedBy`, etc.).
- **Security**: BCrypt hashing, JWT tokens, multi-tenant claims matching, custom policies (`RequireCompanyAdmin`, `RequireRecruiter`).

### Frontend (Angular 20 Standalone)
- **Architecture**: Feature-Based Architecture (SaaS design tokens, layout shell, shared UI elements, feature subfolders).
- **Styling**: SCSS, Material 3 theme configurations, custom CSS overrides, and Inter/Material Symbols typography.
- **State Management**: Zero-dependency class-based Signal Stores orchestrating loading indicators, pagination, and filter debounces.
- **Security**: Functional HttpClient interceptors adding bearer authorization headers, handling 401 Unauthorized status codes, queuing requests, and refreshing tokens.

---

## 2. Database Schema (Domain Entities)

The system consists of 25 core entities mapped in EF Core:

| Entity Name | Primary Key | Key Foreign Mappings | Purpose / Description |
| :--- | :--- | :--- | :--- |
| **Company** | Guid | - | Master tenant entity. Represents the customer organization. |
| **User** | Guid | CompanyId | User accounts scoped under a specific company tenant. |
| **Role** | Guid | - | Global security roles (e.g. `CompanyAdmin`, `Recruiter`). |
| **UserRole** | Guid | UserId, RoleId | Join table mapping users to security roles. |
| **Department** | Guid | CompanyId | Departments within a company (e.g., Engineering, Sales). |
| **Job** | Guid | CompanyId, DepartmentId, HiringManagerId | Job postings (Draft, Published, Closed, Archived). |
| **Skill** | Guid | - | Global directory of skills (e.g. C#, Angular, SQL). |
| **JobSkill** | Guid | JobId, SkillId | Required skills for a job posting. |
| **Candidate** | Guid | CompanyId | Candidate profiles. |
| **Application** | Guid | CompanyId, JobId, CandidateId | Job applications. |
| **ApplicationStage** | Guid | CompanyId, JobId | Custom application hiring pipelines. |
| **Interview** | Guid | CompanyId, ApplicationId, HostId | Scheduled interviews. |
| **InterviewFeedback** | Guid | CompanyId, InterviewId, InterviewerId | Interview ratings and notes. |
| **ActivityLog** | Guid | CompanyId, UserId | General audit log of events. |

---

## 3. Sprint History & Milestones Completed

### Sprint 7.1 – Decoupled Domain Models & Value Objects
- Created value objects [EmailAddress.cs](file:///C:/Projects/RecruitIQ/src/RecruitIQ.Domain/ValueObjects/EmailAddress.cs), [PhoneNumber.cs](file:///C:/Projects/RecruitIQ/src/RecruitIQ.Domain/ValueObjects/PhoneNumber.cs), and [Address.cs](file:///C:/Projects/RecruitIQ/src/RecruitIQ.Domain/ValueObjects/Address.cs).
- Declared [IRecruitIQDbContext.cs](file:///C:/Projects/RecruitIQ/src/RecruitIQ.Application/Common/Interfaces/IRecruitIQDbContext.cs) with native collections only, preventing Persistence dependencies in Application project.

### Sprint 7.2 – Concrete DBContext & Concurrency Token Mapping
- Implemented [RecruitIQDbContext.cs](file:///C:/Projects/RecruitIQ/src/RecruitIQ.Persistence/DbContext/RecruitIQDbContext.cs).
- Configured optimistic concurrency mapping for `RowVersion` columns (mapped to database `rowversion` for SQL Server, and rotated byte arrays for SQLite in-memory).
- Wired multi-tenant and soft-delete global query filters.

### Sprint 7.3 – Configurations & SaveChanges Interceptors
- Created configurations inside `Persistence/Configurations/` scoping lengths, formats, indexes, and deletion behaviors.
- Implemented [AuditEntityInterceptor.cs](file:///C:/Projects/RecruitIQ/src/RecruitIQ.Persistence/Interceptors/AuditEntityInterceptor.cs) and [SoftDeleteInterceptor.cs](file:///C:/Projects/RecruitIQ/src/RecruitIQ.Persistence/Interceptors/SoftDeleteInterceptor.cs).

### Sprint 7.4 – Security & Authentication Endpoints
- Implemented JWT token generation and BCrypt password encryption.
- Built `AuthController` mapping register, login, logout, refresh, invite, and password reset endpoints.

### Sprint 7.5 – Company Settings Configuration
- Created company details update endpoints supporting local storage files logo uploads with magic-byte validation.

### Sprint 7.6 – Job Management Features (Draft, Publish, Archive)
- Implemented MediatR commands, validators, and endpoints for:
  - `CreateJobCommand`: Generates sequential `JobCode` (`JOB-YYYY-0001`), creates drafts.
  - `UpdateJobCommand`: Updates values checking for concurrency tokens.
  - `PublishJobCommand`: Enforces department/hiring manager validation.
  - `ArchiveJobCommand`: Restricts status changes.

### Sprint 7.7 – Delete Job (Soft Delete)
- Implemented logical deletion (flagging `IsDeleted = true`), verifying that soft-deleted entities are automatically excluded from lists and queries.

---

### Sprint 8.1 – Frontend Scaffolding
- Scaffolder Angular 20 Standalone workspace inside `recruitiq-ui/`.
- Configured `app.config.ts` composition root, lazy routes, auth/guest guards, and HTTP client interceptors.

### Sprint 8.2 – Design System Tokens & UI Foundation
- Configured typography (Inter, Material Symbols Rounded) and spacing layout custom tokens in `styles/tokens/`.
- Implemented custom Angular Material M3 overrides.
- Created standalone foundation components: `PageContainer`, `SectionHeader`, `AppCard`, and `StatCard`.

### Sprint 8.3 – JWT Authentication & Login Page
- Implemented `TokenService`, `StorageService` (remember-me routing), and `AuthService`.
- Built `authInterceptor` managing 401 capture and request token refresh queuing.
- Created Premium Login page with Caps Lock warnings and throttled background mouse radial glows.

### Sprint 8.4 – Admin Shell & Navigation
- Implemented layout drawer viewport sizing (`LayoutService` mapping cdk breakpoints).
- Collapsible sidebar supports 280px / 72px width shifts, tooltips, and dynamic categorizations.
- Topbar implements Breadcrumbs, readonly search bar, and profile dropdown trigger with `UserAvatarComponent`.

### Sprint 8.5A & 8.5B – Job Listing Workspace & Polish
- Implemented model enums, `JobsApiService` parameter mapping, and `JobStore` Signals state manager.
- Displays four stats cards querying totals in parallel.
- Built responsive Material Table, debounced search filters, status chips, and three-dot actions menu.

### Sprint 8.6 – Create Job Experience
- Created live backend endpoints for fetching departments dynamically: `GetDepartmentsQuery` and `DepartmentsController`.
- Created functional generic `unsavedChangesGuard` protecting forms against unintended navigations.
- Built `CreateJobPageComponent` with 6 detailed sections, responsive grid columns, min-date date picker, dynamic Material Chips skill inputs, conditional salary ranges, validation summary sticky action bar, and inline error mappers.

### Sprint 8.7 – Job Details Workspace
- Added dynamic `Location` parameter to C# `JobDetailsResponse` contract and database select projections.
- Created reusable `DetailRowComponent` (supporting multiline values, optional icons, and labels) for unified workspace metadata representation.
- Built `JobDetailsPageComponent` featuring responsive columns, USD currency-formatted salary ranges, wrapped skills chips, audit histories, retry-enabled 404 handler states, and a visual Entity Header layout convention.

### Sprint 8.8 – Edit Job Workspace
- Added `RowVersion` concurrency token parameter to C# `JobDetailsResponse` contract and query handlers.
- Refactored forms by extracting a reusable `<app-job-form>` component utilized by both Create and Edit workflows.
- Created generic reusable `ConcurrencyConflictDialogComponent` mapping optimistic concurrency conflicts.
- Built `EditJobPageComponent` loading active details, forwarding updates with unaltered `rowVersion` tokens, clearing form dirty flags on success, and trapping `409 Conflict` server responses to prompt concurrency reloads.

### Sprint 8.9 – Job Lifecycle Actions
- Project `RowVersion` inside C# `JobSummaryResponse` EF Core select queries to support list-view concurrency checks.
- Created generic reusable `ConfirmationDialogComponent` supporting custom buttons color configurations and dynamic async `confirmAction` parameter subscriptions to control dialog-level loading indicators.
- Created application-wide `business-error-mapper.ts` converting business error strings into localized recruiter messages.
- Created centralized `JobActionService` executing Publish, Archive, and Delete workflows, clearing concurrency traps, and displaying snackbar results.
- Integrated lifecycle actions inside the list-view actions menu and details-page header toolbar, showing only permitted commands based on the entity status (Draft -> Publish, Published -> Archive, Archived -> Delete).

---

## 4. How to Bootstrap/Run the Project

### Database Migrations (Backend)
To run migrations and start the backend API:
```bash
# Navigate to the API project
cd src/RecruitIQ.API

# Run EF Core migrations (ensure SQL Server localdb or database is running)
dotnet ef database update --project ../RecruitIQ.Persistence --startup-project .

# Run the API
dotnet run
```
The API is configured to listen on localhost (default Swagger endpoint at `https://localhost:7085/swagger`).

### Frontend Startup
To start the Angular dev server:
```bash
# Navigate to UI directory
cd recruitiq-ui

# Install dependencies (if not present)
npm install

# Start the dev server
npm start
```
Dev server starts at `http://localhost:4200/`.
