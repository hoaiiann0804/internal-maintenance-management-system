# Office Facility Maintenance Management System

> A web-based internal platform for managing office equipment maintenance across departments, technicians, and staff. It centralizes requests, assignment, status tracking, comments, and history into one auditable workflow.

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)]()
[![React 19](https://img.shields.io/badge/React-19-61DAFB)]()
[![SQL Server 2022](https://img.shields.io/badge/SQL%20Server-2022-D83B01)]()
[![Status](https://img.shields.io/badge/status-active-success)]()

## About

Office Facility Maintenance Management System is an internal maintenance platform built for an office environment. It is designed for office assets such as printers, routers, air conditioners, meeting-room devices, and other shared equipment that need structured maintenance handling.

The system helps:

- staff report issues in one place
- managers assign work with better visibility
- technicians update progress in a controlled workflow
- administrators maintain users, departments, and equipment
- the organization keep a clear history of every ticket

## Business Context

Before this system, maintenance requests were typically handled through scattered channels such as phone calls, email, chat tools, or direct messages. That approach makes it easy to miss requests, duplicate work, and lose operational history.

This project replaces that fragmented process with a centralized workflow so the team can:

- capture every request consistently
- track who created, assigned, and resolved each ticket
- connect each maintenance request to a department and an equipment item
- review ticket history and comments later for audit and reporting

## Current Scope

The repository currently contains:

- an ASP.NET Core Web API backend
- a React + Vite frontend
- JWT authentication with refresh tokens
- role-based access control
- department, equipment, user, ticket, ticket attachment, and dashboard modules
- seeded demo data for local development

The frontend currently includes:

- `Login`
- `Dashboard`
- `Tickets`
- `Equipment`
- `Users`
- `Departments`

The UI is built as a complete operational console with API hooks for authentication, ticket lifecycle management, equipment tracking, department administration, file attachments, and analytics.

## Key Features

### Authentication

- JWT login
- current user profile lookup
- password change
- refresh token rotation
- logout via refresh-token revocation

### User Management

- admin-only user directory
- role-aware access
- department assignment
- active/inactive status control
- reset password support
- new Admin accounts are not created from the user-management form; existing Admin accounts can still be maintained

### Department Management

- create, update, read, delete departments
- unique department name protection
- dependency checks before deletion
- `IsMaintenanceTeam` flag to mark specialized maintenance departments

### Equipment Management

- create, update, read, delete equipment
- equipment assigned to a department
- immutable equipment code after creation
- support for office asset tracking

### Maintenance Tickets

- create maintenance requests
- view ticket queue and detail
- assign technicians
- change ticket status through workflow rules
- add comments
- track status history
- store resolution notes and timestamps

### Ticket Attachments & Storage

- presigned file upload flow (S3 / Local storage compatible)
- list attachments per ticket
- preview modal for images, videos, and PDF documents
- secure download link generation
- attachment deletion with role/owner permissions

### Dashboard & Analytics

- real-time KPI overview (tickets by status, active vs maintenance equipment, technician and department metrics)
- interactive status and priority distribution charts
- department equipment breakdown
- role-scoped data filtering

### Theme & UI Polish

- Light and Dark mode toggle with system preference support
- responsive tables, modal dialogs, and status badges

## UI & Features Screenshots

### 1. Dashboard & Analytics
Overview of maintenance tickets, KPI summaries, and equipment status.
![Dashboard UI](./InternalMaintenance.Client/public/screenshots/dashboard.png)
![Dashboard UI](./InternalMaintenance.Client/public/screenshots/dashboard2.png)

### 2. Ticket Management
List of maintenance tickets with color-coded status badges and priority indicators.
![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment1.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment2.png)


### 3. Ticket Details & Workflow
Detailed view of a ticket showing status transitions, assignee, and discussion thread.

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment3.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment4.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment5.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment6.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment7.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment8.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment9.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment10.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment11.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment12.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment13.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment14.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment15.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment16.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment17.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment18.png)

![Ticket Details](./InternalMaintenance.Client/public/screenshots/equipment19.png)


### 4. Equipment Management
Asset tracking interface with department assignment and active/maintenance status toggle.
![Equipment Management](./InternalMaintenance.Client/public/screenshots/equipment19.png)

![Equipment Management](./InternalMaintenance.Client/public/screenshots/equipment20.png)

![Equipment Management](./InternalMaintenance.Client/public/screenshots/equipment21.png)


### 5. User Management & Access Control
Admin directory for managing users, roles (Admin/Manager/Staff/Technician), and department assignments.
![User Management](./InternalMaintenance.Client/public/screenshots/user1.png)

![User Management](./InternalMaintenance.Client/public/screenshots/user2.png)

![User Management](./InternalMaintenance.Client/public/screenshots/user3.png)

![User Management](./InternalMaintenance.Client/public/screenshots/user4.png)

![User Management](./InternalMaintenance.Client/public/screenshots/user5.png)


## Business Workflow

```mermaid
flowchart TB
    A[Staff] --> B[Create Ticket]
    B --> C[Pending]
    C --> D[Manager Assign]
    D --> E[Assigned]
    E --> F[Technician Works]
    F --> G[In Progress]
    G --> H[Resolved]
    H --> I[Staff Confirm]
    I --> J[Closed]
```

## Access Model

The backend uses role-based access control:

- `Admin`
- `Manager`
- `Staff`
- `Technician`

Ticket visibility is filtered by role:

- `Admin` can access all tickets
- `Manager` can access tickets in their department
- `Staff` can access tickets they created
- `Technician` can access tickets assigned to them

## Tech Stack

**Frontend:** React 19, TypeScript, Vite, TanStack Query, React Router, Zustand, React Hook Form, Zod  
**Backend:** ASP.NET Core 10, Entity Framework Core, JWT Authentication, BCrypt.Net-Next, Swagger/OpenAPI  
**Database:** Microsoft SQL Server 2022  
**Tooling:** Docker Compose, pnpm, ESLint, Vitest

## Repository Structure

```text
InternalMaintenanceManagement.slnx
|-- InternalMaintenance.Api/           # ASP.NET Core Web API
|   |-- Common/                         # Query and pagination helpers
|   |-- Constants/                      # Shared role, status, and priority constants
|   |-- Data/                           # DbContext and seed data
|   |-- Extensions/                     # Startup and pipeline wiring
|   |-- Migrations/                     # EF Core migrations
|   |-- Models/                         # Domain entities
|   |-- Modules/                        # Feature modules
|   |   |-- Auth/
|   |   |-- Departments/
|   |   |-- Equipment/
|   |   |-- Tickets/
|   |   `-- Users/
|   `-- Services/                       # JWT, current user, ticket code generation
|-- InternalMaintenance.Client/         # React + Vite frontend
|   |-- src/
|   |   |-- app/
|   |   |-- features/
|   |   |-- entities/
|   |   |-- pages/
|   |   |-- shared/
|   |   `-- main.tsx
|   `-- public/
|-- docker-compose.yml                  # SQL Server development container
|-- .env.example
`-- README.md
```

## System Architecture

```mermaid
flowchart LR
    A[React Client] --> B[ASP.NET Core API]
    B --> C[JWT Authentication]
    B --> D[Entity Framework Core]
    D --> E[(SQL Server)]
    B --> F[Seeded Roles, Users, Departments, Equipment]
```

The backend is organized as a modular monolith, which keeps feature boundaries clear without splitting the system into many separate services too early.

## API Modules

### Authentication

- `POST /api/auth/login`
- `GET /api/auth/me`
- `POST /api/auth/change-password`
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout`

### Users

- `GET /api/users`
- `GET /api/users/{id}`
- `POST /api/users`
- `PUT /api/users/{id}`
- `PATCH /api/users/{id}/status`
- `PATCH /api/users/{id}/reset-password`

Supported query parameters for `GET /api/users`:

- `keyword`
- `role`
- `departmentId`
- `isActive`
- `page`
- `pageSize`

### Departments

- `GET /api/departments`
- `GET /api/departments/{id}`
- `POST /api/departments`
- `PUT /api/departments/{id}`
- `DELETE /api/departments/{id}`

Supported query parameters for `GET /api/departments`:

- `keyword`
- `page`
- `pageSize`

### Equipment

- `GET /api/equipment`
- `GET /api/equipment/{id}`
- `POST /api/equipment`
- `PUT /api/equipment/{id}`
- `DELETE /api/equipment/{id}`

Supported query parameters for `GET /api/equipment`:

- `keyword`
- `status`
- `departmentId`
- `page`
- `pageSize`

### Dashboard

- `GET /api/dashboard/summary`
- `GET /api/dashboard/charts`

### Ticket Attachments

- `POST /api/tickets/{ticketId}/attachments/presign`
- `POST /api/tickets/{ticketId}/attachments/confirm`
- `GET /api/tickets/{ticketId}/attachments`
- `GET /api/tickets/{ticketId}/attachments/{attachmentId}/download-url`
- `DELETE /api/tickets/{ticketId}/attachments/{attachmentId}`

### Maintenance Tickets

- `GET /api/tickets`
- `GET /api/tickets/{id}`
- `POST /api/tickets`
- `PUT /api/tickets/{id}`
- `PATCH /api/tickets/{id}/assign`
- `PATCH /api/tickets/{id}/status`
- `POST /api/tickets/{id}/comments`
- `GET /api/tickets/{id}/comments`
- `GET /api/tickets/{id}/history`

Supported query parameters for `GET /api/tickets`:

- `status`
- `priority`
- `equipmentId`
- `page`
- `pageSize`

Ticket statuses:

- `Pending`
- `Assigned`
- `InProgress`
- `Resolved`
- `Closed`
- `Cancelled`

Ticket priorities:

- `Low`
- `Medium`
- `High`
- `Critical`

## Ticket Workflow Rules

- A ticket can be cancelled from `Pending` or `Assigned`
- A ticket can move from `Assigned` to `InProgress`
- A ticket can move from `InProgress` to `Resolved`
- A ticket can move from `Resolved` to `Closed`
- A resolution note is required before resolving a ticket
- Closed tickets should not be modified
- Every status change is stored in history

## Seeded Demo Data

The API seeds local data on startup so the project can be explored immediately.

Seeded roles:

- `Admin`
- `Manager`
- `Staff`
- `Technician`

Seeded departments:

- `IT`
- `Accounting`
- `HR`

Seeded users:

- `admin@test.com`
- `manager@test.com`
- `staff@test.com`
- `technician@test.com`

Seeded equipment includes:

- `PRN-ACC-001` - Canon Printer - Accounting Room
- `RTR-IT-001` - Main Office Router

Temporary password for seeded accounts:

```text
Temp@123456
```

Seeded users are marked `MustChangePassword = true` on first login.

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js LTS
- pnpm
- Docker Desktop
- Microsoft SQL Server 2022, or Docker Compose

### Configuration

Create a local `.env` file from the example:

```powershell
Copy-Item .env.example .env
```

### Environment Variables

| Variable | Purpose | Example |
| --- | --- | --- |
| `ConnectionStrings__DefaultConnection` | SQL Server connection string used by the API | `Server=localhost,1433;Database=InternalMaintenanceDb;User Id=sa;Password=YourStrongPassword;TrustServerCertificate=True;Encrypt=False` |
| `Jwt__Key` | Secret key used to sign JWT access tokens | `replace-with-a-long-random-secret` |
| `Jwt__Issuer` | JWT issuer claim | `InternalMaintenance.Api` |
| `Jwt__Audience` | JWT audience claim | `InternalMaintenance.Client` |
| `Jwt__ExpiresInMinutes` | Token lifetime in minutes | `60` |
| `MSSQL_SA_PASSWORD` | SQL Server SA password for Docker Compose | `YourStrongPassword` |
| `SQLSERVER_PORT` | Local port exposed by the SQL Server container | `1433` |

### Start the Database

```powershell
docker compose up -d
```

### Start the API

```powershell
dotnet restore InternalMaintenance.Api/InternalMaintenance.Api.csproj
dotnet run --project InternalMaintenance.Api
```

API URLs:

- `http://localhost:5253`
- `https://localhost:7237`

Swagger:

- `http://localhost:5253/swagger`

### Start the Frontend

```powershell
cd InternalMaintenance.Client
pnpm install
pnpm dev
```

Vite dev server:

- `http://localhost:5173`

## Frontend Notes

The client app is built with React + Vite and is structured with feature-oriented folders:

- `app/` for routing, layouts, providers, and guards
- `features/` for auth and ticket-related UI logic
- `entities/` for shared domain types
- `pages/` for route-level screens
- `shared/` for API clients, UI primitives, config, and helpers

The API proxy in `vite.config.ts` forwards:

- `/api` to `http://localhost:5253`
- `/swagger` to `http://localhost:5253`

Useful scripts in `InternalMaintenance.Client/package.json`:

- `pnpm dev`
- `pnpm build`
- `pnpm lint`
- `pnpm test`
- `pnpm test:run`
- `pnpm format`
- `pnpm format:check`
- `pnpm generate:api`

## Business Rules

- One equipment item cannot have multiple active maintenance tickets
- Equipment code is immutable after creation
- A department cannot be deleted if related users or equipment still exist
- Every ticket status change is recorded
- A resolution note is required before a ticket can be resolved
- Ticket visibility depends on role and department

## Feature Implementation Status

| Feature / Module | Status | Backend (.NET) | Frontend (React) | Description |
| --- | --- | --- | --- | --- |
| **JWT Auth & Refresh Token** | ✅ Completed | Yes | Yes | Login, logout, token rotation, current user lookup |
| **Role-Based Access (RBAC)** | ✅ Completed | Yes | Yes | Admin, Manager, Staff, Technician authorization |
| **User Management** | ✅ Completed | Yes | Yes | User directory, active status toggle, reset password |
| **Department Management** | ✅ Completed | Yes | Yes | Department CRUD, `IsMaintenanceTeam` flag |
| **Equipment Management** | ✅ Completed | Yes | Yes | Asset tracking, department link, immutable code |
| **Maintenance Tickets** | ✅ Completed | Yes | Yes | Create ticket, technician assignment, status workflow |
| **Ticket Comments & History** | ✅ Completed | Yes | Yes | Discussion thread, automated status audit log |
| **Ticket Attachments & Files** | ✅ Completed | Yes | Yes | Presigned upload, image/video/PDF preview, deletion |
| **Dashboard & Analytics** | ✅ Completed | Yes | Yes | Summary KPIs, status & priority charts, dept equipment stats |
| **Dark / Light Theme** | ✅ Completed | N/A | Yes | Theme mode switcher with persistent UI state |
| **Equipment QR Code** | 🔳 Pending | No | No | Generate & scan QR codes for rapid asset lookup |
| **Email Notifications** | 🔳 Pending | No | No | SMTP/SendGrid alerts on ticket lifecycle events |
| **Preventive Maintenance** | 🔳 Pending | No | No | Scheduled recurring maintenance tasks & automatic tickets |
| **SLA Tracking & Alerts** | 🔳 Pending | No | No | Target resolution times & SLA breach alerts |
| **Data Export & PDF/Excel** | 🔳 Pending | No | No | Export tickets and asset history reports to Excel/PDF |
| **Multi-building / Location** | 🔳 Pending | No | No | Location hierarchy (Building -> Floor -> Room) |
| **Multi-tenancy & SaaS** | 🔳 Pending | No | No | Organization isolation & multi-tenant billing |

## Roadmap

### Next Improvements (Short-Term)

- [ ] **Equipment QR Code**: Generate QR codes for equipment items and enable camera scanning on mobile web.
- [ ] **Email Notification**: Integrate email service to alert users on ticket assignment, comment, or status resolution.
- [ ] **Preventive Maintenance Scheduling**: Periodic asset inspection plans with automatic recurring ticket generation.
- [ ] **SLA Dashboard & Alerts**: Target resolution SLA metrics, overdue ticket highlights, and escalation rules.
- [ ] **Data Export**: Export maintenance history and asset inventories to CSV, Excel, or PDF format.

### Longer-Term Ideas

- [ ] **Multi-Building / Location Hierarchy**: Support complex office physical locations (Campus -> Building -> Floor -> Room).
- [ ] **Multi-Tenant Architecture**: Data isolation for multiple organizations / companies within a single platform instance.
- [ ] **Subscription & Billing Model**: Usage tier management and invoicing system.
- [ ] **Predictive Maintenance & Forecasting**: Maintenance history analysis to forecast equipment failure rates.

## Contributing

Pull requests are welcome. For larger changes, please open an issue first so scope and direction can be aligned.

## License

No license file is currently included in this repository.
