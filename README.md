# CloudPallet: Cold Storage WMS

CloudPallet is a robust, scalable Warehouse Management System (WMS) built specifically for the complexities of cold chain logistics. It is designed to manage temperature-controlled inventory, automate receiving and shipping workflows, and provide granular, event-driven billing for Third-Party Logistics (3PL) cold storage operations.

The system is built on a modern, decoupled architecture using Domain-Driven Design (DDD) and Command Query Responsibility Segregation (CQRS) principles, ensuring high performance, maintainability, and scalability.

## Key Features

  - **Cold Storage Focus:** Real-time temperature monitoring, support for perishables (lot/batch tracking), and compliance with standards like ISO 22000.
  - **Automated Receiving:** Integrates with scales for weighing, generates unique SSCC pallet barcodes on-the-fly, and supports batch scanning of materials.
  - **Advanced Inventory & Putaway:** Manages precise, hierarchical locations (Warehouse \> Room \> Bay \> Row \> Column \> Level) and uses services to suggest optimal putaway locations.
  - **Picking & Shipping:** Enforces barcode scanning for pick confirmation to reduce errors and manages the full shipping and withdrawal workflow.
  - **Dock & Yard Management:** Features an automated dock scheduling system to manage inbound/outbound appointments and tracks trailer status in yard spots.
  - **Granular 3PL Billing:** An event-driven billing service that automatically calculates charges for clients (Accounts) based on specific activities. This includes:
      - Storage (tiered by temperature zone and duration).
      - Handling (receiving, putaway, picking, shipping).
      - Value-Added Services (VAS) such as blasting, kitting, repackaging, and fumigation.
  - **Role-Based Access Control (RBAC):** Comprehensive security with distinct roles (e.g., **Admin**, **Operator**, **Finance**) to ensure users only access relevant workflows (e.g., Finance handles invoicing, Operator handles receiving).
  - **Real-time Monitoring:** Uses SignalR to provide live updates for dashboards, including dock status and room temperatures.

## Technology Stack

The project is a full-stack solution, combining a .NET backend with an Angular frontend.

### Backend (.NET 9)

  - **Framework:** .NET 9, ASP.NET Core Web API
  - **Architecture:** Domain-Driven Design (DDD), CQRS (with MediatR), and Clean/Hexagonal Architecture.
  - **Database:** Entity Framework Core 9 with SQL Server.
  - **Authentication:** ASP.NET Core Identity with JWT Bearer tokens for
    authentication and policy-based authorization (RBAC).
  - **Real-time:** SignalR for live dashboard and notification pushes.
  - **Caching:** StackExchangeRedis for performance.
  - **Reporting:** QuestPDF for generating PDF invoices and reports.
  - **Logging:** Serilog.

### Frontend (Angular 20)

  - **Framework:** Angular 20
  - **Language:** TypeScript
  - **UI:** Angular Material
  - **State Management:** RxJS
  - **Real-time:** SignalR client to connect to the backend hubs.
  - **Dashboards:** `@swimlane/ngx-charts` for data visualization.

## Project Architecture

The system follows a layered architecture inspired by DDD and Clean Architecture, promoting a strong separation of concerns.

  - **`WMS.Domain`:** Contains the core business logic, including all aggregates (e.g., `Warehouse`, `DockAppointment`, `Invoice`), entities, and domain services (e.g., `BillingService`).
  - **`WMS.Application`:** Implements the CQRS pattern. This layer holds all command and query handlers, DTOs, and application-level services. It orchestrates the domain logic to fulfill use cases.
  - **`WMS.Infrastructure`:** Handles all external concerns. This includes database persistence using EF Core (`WmsDbContext`), integrations with external services (like scales or printers), and implementations for caching (Redis).
  - **`WMS.Api` (Presentation):** The ASP.NET Core Web API project. It exposes endpoints, handles authentication (JWT), wires up services (`Program.cs`), and hosts the SignalR hubs.
  - **`wms-frontend` (Presentation):** The Angular client application that consumes the API and provides the user interface for all WMS operations.

