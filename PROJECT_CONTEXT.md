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

### Sprint 8.10 – Jobs Module Stabilization
- Created reusable [JobStatusChipComponent](file:///C:/Projects/RecruitIQ/recruitiq-ui/src/app/features/jobs/components/job-status-chip/job-status-chip.component.ts) to centralize status chip rendering.
- Refactored job components by moving list views under `pages/jobs-list/` directory.
- Compiled both solutions cleanly with zero errors and warnings.

### Sprint 8.11 – Company Profile Settings
- Created a highly reusable standalone [ImageUploadComponent](file:///C:/Projects/RecruitIQ/recruitiq-ui/src/app/shared/components/image-upload/image-upload.component.ts) supporting drag-and-drop, format validation, size limit checks, Replace, and Remove overlays.
- Added `RowVersion` concurrency checks to backend settings query/update handlers and mapped optimistic concurrency conflict (`409`) dialog reload triggers.
- Wired a MediatR `DeleteCompanyLogoCommand` and mapped the backend `DELETE /api/company-settings/logo` endpoint to remove logo files.
- Built the reactive `CompanySettingsPageComponent` connecting Theme, Timezone, Default Interview Duration, and Allowed Email Domain settings, while displaying Company Name and Subdomain as read-only metadata rows utilizing `DetailRowComponent`.
- Integrated `UnsavedChangesGuard` router navigation protectors.
- Verified zero errors and zero warnings on both `dotnet build` and `npx ng build`.

### Sprint 8.12 – Department Management
- Implemented backend CRUD commands and handlers (`CreateDepartmentCommand`, `UpdateDepartmentCommand`, `DeleteDepartmentCommand`) and exposed their endpoints on `DepartmentsController.cs`.
- Implemented **Active Jobs Guard** preventing deletion of departments with active jobs, returning `DepartmentHasActiveJobs` and mapping it to a friendly message.
- Implemented server-side search by adding the `Search` parameter to `GetDepartmentsQuery` and handler.
- Created standalone `DepartmentFormDialogComponent` modal form managing both Create and Edit states.
- Created `DepartmentsPageComponent` rendering responsive department cards (desktop 3-4 columns, tablet 2, mobile 1) with standardized metadata footer (Created Date, Last Updated Date), loading skeletons, and empty illustration states.
- Handled concurrency conflicts (409) using `ConcurrencyConflictDialogComponent` reload hooks.

### Sprint 9.0 – Candidate Management Architecture
- Designed core entity hierarchies: `Candidate`, `CandidateExperience`, `CandidateEducation`, `CandidateCertification`, `CandidateSkill`, and `Resume`.
- Composed the `CandidateSummaryResponse` DTO out of nested DTO summaries (`PersonSummary`, `ContactSummary`, `StatusSummary`, `ResumeSummary`) to enable data reuse across Cards, Applications, and Interviews.
- Configured ATS workflows using status enums (`New`, `Available`, `Shortlisted`, `Interviewing`, `Offered`, `Hired`, `Rejected`, `Inactive`).
- Split Sprint 9 roadmap into independent milestones (Sprint 9.1 through Sprint 9.9) for easier implementation.

### Sprint 9.1 – Candidate List Infrastructure
- Created `CandidateStatus` enum and expanded `Candidate` and `Resume` domain entities and database configurations with `Title`, `Status`, `YearsOfExperience`, `ParserVersion`, and `IsPrimary`.
- Implemented `GetCandidatesQuery` and `GetCandidatesQueryHandler` under `Application/Features/Candidates/GetCandidates/` projecting data into structured composed DTOs, and exposed the `GET /api/candidates` endpoint inside `CandidatesController.cs`.
- Exposed `PagedResponse<T>` globally inside the core `api-response.model.ts` and created `CandidateApiService` and `CandidateStore` Signal store.
- Built `CandidatesListPageComponent` rendering a card/list hybrid layout grid (desktop 3-4 columns, tablet 2, mobile 1) with Teams-style initial avatars, contact actions (Copy Email / Open LinkedIn), skill chips, experience years, status badges, skeletons, and empty state illustrations.
- Registered Candidates lazy-loaded routing inside `routes.ts` and `app.routes.ts` and enabled the navigation item inside `navigation.service.ts`.

### Sprint 9.2 – Candidate Details Page
- Implemented backend query `GetCandidateByIdQuery` and handler returning a complete candidate profile (eager loading experiences, educations, certifications, and skills), and exposed `GET /api/candidates/{id}` on `CandidatesController.cs`.
- Created reusable `EntityHeaderComponent` inside `src/app/shared/components/entity-header/` that wraps avatar, title, subtitle, status chip projection, and action button projections.
- Built responsive 2-column details workspace `CandidateDetailsPageComponent` using dynamic Breadcrumbs updates (`Candidates > [John Smith]`), contact info card, resume summary card (with download details, upload date, uploaded by, and parser version), candidate statistics card (Applications, Interviews, Member Since, Last Updated), and tabbed right column (Overview list cards, Applications placeholder, Activity Timeline placeholder, AI screening reserved layouts).
- Linked candidate list page card name headers to details pages using routerLinks.

### Sprint 9.3 – Create Candidate Page
- Created `CreateCandidateRequest` DTO and implemented `CreateCandidateCommand` + handler (tenant-isolated email uniqueness check, default status `New`, and logs "Candidate Created" activity).
- Added FluentValidation in `CreateCandidateCommandValidator` checking name bounds, emails, valid LinkedIn URLs, and experience parameters. Expose POST endpoint `/api/candidates`.
- Exposes `CreateCandidateRequestDto` in `candidate.models.ts` and `createCandidate` inside `CandidateApiService`.
- Created reusable `CandidateFormComponent` handling basic info, contact details, professional, and future resume info fields.
- Implemented `CreateCandidatePageComponent` utilizing `unsavedChangesGuard` (using the component's dirty form state), custom validation UX routines (scroll to invalid controls, disables duplicate clicks), and sticky action bar footer.
- Configured routes and mapped list page navigation links to the new create candidate workspace.

### Sprint 9.4 – Edit Candidate Workspace
- Created `UpdateCandidateRequest` DTO and implemented `UpdateCandidateCommand` + handler (tenant-isolated, tracked load, email uniqueness check, updates change log, sets RowVersion in EF change tracker). Expose PUT endpoint `/api/candidates/{id}` catching `DbUpdateConcurrencyException` to return HTTP 409 Conflict.
- Mapped `UpdateCandidateRequestDto` in `candidate.models.ts` and `updateCandidate` inside `CandidateApiService`.
- Reused `CandidateFormComponent` to support status selection in Edit Mode.
- Implemented `EditCandidatePageComponent` resolving page loaders/errors, handling `unsavedChangesGuard`, catching HTTP 409 conflict returns to open `ConcurrencyConflictDialogComponent`, and updating the form's `RowVersion` and pristine state upon success.
- Enabled Edit button links in candidate details workspace.

### Sprint 9.5 – Resume Upload & Resume Management
- Extended domain entity [Resume.cs](file:///C:/Projects/RecruitIQ/src/RecruitIQ.Domain/Entities/Resume.cs) with properties `OriginalFileName`, `FileSize`, and `MimeType`. Mapped columns inside `ResumeConfiguration.cs` and applied EF Core schema migration `UpdateResumeSchema` to the database.
- Created `UploadResumeCommand`, `UploadResumeCommandHandler`, and validators in `src/RecruitIQ.Application/Features/Candidates/UploadResume/`. Validates file size (max 10MB) and allowed extensions/mimes (`.pdf`, `.doc`, `.docx`). Prevents duplicate uploads of the same file name, stores files via `IFileStorageService`, and logs "Resume Uploaded" activities.
- Created `DeleteResumeCommand` and handler soft-deleting database records while physically removing files from local disk and automatically reassigning the primary flag to other active resumes if needed.
- Created `SetPrimaryResumeCommand` and handler setting the target resume as primary while clearing other resumes of the candidate.
- Updated `CandidateDetailsResponse` and query handler mapping `IReadOnlyList<CandidateResumeSummary> Resumes`.
- Exposed endpoints in `CandidatesController.cs`:
  - `POST /api/candidates/{id}/resume`
  - `DELETE /api/candidates/{id}/resume/{resumeId}`
  - `PUT /api/candidates/{id}/resume/{resumeId}/primary`
  - `GET /api/candidates/{id}/resume/{resumeId}/download` (streams file content securely under tenant isolation context)
- Added `CandidateResumeSummary` interface and updated details payload inside `candidate.models.ts`.
- Implemented `ResumeApiService` and `ResumeStore` managing upload progress events.
- Created reusable `ResumeUploadComponent` with drag and drop, file size/extension validations, and progress bars.
- Integrated the new **Resume Management** card inside candidate details view to display active resumes, badges, download/set primary/delete triggers, and inline resume uploads.

### Sprint 9.6 – Candidate Activity Timeline
- Created `CandidateTimelineItemResponse` DTO holding timeline properties.
- Implemented `GetCandidateTimelineQuery` and `GetCandidateTimelineQueryHandler` in `src/RecruitIQ.Application/Features/Candidates/GetCandidateTimeline/`. Filters by current tenant and candidate context, eager-loads user performer, and splits Action log details on colons for cleaner presentation.
- Mapped material icons (`person_add`, `cloud_upload`, `delete`, `star`, `change_circle`, `edit`) and colors dynamically based on activity content.
- Exposed endpoint `GET /api/candidates/{id}/timeline` on `CandidatesController.cs`.
- Mapped `CandidateTimelineItemResponse` interface in `candidate.models.ts` and implemented `CandidateTimelineApiService` and `CandidateTimelineStore`.
- Created `CandidateTimelineComponent` containing shimmering skeletons, retry panels, empty states, and vertical connecting track line.
- Embedded the timeline component inside the "Activity Timeline" tab of candidate details workspace.

### Sprint 9.7 – Candidate Lifecycle Workflow
- Created `CandidateLifecycleService` and interface validating workflow transitions (e.g. `New` -> `Available`/`Inactive`, etc.). Registered it in `DependencyInjection.cs`.
- Created command `ChangeCandidateStatusCommand` and handler (tenant isolation, check existence, lifecycle validation, optimistic concurrency, and audit logging).
- Exposed PATCH `/api/candidates/{id}/status` in `CandidatesController` catching database concurrency exceptions to return HTTP 409 Conflict.
- Updated core `ApiService` with generic `patch` requests and exposed `changeStatus` inside `CandidateApiService`.
- Declared `CandidateStatus` enum and name mappings in `candidate.models.ts` and mapped `changeStatus` to `CandidateStore`.
- Mapped `InvalidCandidateStatusTransition` to business error translations.
- Created `ChangeCandidateStatusDialogComponent` with reactive controls, computed transition filtering, and static confirmation dialog triggers for `Hired` and `Rejected` transitions.
- Integrated the **Recruitment Workflow** card inside candidate details view to display current pipeline stage, status explanations, allowed next stages, and trigger the status dialog flow.

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
