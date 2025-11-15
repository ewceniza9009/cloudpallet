# Warehouse Management System (WMS) Architecture Specification

**Version 1.7**  
**Date: October 10, 2025**  
**Prepared by: Erwin Wilson Ceniza**  
**Status: Final**  
**Change Log**: Refined document for enhanced clarity, consistency, and flow (e.g., streamlined descriptions, consistent terminology, minor phrasing adjustments across sections); Expanded "Key Aggregates and Entities" with refined invariants for better enforceability; Updated "Security and Compliance" with comprehensive Role-Based Authentication (RBAC) details, including permissions matrix, policy integration, and assignment workflows; Aligned related sections (e.g., User entity, AuditTrail) for RBAC support; Incorporated cold storage specifics like temperature tolerances and certifications.

## Table of Contents

1. [Executive Summary](#executive-summary)  
2. [Introduction](#introduction)  
   2.1 [Purpose](#purpose)  
   2.2 [Scope](#scope)  
   2.3 [Assumptions and Key Decisions](#assumptions-and-key-decisions)  
3. [System Overview](#system-overview)  
   3.1 [High-Level Architecture](#high-level-architecture)  
   3.2 [Deployment Diagram](#deployment-diagram)  
   3.3 [Technology Stack](#technology-stack)  
   3.4 [Non-Functional Requirements](#non-functional-requirements)  
4. [Domain Model](#domain-model)  
   4.1 [Bounded Contexts](#bounded-contexts)  
   4.2 [Key Aggregates and Entities](#key-aggregates-and-entities)  
   4.3 [Value Objects](#value-objects)  
   4.4 [Domain Services](#domain-services)  
   4.5 [Domain Events](#domain-events)  
5. [Data Model and Setup](#data-model-and-setup)  
   5.1 [Setup Tables](#setup-tables)  
   5.2 [Database Schema Considerations](#database-schema-considerations)  
6. [Business Processes](#business-processes)  
   6.1 [Core Workflows](#core-workflows)  
   6.2 [CQRS Implementation Examples](#cqrs-implementation-examples)  
7. [Technical Architecture](#technical-architecture)  
   7.1 [Backend Implementation](#backend-implementation)  
   7.2 [Frontend Implementation](#frontend-implementation)  
   7.3 [Integration Layer](#integration-layer)  
8. [Security and Compliance](#security-and-compliance)  
   8.1 [Authentication and Authorization](#authentication-and-authorization)  
   8.2 [Audit and Traceability](#audit-and-traceability)  
9. [Deployment and Operations](#deployment-and-operations)  
   9.1 [Deployment Model](#deployment-model)  
   9.2 [Monitoring and Scalability](#monitoring-and-scalability)  
10. [Appendices](#appendices)  
    A. [Text-Based Diagrams](#text-based-diagrams)  
    B. [Glossary](#glossary)  
    C. [References](#references)  

## Executive Summary

This document outlines the complete architecture for a **Cold Storage Warehouse Management System (WMS)**, a robust, scalable solution for managing temperature-controlled inventory, logistics, and operations in cold chain environments. Built on Domain-Driven Design (DDD), Command Query Responsibility Segregation (CQRS), and dependency injection principles, the system supports precise location tracking (bay, row, column, level), automated receiving with scale integration and pallet-level barcode generation, picking/withdrawal for shipping with barcode scanning, and advanced docking logistics for cargo management. Invoicing is highly granular, charging per material for services like blasting (per cycle/lb), refrigeration (tiered by temp zone and duration), storage (per pallet/day), handling (receiving/putaway/picking/shipping per unit/weight), and value-added services (VAS) such as kitting, repackaging, fumigation, and processing fees.

Key innovations include:
- **Cold Storage Focus**: Real-time temperature monitoring, lot/batch tracking for perishables, and compliance with ISO 22000 and FDA standards for pharmaceuticals.
- **Receiving & Pallets**: Hierarchical receiving (shipment → pallets → materials) with batch scanning and on-scale barcode generation to reduce errors by up to 40%.
- **Picking Efficiency**: Mandatory barcode reads for pick confirmation, supporting batch modes.
- **Docking Logistics**: Automated dock scheduling, yard management, and cargo manifests to optimize throughput and minimize cold air loss.
- **Invoicing Granularity**: Event-driven per-material charges (e.g., $1.25/100 lbs for freezing), configurable rates for storage ($8–$40/pallet/month), handling, and VAS like blast room usage or labor servicing fees; Billed to the Account (customer/client storing goods).
- **Implementation**: Single ASP.NET Core 9 Web API backend with Angular 20 frontend, ensuring rapid development and maintainability.
- **Security Enhancements**: Comprehensive Role-Based Authentication (RBAC) with granular permissions, ensuring operators access only relevant workflows (e.g., Finance role for invoicing).

The architecture aligns with industry best practices, such as modeling warehouses as aggregates in DDD for cohesive domain logic and using CQRS to separate read/write models for high-performance queries. This design supports scaling to 10,000+ SKUs and 100+ daily dock appointments, with built-in extensibility for event sourcing and energy demand management.

## Introduction

### Purpose
The WMS addresses the complexities of cold storage operations, where temperature fluctuations can lead to spoilage and regulatory violations. It enables end-to-end visibility from dock arrival to shipment, automating workflows to reduce errors by up to 30% through features like lot tracking, pallet-level barcode generation during receiving, and scan confirmation in picking. Detailed invoicing ensures accurate billing for per-material services (e.g., blasting vs. standard refrigeration) and VAS (e.g., processing fees for kitting/repacks), with automated calculations from WMS events to minimize disputes and support 3PL profitability. Charges are applied to the Account (the customer/client responsible for stored goods), not the Supplier (vendor delivering goods).

### Scope
- **Domains**: Inventory, Receiving/Putaway, Picking/Shipping, Billing (granular per-material/service), Configuration, Security, Docking Logistics, Energy Management.
- **Features**: Hierarchical locations, scale integration with barcode printing, palletized receiving with multi-material support, barcode-scanned picking, temperature-zoned billing with VAS (blasting, chilling, handling, processing), automated dock scheduling, cargo manifests, real-time dashboards, AS/RS integration for automated putaway/picking.
- **Exclusions**: Hardware procurement (e.g., physical docks, refrigeration units, label printers); full ERP integration (stubbed for future).

### Assumptions and Key Decisions
- Multi-tenant via Company entity.
- SQL Server for persistence; in-memory for dev.
- RESTful APIs with Swagger; no GraphQL initially.
- Compliance: FIFO for perishables; audit trails for all transactions; support for vertical racking to maximize space in cold environments; GS1 standards for pallet barcodes (SSCC).
- Invoicing: Configurable rates per Account (customer); Grace periods and tiered storage; Automated VAS capture; Suppliers are not billed—only tracked for inbound logistics.
- Security: RBAC enforced at API and UI levels; Roles assigned via Admin workflow.

## System Overview

### High-Level Architecture
The system employs a **layered hexagonal architecture** inspired by Clean Architecture and DDD, promoting separation of concerns: Domain (core logic), Application (use cases via CQRS), Infrastructure (persistence/integrations), and Presentation (API/UI). CQRS decouples commands (writes) from queries (reads), using MediatR for orchestration and optional event sourcing for audit-heavy scenarios.

**Layers**:
- **Domain**: Entities, aggregates (e.g., Warehouse as root aggregate encompassing rooms/docks; Receiving as aggregate for pallet hierarchy; Invoice as aggregate for line-level charges).
- **Application**: Commands/Queries/Handlers; Validators (FluentValidation).
- **Infrastructure**: EF Core, external services (scales, sensors, AS/RS, barcode printers).
- **Presentation**: Controllers route to MediatR; Angular consumes via HttpClient.

### Deployment Diagram
```
[Angular PWA Frontend] <--> [API Gateway (Optional)] <--> [ASP.NET Core API]
                                           |                          |
                                 [Load Balancer]              [SQL Server DB]
                                           |                          |
[Mobile App (PWA)] <--> [SignalR Hub] <--> [Event Bus (MassTransit)] <--> [Redis Cache]
                                           |
                                 [Integrations: Scale API, Dock Sensors, ERP, AS/RS, Label Printer]
```

### Technology Stack
- **Backend**: .NET 9, MediatR, EF Core, FluentValidation, Serilog, AutoMapper.
- **Frontend**: Angular 20 (standalone), RxJS, Angular Material, SignalR.
- **Database**: SQL Server; Projections for read models.
- **DevOps**: Docker, Azure DevOps, Swagger.

### Non-Functional Requirements
| Category | Requirement | Metric/Detail |
|----------|-------------|---------------|
| **Performance** | Response time for core operations (e.g., inventory query). | < 200ms for 95% of requests; Support 1,000 concurrent users. |
| **Scalability** | Horizontal scaling for peak loads (e.g., 200 daily shipments). | Auto-scale pods in Kubernetes; CQRS for read replicas. |
| **Reliability** | Uptime and fault tolerance. | 99.9% availability; Graceful degradation for integrations. |
| **Security** | Data protection and compliance. | Encryption at rest/transit; GDPR/ISO 22000 adherence; RBAC with 95% policy enforcement coverage. |
| **Usability** | Intuitive UI for operators in cold gear. | Touch-optimized PWA; Offline support via Service Workers; Batch scan modes. |
| **Maintainability** | Code and deployment. | 80% test coverage; CI/CD pipelines. |
| **Energy Efficiency** | Minimize operational costs in cold storage. | Track and alert on door-open durations > 5min. |
| **Billing Accuracy** | Invoice generation precision. | Calculations to 5 decimal places; Automated VAS capture. |

## Domain Model

### Bounded Contexts
- **Core Logistics**: Inventory, Receiving (with pallets), Putaway, Picking (with scanning), Shipping.
- **Cold Chain**: Temperature management, batch tracking, energy monitoring.
- **Docking & Yard**: Appointments, cargo handling.
- **Billing & Reporting**: Invoicing (per-material/service lines), analytics, rate configuration.
- **Administration**: Setup, users (including RBAC role assignments).
- **Automation**: AS/RS coordination for high-density storage.

Contexts communicate via domain events (e.g., DockCheckInEvent triggers Receiving workflow; WithdrawalCompletedEvent triggers Invoice generation).

### Key Aggregates and Entities

This section provides exhaustive field details for each entity, including data types, constraints, and rationale based on standard WMS practices. Fields are designed for cold storage nuances, e.g., temperature certifications and expiry tracking. Invariants refined for stricter enforcement in domain logic.

| Aggregate/Entity | Description | Detailed Fields | Invariants/Business Rules |
|------------------|-------------|-----------------|---------------------------|
| **Warehouse** (Aggregate Root) | Facility with hierarchy. | - Id (Guid, PK, Required): Unique identifier.<br>- CompanyId (Guid, FK to Company, Required): Tenant isolation.<br>- Name (string, Max 100, Required): e.g., "Main Cold Facility".<br>- Address (string, Max 500, Required): Full street address.<br>- City (string, Max 50), State (string, Max 2, e.g., "CA"), PostalCode (string, Max 10), Country (string, Max 50).<br>- TotalCapacityWeight (decimal, Precision 18,2, kg): Max load.<br>- TotalCapacityVolume (decimal, Precision 18,2, m³): Space limit.<br>- OperatingHours (string, Max 100): e.g., "24/7".<br>- ContactPhone (string, Max 20), ContactEmail (string, Max 100).<br>- IsActive (bool, Default true): Operational status.<br>- CreatedDate (DateTime, Required), UpdatedDate (DateTime), Version (int, for concurrency). | Total capacity enforced across rooms/locations via domain validation; Modeled as single aggregate for consistency; RBAC: Admin/Finance read/write. |
| **Room** | Temp zone (e.g., -20°C Blast). | - Id (Guid, PK, Required).<br>- WarehouseId (Guid, FK to Warehouse, Required).<br>- Name (string, Max 50, Required): e.g., "Blast Freezer A".<br>- TemperatureMin (decimal, Precision 5,1, °C, Required): e.g., -25.0.<br>- TemperatureMax (decimal, Precision 5,1, °C, Required): e.g., -18.0.<br>- ServiceType (Enum: Chilling, Blasting, FrozenStorage, Required): Zone purpose.<br>- CapacityPallets (int, Default 1000): Slot count.<br>- HumidityRangeMin/Max (decimal, Precision 3,1, %): RH control.<br>- EnergyProfileId (Guid, FK): Linked to monitoring.<br>- IsActive (bool, Default true).<br>- CreatedDate/UpdatedDate (DateTime). | MinTemp < MaxTemp enforced in constructor; Real-time monitoring integration; Energy usage tracking; RBAC: Operator read, Admin write. |
| **Location** | Slot: Warehouse > Room > Bay > Row > Column > Level. | - Id (Guid, PK, Required).<br>- RoomId (Guid, FK to Room, Required).<br>- Bay (string, Max 10, Required): e.g., "A1".<br>- Row (int, 1-100, Required).<br>- Column (int, 1-50, Required).<br>- Level (int, 1-20, Required): Shelf tier.<br>- CapacityWeight (decimal, Precision 10,2, kg): Per slot.<br>- CapacityVolume (decimal, Precision 10,2, m³).<br>- IsActive (bool, Default true).<br>- ZoneType (Enum: Picking, Storage, Staging): Usage.<br>- CreatedDate/UpdatedDate (DateTime). | Unique path (composite index on RoomId + Bay/Row/Col/Level); Capacity checks during putaway via service; Optimized for vertical racking; RBAC: Operator update during putaway/pick. |
| **Dock** | Loading bay. | - Id (Guid, PK, Required).<br>- WarehouseId (Guid, FK to Warehouse, Required).<br>- Name (string, Max 20, Required): e.g., "Dock 1".<br>- Type (Enum: Inbound, Outbound, Both, Required).<br>- EquipmentFlags (bitmask int: Seals=1, Levelers=2, AutoDoors=4): Features.<br>- CapacityTruckLength (decimal, Precision 5,2, m): Trailer fit.<br>- IsActive (bool, Default true).<br>- CreatedDate/UpdatedDate (DateTime). | No overlapping appointments enforced in scheduling service; Insulated for cold ops; Auto-door controls; RBAC: Operator schedule/read. |
| **YardSpot** | Trailer parking. | - Id (Guid, PK, Required).<br>- WarehouseId (Guid, FK to Warehouse, Required).<br>- SpotNumber (string, Max 10, Required): e.g., "Y01".<br>- Status (Enum: Available, Occupied, Reserved, Required).<br>- CapacityTruckLength (decimal, Precision 5,2, m).<br>- IsActive (bool, Default true).<br>- CreatedDate/UpdatedDate (DateTime). | Auto-assignment on check-in via service; RBAC: Operator update status. |
| **Carrier** | Trucking entity. | - Id (Guid, PK, Required).<br>- Name (string, Max 100, Required).<br>- ContactName (string, Max 100), ContactPhone (string, Max 20), ContactEmail (string, Max 100).<br>- CertificationColdChain (bool, Default false): Compliance flag.<br>- IsActive (bool, Default true).<br>- CreatedDate/UpdatedDate (DateTime). | Cold chain certification validation; RBAC: Admin create/update. |
| **Truck** | Transport vehicle. | - Id (Guid, PK, Required).<br>- CarrierId (Guid, FK to Carrier, Required).<br>- LicensePlate (string, Max 20, Required).<br>- TempCapabilityMin/Max (decimal, Precision 5,1, °C): e.g., -30 to 5.<br>- IsActive (bool, Default true).<br>- CreatedDate/UpdatedDate (DateTime). | Matches dock capacity in assignment; RBAC: Operator read during scheduling. |
| **DockAppointment** (Aggregate Root) | Scheduled slot. | - Id (Guid, PK, Required).<br>- DockId (Guid, FK to Dock, Required).<br>- TruckId (Guid, FK to Truck).<br>- StartTime/EndTime (DateTime, Required).<br>- Type (Enum: Receiving, Shipping, Required).<br>- Status (Enum: Scheduled, InProgress, Completed, Cancelled, Required).<br>- CargoManifestId (Guid, FK).<br>- EnergyImpactScore (decimal, Precision 5,2): Estimated cost.<br>- CreatedDate/UpdatedDate (DateTime). | No overlaps (unique index on DockId + Start/End); Buffer time for loading enforced in service; Energy impact scoring; RBAC: Operator create/update. |
| **CargoManifest** | Load/unload list. | - Id (Guid, PK, Required).<br>- AppointmentId (Guid, FK to DockAppointment, Required).<br>- Lines (List: MaterialId (Guid), Quantity (decimal), Weight (decimal)): Details.<br>- TotalWeight (decimal, Precision 10,2, kg).<br>- CreatedDate/UpdatedDate (DateTime). | Balances with transactions via validation; RBAC: Operator edit lines. |
| **Material** | SKU. | - Id (Guid, PK, Required).<br>- Name (string, Max 100, Required): e.g., "Frozen Chicken Breasts".<br>- Description (string, Max 500): Full details.<br>- CategoryId (Guid, FK to MaterialCategory, Required).<br>- UOMId (Guid, FK to UnitOfMeasure, Required).<br>- Perishable (bool, Default false): Expiry flag.<br>- DefaultBarcodeFormat (Enum: GS1_128, UPC, Required).<br>- BaseWeight (decimal, Precision 10,3, kg): Per unit.<br>- DimensionsLength/Width/Height (decimal, Precision 5,2, cm): Packaging.<br>- CostPerUnit (decimal, Precision 10,2, currency).<br>- MinStockLevel (int), MaxStockLevel (int): Reorder.<br>- ShelfLifeDays (int): Default expiry.<br>- IsHazardous (bool): Cold chain handling.<br>- GS1BarcodePrefix (string, Max 20): Standard.<br>- IsActive (bool, Default true).<br>- CreatedDate/UpdatedDate (DateTime). | GS1 barcode support; Used for line-level labeling; RBAC: Admin create, Operator read. |
| **MaterialCategory** | Grouping (e.g., Frozen). | - Id (Guid, PK, Required).<br>- Name (string, Max 50, Required): e.g., "Poultry".<br>- ParentId (Guid, FK to self, Nullable): Hierarchy.<br>- Description (string, Max 200).<br>- IsActive (bool, Default true).<br>- CreatedDate/UpdatedDate (DateTime). | Hierarchical validation (no cycles); RBAC: Admin manage. |
| **Supplier** | Vendor delivering goods (not billed). | - Id (Guid, PK, Required).<br>- Name (string, Max 100, Required): e.g., "ABC Foods Inc.". <br>- Description (string, Max 500): Business overview.<br>- AddressStreet (string, Max 200), City (string, Max 50), State (string, Max 2), PostalCode (string, Max 10), Country (string, Max 50).<br>- ContactName (string, Max 100), Phone (string, Max 20), Email (string, Max 100), Fax (string, Max 20).<br>- TaxId (string, Max 20, Required): VAT/GST number.<br>- LeadTimeDays (int, Default 0): Avg delivery.<br>- CertificationColdChain (bool, Default false): ISO/FDA.<br>- PaymentTerms (string, Max 50): e.g., "Net 30".<br>- CurrencyCode (string, Max 3): e.g., "USD".<br>- IsActive (bool, Default true).<br>- CreditLimit (decimal, Precision 12,2).<br>- CreatedDate/UpdatedDate (DateTime). | Workflow approval; Inbound only—no charges; RBAC: Admin create, Operator read. |
| **UnitOfMeasure** | e.g., KG. | - Id (Guid, PK, Required).<br>- Name (string, Max 50, Required): e.g., "Kilogram".<br>- Symbol (string, Max 5, Required): "kg".<br>- ConversionFactor (decimal, Precision 10,4): To base unit.<br>- IsActive (bool, Default true).<br>- CreatedDate/UpdatedDate (DateTime). | Unique symbol enforced; RBAC: Admin manage. |
| **Account** | Customer/client storing goods (billed for services). | - Id (Guid, PK, Required).<br>- Name (string, Max 100, Required): e.g., "XYZ Retailers".<br>- TypeId (Enum: 3PL, Direct, Vendor, Required): Category.<br>- CategoryId (Guid, FK to MaterialCategory, Nullable): Preferred goods.<br>- AddressStreet (string, Max 200), City (string, Max 50), State (string, Max 2), PostalCode (string, Max 10), Country (string, Max 50).<br>- ContactName (string, Max 100), Phone (string, Max 20), Email (string, Max 100), Fax (string, Max 20).<br>- TaxId (string, Max 20, Required): Billing tax.<br>- CreditLimit (decimal, Precision 12,2, currency): Exposure cap.<br>- PaymentTerms (string, Max 50): e.g., "Net 60".<br>- CurrencyCode (string, Max 3, Default "USD").<br>- PreferredTempZone (Enum: Chilling, Frozen): Default storage.<br>- IsPreferred (bool, Default false): Priority status.<br>- IsActive (bool, Default true).<br>- CreatedDate/UpdatedDate (DateTime). | Credit limits checked in billing; Customer-specific pricing; Distinct from Supplier—handles outgoing shipments and receives invoices for storage/handling/VAS; RBAC: Finance read/write, Admin full. |
| **User** | System operator. | - Id (Guid, PK, Required).<br>- CompanyId (Guid, FK to Company, Required).<br>- Username (string, Max 50, Unique, Required).<br>- PasswordHash (string, Max 256, Required).<br>- Role (Enum: Admin, Operator, Finance, Required).<br>- FirstName/LastName (string, Max 50).<br>- Email (string, Max 100, Unique).<br>- Phone (string, Max 20).<br>- IsActive (bool, Default true).<br>- LastLogin (DateTime, Nullable).<br>- CreatedDate/UpdatedDate (DateTime). | RBAC claims populated on login; Role assignment audited. |
| **MaterialInventory** | Stock record. | - Id (Guid, PK, Required).<br>- MaterialId (Guid, FK to Material, Required).<br>- LocationId (Guid, FK to Location, Required).<br>- PalletId (Guid, FK to Pallet, Required).<br>- Quantity (decimal, Precision 12,3, Required): On hand.<br>- BatchNumber (string, Max 50).<br>- ExpiryDate (DateTime, Nullable).<br>- WeightActual (decimal, Precision 10,3, kg).<br>- Barcode (string, Max 50): GS1.<br>- AccountId (Guid, FK to Account, Required): Owner.<br>- Status (Enum: Available, Reserved, Quarantined).<br>- CreatedDate/UpdatedDate (DateTime). | Quantity >=0 enforced; FIFO expiry via service; Pallet-linked for traceability; Tied to Account for billing; RBAC: Operator update during pick/putaway. |
| **Receiving** (Aggregate Root) | Inbound shipment header. | - Id (Guid, PK, Required).<br>- SupplierId (Guid, FK to Supplier, Required).<br>- AppointmentId (Guid, FK to DockAppointment, Required).<br>- TotalWeight (decimal, Precision 12,2, kg, Required).<br>- Timestamp (DateTime, Required).<br>- Status (Enum: Pending, InProgress, Completed, Required).<br>- TotalPallets (int).<br>- AccountId (Guid, FK to Account, Nullable): Assigned owner.<br>- CreatedDate/UpdatedDate (DateTime). | Links to appointment; Triggers barcode gen on completion; Materials assigned to Account post-receiving; RBAC: Operator create/complete. |
| **Pallet** | Unit of receipt. | - Id (Guid, PK, Required).<br>- ReceivingId (Guid, FK to Receiving, Required).<br>- PalletNumber (string, Max 20, Required): Sequential.<br>- TotalWeight (decimal, Precision 10,3, kg, Required).<br>- Barcode (string, Max 50, Unique, Required): SSCC.<br>- Status (Enum: Received, Labeled, Putaway, Required).<br>- AccountId (Guid, FK to Account, Required): Owner.<br>- DimensionsLength/Width/Height (decimal, Precision 5,2, cm).<br>- CreatedDate/UpdatedDate (DateTime). | Generated post-weigh; Capacity limits validated; GS1 SSCC barcode; RBAC: Operator label/putaway. |
| **PalletLine** | Material detail per pallet. | - Id (Guid, PK, Required).<br>- PalletId (Guid, FK to Pallet, Required).<br>- MaterialId (Guid, FK to Material, Required).<br>- Quantity (decimal, Precision 12,3, Required).<br>- Weight (decimal, Precision 10,3, kg).<br>- BatchNumber (string, Max 50).<br>- ExpiryDate (DateTime, Nullable).<br>- AccountId (Guid, FK to Account, Required): Owner.<br>- CreatedDate/UpdatedDate (DateTime). | Sums to pallet totals enforced in aggregate; Scan-confirmed; RBAC: Operator add lines. |
| **PutawayTransaction** | Location assignment. | - Id (Guid, PK, Required).<br>- PalletId (Guid, FK to Pallet, Required).<br>- LocationId (Guid, FK to Location, Required).<br>- UserId (Guid, FK to User, Required).<br>- Timestamp (DateTime, Required).<br>- Status (Enum: Planned, Completed).<br>- CreatedDate/UpdatedDate (DateTime). | Availability check in service; AS/RS routing if enabled; Pallet-level move; RBAC: Operator execute. |
| **PickTransaction** | Outbound pick. | - Id (Guid, PK, Required).<br>- InventoryId (Guid, FK to MaterialInventory, Required).<br>- PalletId (Guid, FK to Pallet).<br>- Quantity (decimal, Precision 12,3, Required).<br>- Reason (Enum: Order, Adjustment, Required).<br>- ScannedBarcode (string, Max 50).<br>- AccountId (Guid, FK to Account, Required).<br>- Timestamp (DateTime, Required).<br>- Status (Enum: Planned, Confirmed).<br>- CreatedDate/UpdatedDate (DateTime). | <= Available stock enforced; Barcode scan required for confirmation; RBAC: Operator confirm. |
| **WithdrawalTransaction** | Shipping. | - Id (Guid, PK, Required).<br>- PickId (Guid, FK to PickTransaction, Required).<br>- AccountId (Guid, FK to Account, Required).<br>- ShipmentNumber (string, Max 50, Required).<br>- AppointmentId (Guid, FK to DockAppointment).<br>- TotalWeight (decimal, Precision 10,2, kg).<br>- Timestamp (DateTime, Required).<br>- Status (Enum: Packed, Shipped).<br>- CreatedDate/UpdatedDate (DateTime). | Triggers billing for Account; RBAC: Operator complete. |
| **DockTransaction** | Load/unload log. | - Id (Guid, PK, Required).<br>- AppointmentId (Guid, FK to DockAppointment, Required).<br>- Action (Enum: Load, Unload, Required).<br>- Timestamp (DateTime, Required).<br>- DockTemp (decimal, Precision 5,1, °C).<br>- DoorOpenDuration (decimal, Precision 5,2, min).<br>- Status (Enum: Started, Completed).<br>- CreatedDate/UpdatedDate (DateTime). | Minimize door-open time (<5min alert via service); RBAC: Operator log. |
| **InventoryAdjustment** | Corrections. | - Id (Guid, PK, Required).<br>- InventoryId (Guid, FK to MaterialInventory, Required).<br>- DeltaQuantity (decimal, Precision 12,3, Required): +/- change.<br>- Reason (Enum: Damage, Count, Expiry, Required).<br>- AccountId (Guid, FK to Account, Required).<br>- Timestamp (DateTime, Required).<br>- UserId (Guid, FK to User).<br>- CreatedDate/UpdatedDate (DateTime). | Audit required on creation; RBAC: Operator with approval. |
| **TransferTransaction** | Internal move. | - Id (Guid, PK, Required).<br>- FromLocationId (Guid, FK to Location, Required).<br>- ToLocationId (Guid, FK to Location, Required).<br>- PalletId (Guid, FK to Pallet, Required).<br>- Quantity (decimal, Precision 12,3, Required).<br>- Timestamp (DateTime, Required).<br>- UserId (Guid, FK to User).<br>- Status (Enum: Planned, Completed).<br>- CreatedDate/UpdatedDate (DateTime). | Same warehouse enforced; Pallet integrity; RBAC: Operator execute. |
| **Invoice** (Aggregate Root) | Billing doc for Account. | - Id (Guid, PK, Required).<br>- AccountId (Guid, FK to Account, Required).<br>- InvoiceNumber (string, Max 50, Unique, Auto-gen).<br>- PeriodStart/End (DateTime, Required): Billing cycle.<br>- TotalAmount (decimal, Precision 12,2, currency, Required).<br>- TaxAmount (decimal, Precision 10,2).<br>- Status (Enum: Draft, Issued, Paid, Overdue, Required).<br>- DueDate (DateTime).<br>- GracePeriodDays (int, Default 30).<br>- PdfPath (string, Max 500): Generated file.<br>- CreatedDate/UpdatedDate (DateTime). | Auto-generate on withdrawal; Line-level per material/service; Grace periods applied; RBAC: Finance generate/view. |
| **InvoiceLine** | Charge detail. | - Id (Guid, PK, Required).<br>- InvoiceId (Guid, FK to Invoice, Required).<br>- MaterialId (Guid, FK to Material, Nullable).<br>- ServiceType (Enum: Storage, Blasting, Refrigeration, Handling, VAS, Required).<br>- Quantity (decimal, Precision 12,3).<br>- UnitRate (decimal, Precision 10,4, currency).<br>- Amount (decimal, Precision 12,2, currency, Required).<br>- Description (string, Max 200): e.g., "Blasting 100lbs @ $1.25/100lbs".<br>- Tier (string, Max 50): e.g., "-20°C Zone".<br>- CreatedDate/UpdatedDate (DateTime). | Sums to total enforced in aggregate; Per-material for services like blasting; RBAC: Finance edit. |
| **Rate** | Pricing config. | - Id (Guid, PK, Required).<br>- AccountId (Guid, FK to Account, Required).<br>- ServiceType (Enum: Storage, Blasting, etc., Required).<br>- UOM (Enum: Pallet, Lb, Day, Cycle, Required).<br>- Value (decimal, Precision 10,4, currency, Required): e.g., 0.0125 ($1.25/100lbs).<br>- Tier (string, Max 50, Nullable): e.g., "Frozen Zone".<br>- EffectiveStart/EndDate (DateTime, Required): Validity.<br>- MinQuantity (decimal, Precision 12,3, Nullable): Threshold.<br>- IsActive (bool, Default true).<br>- CreatedDate/UpdatedDate (DateTime). | Customer-specific; Default fallback; EffectiveDate overlap prevented; RBAC: Finance/Admin configure. |
| **AuditTrail** | Log entry. | - Id (Guid, PK, Required).<br>- EntityType (string, Max 50, Required): e.g., "Pallet".<br>- Action (Enum: Create, Update, Delete, Required).<br>- UserId (Guid, FK to User, Required).<br>- Timestamp (DateTime, Required).<br>- Changes (string, Max 2000): JSON diff.<br>- OldValue/NewValue (string, Max 1000, Nullable).<br>- RoleAtTime (Enum: Admin, Operator, Finance): Captured for RBAC audit.<br>- CreatedDate (DateTime). | Immutable; Auto-generated on changes; RBAC: Admin view all, Finance view billing-related. |

### Value Objects
- **TemperatureRange**: Min/Max °C (validated, decimal Precision 5,1).
- **Weight**: Decimal kg with tolerance (±0.1kg).
- **LocationPath**: Immutable composite key (Bay-Row-Col-Level).
- **DockEquipment**: Features (e.g., Seals: bool).
- **EnergyMetric**: Duration (min) and cost factors (decimal).
- **Barcode**: String with format/validation (e.g., GS1 SSCC for pallets).
- **ChargeAmount**: Decimal with precision (5 places).

### Domain Services
- **InventoryService**: Availability calc, location suggestions (FIFO).
- **BillingService**: Rate lookup (per Account) → Compute lines (e.g., storage: pallet-days * zone-rate; blasting: cycles * lb-rate ($1.25/100lbs); handling: units * rate; VAS: kitting/repack by yield/quantity); Apply grace/taxes.
- **DockSchedulingService**: Auto-assign based on time/truck type; Optimize peaks with energy-aware slotting.
- **ColdChainService**: Expiry alerts, lot isolation, compliance reporting (FDA/ISO).
- **BarcodeGenerationService**: Generates SSCC barcodes for pallets post-receiving/weighing; Integrates with label printers.
- **ReceivingService**: Validates pallet totals, triggers barcode gen and putaway suggestions; Assigns to Account.
- **AuthorizationService**: Resolves RBAC permissions for commands/queries based on User.Role and context (e.g., AccountId scoping).

### Domain Events
- **MaterialReceivedEvent**: Triggers putaway suggestion and barcode printing; Accrues to Account.
- **DockCheckInEvent**: Assigns yard spot.
- **WithdrawalCompletedEvent**: Generates invoice with line details for Account.
- **DoorOpenEvent**: Monitors duration for energy alerts (adds to invoice if surcharge).
- **PalletLabeledEvent**: Notifies completion of receiving for pallet.
- **ServicePerformedEvent**: Captures VAS (e.g., blasting cycle) for billing to Account.
- **RoleAssignedEvent**: Logs user role changes for audit.

## Data Model and Setup

### Setup Tables
CRUD via Admin UI; Seeded via migrations. Expanded with detailed fields from entity specs; Added RoleAtTime to AuditTrail for RBAC tracing.

| Table | Key Fields (Expanded) | Relationships | Features |
|-------|-----------------------|---------------|----------|
| Company | Id (Guid), Name (string), TaxId (string) | 1:M Warehouse/User | Tenant key. |
| Warehouse | Id (Guid), CompanyId (Guid), Name (string), Address (string), TotalCapacityWeight (decimal), ... (full as above) | 1:M Room/Location/Dock/YardSpot | Layout optimized for vertical storage. |
| Room | Id (Guid), WarehouseId (Guid), Name (string), Min/MaxTemp (decimal), ServiceType (enum), CapacityPallets (int), ... | 1:M Location | Temp zones; Energy profile. |
| Location | Id (Guid), RoomId (Guid), Bay (string), Row/Col/Level (int), CapacityWeight (decimal), ... | 1:M Inventory | Unique index; AS/RS compatible. |
| Dock | Id (Guid), WarehouseId (Guid), Name (string), Type (enum), EquipmentFlags (int), ... | 1:M Appointment | Equipment flags; Sensor hooks. |
| YardSpot | Id (Guid), WarehouseId (Guid), SpotNumber (string), Status (enum), ... | N/A | Status enum. |
| Carrier | Id (Guid), Name (string), ContactName (string), CertificationColdChain (bool), ... | 1:M Truck | Cert status. |
| Material | Id (Guid), Name (string), Description (string), CategoryId (Guid), UOMId (Guid), Perishable (bool), DefaultBarcodeFormat (enum), BaseWeight (decimal), Dimensions (decimal x3), CostPerUnit (decimal), ... | M:1 Category/UOM | Barcode symbology. |
| Supplier | Id (Guid), Name (string), Description (string), Address (string), ContactName (string), TaxId (string), LeadTimeDays (int), CertificationColdChain (bool), ... | M:M Material | Contacts; Delivery tracking only. |
| UnitOfMeasure | Id (Guid), Name (string), Symbol (string), ConversionFactor (decimal), ... | 1:M Material | Conversions. |
| Account | Id (Guid), Name (string), TypeId (enum), CategoryId (Guid), Address (string), ContactName (string), TaxId (string), CreditLimit (decimal), PaymentTerms (string), PreferredTempZone (enum), ... | 1:M Invoice/Rate; M:1 MaterialInventory/Pallet/PalletLine | Balances; Customer pricing; Billed entity. |
| User | Id (Guid), CompanyId (Guid), Username (string), PasswordHash (string), Role (enum), FirstName (string), Email (string), ... | N/A | Roles; RBAC claims. |
| Receiving | Id (Guid), SupplierId (Guid), AppointmentId (Guid), TotalWeight (decimal), Timestamp (DateTime), Status (enum), TotalPallets (int), AccountId (Guid), ... | 1:M Pallet | Header; Status enum. |
| Pallet | Id (Guid), ReceivingId (Guid), PalletNumber (string), TotalWeight (decimal), Barcode (string), Status (enum), AccountId (Guid), Dimensions (decimal x3), ... | 1:M PalletLine | SSCC index; Weight validated; Owner-linked. |
| PalletLine | Id (Guid), PalletId (Guid), MaterialId (Guid), Quantity (decimal), Weight (decimal), BatchNumber (string), ExpiryDate (DateTime), AccountId (Guid), ... | N/A | BatchNumber, ExpiryDate; Owner-linked. |
| Invoice | Id (Guid), AccountId (Guid), InvoiceNumber (string), PeriodStart/End (DateTime), TotalAmount (decimal), TaxAmount (decimal), Status (enum), DueDate (DateTime), ... | 1:M InvoiceLine | Status enum; PDF export. |
| InvoiceLine | Id (Guid), InvoiceId (Guid), MaterialId (Guid), ServiceType (enum), Quantity (decimal), UnitRate (decimal), Amount (decimal), Description (string), Tier (string), ... | M:1 Material (optional) | Amount computed; Tiered. |
| Rate | Id (Guid), AccountId (Guid), ServiceType (enum), UOM (enum), Value (decimal), Tier (string), EffectiveStart/EndDate (DateTime), MinQuantity (decimal), ... | N/A | EffectiveDate range; Tier (TempZone). |
| AuditTrail | Id (Guid), EntityType (string), Action (enum), UserId (Guid), Timestamp (DateTime), Changes (string), RoleAtTime (enum), ... | M:1 User | Immutable; RBAC-audited. |

### Database Schema Considerations
- Indexes: Composite on LocationPath (RoomId + Bay/Row/Col/Level), Appointment times (DockId + Start/End); Partial on DoorOpenDuration; Unique on Pallet.Barcode, Material.Name; AccountId on Inventory/Invoice for billing queries; Role index on AuditTrail for RBAC reporting.
- Projections: Denormalized views (e.g., InventoryByRoom, EnergyUsageByDock, PalletsByReceiving, InvoiceSummaryByAccount, UserPermissionsByRole).
- Migrations: EF Core for versioning; Enforce constraints like Quantity >=0; Add RBAC policy triggers.

## Business Processes

### Core Workflows
1. **Receiving**: Appointment → Check-in (Supplier delivers) → Unload to scale → Weigh pallet → Scan/enter materials (batch) → Generate pallet barcode (SSCC) → PalletLine creation (assign to Account) → Pallet completion → Event for putaway; Accrues receiving handling to Account. RBAC: Operator executes, Admin approves exceptions.
2. **Putaway**: Suggest location → Assign pallet → Update inventory (pallet-linked); Triggers storage charge accrual to Account. RBAC: Operator assigns.
3. **Picking**: Order query (for Account's goods) → Pick list → Barcode scan per item/pallet (batch mode) → Reduce stock → Confirmation; Handling charge per unit/weight to Account. RBAC: Operator scans/confirms.
4. **Shipping/Withdrawal**: Appointment → Manifest (Account's goods) → Load (DockTransaction with door monitoring) → Invoice generation with lines for Account. RBAC: Operator loads, Finance reviews invoice.
5. **Docking/Cargo**: Schedule → Yard assign → Unload/Load → Check-out; Minimize exposure time; Dock fee per hour/appointment to Account. RBAC: Operator manages.
6. **Adjustment/Transfer**: Manual with audit; Pallet-aware; Adjustment fees to Account if applicable. RBAC: Operator initiates, Admin approves.
7. **Billing**: Event-driven accrual during workflows (tied to Account) → Monthly/periodic aggregation → Compute lines (e.g., storage: #pallet-days * rate/zone; blasting: #cycles * lb-rate; refrigeration: base + tiered by temp/duration; handling: receiving/putaway/pick/ship * unit-rate; VAS: kitting/repack/fumigation * quantity/yield; processing/servicing: labor hours * rate; energy: door-open surcharges) → Apply grace periods/taxes → Generate PDF for Account. RBAC: Finance generates/views.
8. **Energy Management**: Real-time tracking of refrigeration load; Alerts for inefficiencies; Surcharge lines to Account if thresholds exceeded. RBAC: Operator views alerts, Finance bills surcharges.
9. **User Management**: Admin assigns roles via UI; Triggers RoleAssignedEvent. RBAC: Admin only.

All async; Scan patterns for accuracy; Automation-first design; Batch scanning for efficiency; RBAC enforced per step.

### CQRS Implementation Examples
- **Command**: `ReceivePalletCommand : IRequest<Guid> { ReceivingId, PalletNumber, Lines: List<MaterialQuantityDto>, Weight, AccountId }`
- **Handler**: Validate totals → Check RBAC (Operator role) → Persist Receiving/Pallet/Lines → Invoke BarcodeGenerationService → Publish Event (accrues receiving handling charge to Account).
- **Query**: `GetDockAvailabilityQuery : IRequest<List<TimeSlotDto>> { DockId, Date }`; Includes energy cost projection; RBAC: Operator read.
- **Command**: `PickMaterialCommand : IRequest { PickId, ScannedBarcode, Quantity, AccountId }` – Validates scan match; Check RBAC (Operator); Accrues pick handling to Account.
- **Command**: `GenerateInvoiceCommand : IRequest<Guid> { AccountId, Period }`
- **Handler**: Query accruals (for Account); Check RBAC (Finance) → BillingService.ComputeLines() → Persist Invoice/Lines → Publish.
- Read models via projections for dashboards (e.g., PendingChargesQuery by Account; RBAC-filtered).

## Technical Architecture

### Backend Implementation
**Structure**:
```
WMS.Solution/
├── WMS.Domain/ (Entities, Services)
├── WMS.Application/ (CQRS, DTOs)
├── WMS.Infrastructure/ (EF, Integrations)
└── WMS.Api/ (Controllers, Program.cs)
```
**DI (Program.cs)**:
```csharp
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddDbContext<WmsDbContext>(options => options.UseSqlServer(connStr));
builder.Services.AddScoped<IDockSchedulingService, DockSchedulingService>();
builder.Services.AddScoped<IEnergyMonitoringService, EnergyMonitoringService>();
builder.Services.AddScoped<IBarcodeGenerationService, BarcodeGenerationService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>(); // For RBAC resolution
builder.Services.AddAuthorization(options => {
    options.AddPolicy("OperatorPolicy", policy => policy.RequireRole("Operator"));
    options.AddPolicy("FinancePolicy", policy => policy.RequireRole("Finance"));
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
});
```
**Controller Example**:
```csharp
[ApiController][Route("api/[controller]")]
[Authorize(Policy = "OperatorPolicy")] // RBAC example
public class ReceivingController : ControllerBase {
    private readonly IMediator _mediator;
    [HttpPost("pallets")] public async Task<IActionResult> ReceivePallet(ReceivePalletCommand cmd) 
        => Ok(await _mediator.Send(cmd));
}

[ApiController][Route("api/[controller]")]
[Authorize(Policy = "FinancePolicy")]
public class BillingController : ControllerBase {
    private readonly IMediator _mediator;
    [HttpPost("invoices")] public async Task<IActionResult> Generate(GenerateInvoiceCommand cmd) 
        => Ok(await _mediator.Send(cmd));
}
```
**DbContext**: Fluent API configs for relationships (e.g., HasOne<PalletLine>().WithMany()); Interceptors for energy logging, barcode audits, and RBAC role capture in AuditTrail; Accrual tracking for billing per Account.

**BillingService Example**:
```csharp
public class BillingService : IBillingService {
    public List<InvoiceLine> ComputeLines(Guid accountId, DateTime period, List<AccrualDto> accruals) {
        var account = GetAccount(accountId); // Customer-specific
        var lines = new List<InvoiceLine>();
        foreach (var accrual in accruals) {
            var rate = GetRate(account, accrual.ServiceType, accrual.Tier); // e.g., Blasting, -20°C
            var amount = accrual.Quantity * rate.Value; // e.g., cycles * $1.25/100lbs
            lines.Add(new InvoiceLine { ServiceType = accrual.ServiceType, Amount = amount, ... });
        }
        // Apply grace, taxes; Return lines
        return lines;
    }
}
```

**AuthorizationService Example**:
```csharp
public class AuthorizationService : IAuthorizationService {
    public bool CanExecute(User user, string action, Guid? accountId = null) {
        // Resolve based on Role enum and context (e.g., Finance for Account-specific billing)
        return user.Role switch {
            Role.Admin => true,
            Role.Operator => action.StartsWith("Operate"), // e.g., Receive, Pick
            Role.Finance => action.StartsWith("Bill") || (accountId.HasValue && user.CompanyId == GetAccountOwner(accountId.Value)),
            _ => false
        };
    }
}
```

### Frontend Implementation
**Structure**:
```
src/app/
├── components/ (ReceivingComponent, DockSchedulerComponent, EnergyDashboardComponent, PalletScannerComponent, InvoiceViewerComponent, UserManagementComponent)
├── services/ (DockService, InventoryService, EnergyService, BarcodeService, BillingService, AuthService)
├── models/ (DockAppointmentDto, EnergyMetricDto, PalletDto, InvoiceDto, RolePermissionDto)
└── routes.ts (Lazy: /docks, /inventory, /receiving, /energy, /billing, /admin/users; Guards for RBAC)
```
**Service Example**:
```typescript
@Injectable({providedIn: 'root'})
export class BillingService {
  constructor(private http: HttpClient) {}
  generate(cmd: GenerateInvoiceCommand): Observable<Guid> {
    return this.http.post<Guid>('/api/billing/invoices', cmd);
  }
  getInvoicesForAccount(accountId: Guid): Observable<InvoiceDto[]> {
    return this.http.get<InvoiceDto[]>(`/api/billing/invoices?accountId=${accountId}`);
  }
}

@Injectable({providedIn: 'root'})
export class AuthService {
  constructor(private http: HttpClient) {}
  hasRole(role: Role): boolean {
    return this.currentUser?.role === role; // From JWT claims
  }
  canAccess(action: string, accountId?: Guid): boolean {
    // Call backend or local resolve
    return this.http.get<boolean>(`/api/auth/can-execute?action=${action}&accountId=${accountId}`).pipe(map(res => res));
  }
}
```
**UI**: MatTable for lists; Calendar for scheduling; Real-time via SignalR; ZXing for barcode scanning; Charts for energy trends; Batch scan UI for pallets; Invoice table with line breakdowns (per material/service, filtered by Account); Role-based menu hiding (e.g., Billing tab for Finance); Guards on routes (e.g., AdminGuard for user management).

### Integration Layer
- **Scale**: `ScaleService` – HTTP to device API; Post-weigh triggers barcode gen.
- **Dock Sensors**: `DockSensorService` – Temp/door status via MQTT; Cold-proofed endpoints.
- **ERP**: Stubbed async queue; Invoice export per Account.
- **AS/RS**: `AutomationService` – API for crane/robot coordination; Fallback to manual.
- **Label Printer**: `BarcodePrinterService` – ZPL/HTTP to printer for SSCC labels post-receiving.

## Security and Compliance

### Authentication and Authorization
The system implements robust Role-Based Authentication (RBAC) using JWT Bearer tokens, integrated with ASP.NET Core Identity for user management. Authentication occurs via login endpoint, generating JWTs with claims for UserId, CompanyId (tenant), Role (enum), and expiration. Multi-factor authentication (MFA) is enforced for Admin roles using Authenticator apps.

**RBAC Details**:
- **Roles**: Defined in User.Role enum (Admin, Operator, Finance). Roles are assigned via Admin UI workflow, triggering RoleAssignedEvent and audit log.
- **Authorization Policies**: Configured in Program.cs with policy-based requirements (e.g., "OperatorPolicy" requires Role.Operator). Handlers invoke AuthorizationService.CanExecute() for fine-grained checks (e.g., Operator can pick for any Account, but Finance scoped to company-owned Accounts).
- **Permissions Matrix**:

| Role | Receiving/Putaway/Picking/Shipping | Docking/Yard | Inventory Adjustments | Billing/Invoicing | Admin/Setup | Energy Monitoring |
|------|------------------------------------|--------------|-----------------------|-------------------|-------------|-------------------|
| **Admin** | Full (Create/Update/Delete) | Full | Full | Full | Full (Users/Roles) | Full |
| **Operator** | Execute (Create/Confirm) | Schedule/Status Update | Initiate (w/ Approval) | View (Own Actions) | None | View Alerts |
| **Finance** | View (Logs) | View | View | Full (Generate/View) | View Users | View Reports/Bill Surcharges |

- **Enforcement**: API controllers use [Authorize(Policy = "...")]; CQRS handlers check via AuthorizationService; Frontend uses AuthService guards and role-based UI visibility. Tenant isolation via CompanyId claim prevents cross-tenant access. Account-scoped queries filter by ownership (e.g., only view invoices for assigned Accounts).
- **Token Refresh**: Sliding expiration (15min idle); Revocation on role change or logout.
- **Compliance Integration**: RBAC logs in AuditTrail for ISO 22000 traceability; MFA aligns with FDA security for pharma handling.

### Audit and Traceability
- EF Interceptors for changes → AuditTrail (includes RoleAtTime for RBAC context).
- Cold chain: Lot isolation on expiry; Door-open logs; FDA traceability reports; Barcode scan logs for picks; Invoice change logs per Account.
- RBAC-Specific: All role assignments and permission denials logged; Quarterly reports for compliance.

## Deployment and Operations

### Deployment Model
- Docker Compose for local; Kubernetes for prod; Helm charts for energy modules and RBAC policies.
- CI/CD: GitHub Actions → Azure; Security scans for JWT vulnerabilities.

### Monitoring and Scalability
- Serilog + Application Insights; Custom metrics for energy usage, temp deviations, scan success rates, billing accuracy (e.g., line count variances per Account), and RBAC enforcement (e.g., denied access rate <1%).
- CQRS scales reads independently; Cache hot queries in Redis; Auto-scale based on dock activity and invoice generation peaks; Alert on RBAC anomalies (e.g., unauthorized billing attempts).

## Appendices

### A. Text-Based Diagrams
**Aggregate Example (Receiving)**:
```
Receiving (Root)
└── Pallets [1..*] (AccountId)
    └── PalletLines [1..*] (Material, Quantity, AccountId)
```

**Receiving Flow**:
```
Scale Weigh → Enter/Scan Materials → Generate SSCC Barcode → Print Label → Putaway (Assign to Account) [RBAC: Operator]
```

**Aggregate Example (Invoice)**:
```
Invoice (Root, for AccountId)
└── Lines [1..*] (ServiceType: Storage/Blasting/Handling/VAS, MaterialId, Amount)
```

**Aggregate Example (Warehouse)**:
```
Warehouse (Root)
├── Rooms [1..*]
│   └── Locations [1..*] (AS/RS Integrated)
├── Docks [1..*]
│   └── Appointments [1..*] (Energy Monitored)
└── YardSpots [1..*]
```

**Energy Flow**:
```
Dock Open → Sensor Alert → Event Bus → EnergyService → Dashboard/Invoice Adjustment (to Account) [RBAC: Operator View, Finance Bill]
```

**Billing Flow**:
```
Workflow Event (e.g., Blast Cycle for Account's Goods) → Accrual → Periodic Compute → Invoice Lines → PDF (to Account) [RBAC: Finance]
```

**RBAC Flow**:
```
Login → JWT Claims (Role, CompanyId) → Policy Check → Execute Command/Query → Audit Log (RoleAtTime)
```

### B. Glossary
- **Aggregate**: DDD cluster of entities treated as a unit.
- **CQRS**: Separates commands/queries for optimized scalability.
- **AS/RS**: Automated Storage and Retrieval System for efficient cold storage density.
- **SSCC**: Serial Shipping Container Code for pallet barcodes.
- **VAS**: Value-Added Services (e.g., kitting, blasting).
- **Account**: Customer/client entity billed for WMS services (e.g., storage of their goods); Distinct from Supplier (inbound vendor).
- **RBAC**: Role-Based Access Control, enforcing permissions via user roles.

### C. References
- Internal: Prior specs.
- External: .NET Documentation; Angular Releases; Cold Storage Best Practices; WMS Receiving Guides; Barcode Implementation; 3PL Billing Guides; ASP.NET Core Identity for RBAC.

This document is the definitive blueprint for implementation. For code generation, reference the original prompt's step-by-step plan.