For a complete and in-depth breakdown of the domain model, bounded contexts, aggregates, and design decisions, please see the `architecture.md` file.

## Prerequisites

To build and run this project, you will need:

  - .NET 9 SDK (or newer)
  - Node.js (LTS version) & npm
  - Angular CLI v20
  - A running instance of SQL Server (e.g., LocalDB, Docker, or a cloud instance)
  - A running instance of Redis (e.g., local or Docker)
  - A development secret manager (e.g., `dotnet user-secrets`)

## Getting Started

Follow these steps to get the application running locally.

### 1\. Backend (WMS.Api)

1.  **Configure Secrets:** The backend requires connection strings and a JWT secret key. Navigate to the API project directory:

    ```bash
    cd src/Presentation/WMS.Api
    ```

    Initialize user secrets:

    ```bash
    dotnet user-secrets init
    ```

    Set the required secrets (update values for your local environment):

    ```bash
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=WmsDev;Trusted_Connection=True;"
    dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379"
    dotnet user-secrets set "JwtSettings:SecretKey" "YOUR_REALLY_LONG_AND_SECURE_SECRET_KEY_HERE"
    dotnet user-secrets set "JwtSettings:Issuer" "WMS.Api"
    dotnet user-secrets set "JwtSettings:Audience" "WMS.Client"
    ```

    *Note: The JWT settings are read in `Program.cs`*.

2.  **Restore & Build:**

    ```bash
    dotnet restore ../../../WMS.sln
    dotnet build ../../../WMS.sln
    ```

3.  **Apply EF Migrations:** (Assuming migrations are set up in `WMS.Infrastructure`)

    ```bash
    # From the WMS.Api directory
    dotnet ef database update
    ```

4.  **Run the Backend:**

    ```bash
    dotnet run
    ```

    The API will start, typically on `http://localhost:5000` (or a similar port).

### 2\. Frontend (wms-frontend)

1.  **Navigate to Frontend:** Open a new terminal and go to the frontend directory:

    ```bash
    cd wms-frontend
    ```

2.  **Install Dependencies:**

    ```bash
    npm install
    ```

    *This will install all packages listed in `package.json`*.

3.  **Run the Frontend:**

    ```bash
    ng serve
    ```

    *This uses the `start` script from `package.json`*.

4.  **Access the Application:**
    Open your browser and navigate to `http://localhost:4200/`. The Angular app will automatically reload if you make changes to the source files.

    *Note: Ensure the `WMS.Api` CORS policy in `Program.cs` allows `http://localhost:4200`*.

## Security & RBAC

Authentication is handled via JWT, and authorization is managed by policies. The system defines three core roles in the `UserRole` enum:

  - **`Admin`**: Full access to all modules, including user management and system configuration.
  - **`Operator`**: Can perform all core warehouse operations (receiving, putaway, picking, dock scheduling) but cannot access financial or administrative modules.
  - **`Finance`**: Can access billing, generate invoices, manage rates, and view related reports, but cannot perform warehouse operations.

These policies are enforced at the API controller level using `[Authorize(Policy = "...")]` attributes and are checked within CQRS handlers for fine-grained control. The frontend UI should also use these roles to hide or disable links and features (e.g., a "Billing" tab only visible to the `Finance` role).

## Video Demo

[![Watch the demo on LinkedIn!](https://media.licdn.com/dms/image/v2/D5622AQELbK5QRi_hPA/feedshare-shrink_2048_1536/B56Zn5U9mMI0Aw-/0/1760824648838?e=1764806400&v=beta&t=ZgdtPJWpWLcezEnDhKbzEIAG94f_Klft4mBZCQDY4ag)](https://www.linkedin.com/feed/update/urn:li:ugcPost:7388265305703292928/)
