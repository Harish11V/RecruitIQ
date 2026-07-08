# RecruitIQ

> **Enterprise AI-Powered Applicant Tracking System (ATS)**

RecruitIQ is a production-style Applicant Tracking System built to
demonstrate enterprise software engineering practices using **ASP.NET
Core**, **Angular 20**, **Clean Architecture**, and **CQRS**. The
application is designed with scalability in mind and is planned to
evolve into an AI-powered recruitment platform.

------------------------------------------------------------------------

# Features

## Authentication

-   JWT Authentication
-   Refresh Tokens
-   Role-Based Authorization
-   Route Guards & HTTP Interceptors

## Enterprise Backend

-   Clean Architecture
-   CQRS with MediatR
-   Entity Framework Core
-   SQL Server
-   FluentValidation
-   Multi-Tenant Architecture
-   Activity Logging
-   Optimistic Concurrency (RowVersion)
-   Soft Delete
-   xUnit Integration Tests

## Angular Frontend

-   Angular 20
-   Standalone Components
-   Angular Signals
-   Angular Material
-   Reactive Forms
-   Responsive Admin Portal
-   Reusable Design System

## Job Management (Completed)

-   Job Listing
-   Create Job
-   View Job Details
-   Edit Job
-   Publish Job
-   Archive Job
-   Soft Delete
-   Skeleton Loading
-   Responsive UI
-   Optimistic Concurrency Handling

------------------------------------------------------------------------

# Technology Stack

  Layer            Technology
  ---------------- ------------------------
  Backend          ASP.NET Core (.NET 10)
  Frontend         Angular 20
  Database         SQL Server
  ORM              Entity Framework Core
  Architecture     Clean Architecture
  Pattern          CQRS + MediatR
  Authentication   JWT
  UI               Angular Material
  Testing          xUnit

------------------------------------------------------------------------

# Architecture

``` text
Angular 20
        │
REST API
        │
ASP.NET Core
        │
Application (CQRS)
        │
Domain
        │
Infrastructure
        │
SQL Server
```

------------------------------------------------------------------------

# Project Structure

``` text
RecruitIQ
│
├── src/
│   ├── RecruitIQ.API
│   ├── RecruitIQ.Application
│   ├── RecruitIQ.Contracts
│   ├── RecruitIQ.Domain
│   ├── RecruitIQ.Infrastructure
│   └── RecruitIQ.Persistence
│
├── tests/
│   └── RecruitIQ.IntegrationTests
│
└── recruitiq-ui/
    └── src/app/
        ├── core/
        ├── layout/
        ├── shared/
        └── features/
```

------------------------------------------------------------------------

# Current Status

## Completed

-   Backend Foundation
-   Authentication
-   Company Administration APIs
-   Department APIs
-   Complete Job Management Backend
-   Angular Architecture
-   Design System
-   Authentication UI
-   Admin Shell
-   Complete Job Management UI

## In Progress

-   Company Settings UI

## Planned

-   Department Management UI
-   Candidate Management
-   Applications
-   Interview Pipeline
-   AI Resume Parsing
-   AI Candidate Ranking

------------------------------------------------------------------------

# Roadmap

## Backend

-   [x] Authentication
-   [x] Departments
-   [x] Company Settings APIs
-   [x] Job Management

## Frontend

-   [x] Authentication
-   [x] Admin Shell
-   [x] Job Management
-   [ ] Company Settings
-   [ ] Departments
-   [ ] Candidate Management

## AI Features

-   [ ] Resume Parsing
-   [ ] Candidate Ranking
-   [ ] Skill Matching
-   [ ] AI Interview Recommendations

------------------------------------------------------------------------

# Getting Started

## Backend

``` bash
git clone <repository-url>
cd RecruitIQ
dotnet restore
dotnet build
dotnet run
```

## Frontend

``` bash
cd recruitiq-ui
npm install
ng serve
```

------------------------------------------------------------------------

# Enterprise Highlights

-   Multi-Tenant Architecture
-   Optimistic Concurrency (RowVersion)
-   Activity Logging
-   Role-Based Authorization
-   JWT Authentication
-   Refresh Tokens
-   Angular Signals
-   Standalone Components
-   Responsive Enterprise UI
-   Reusable Component Architecture

------------------------------------------------------------------------

# License

This project is intended as a portfolio and learning project
demonstrating enterprise application architecture and modern full-stack
development practices.
