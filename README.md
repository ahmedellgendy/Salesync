# Salesync

![.NET](https://img.shields.io/badge/.NET-9-purple)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET-Core-blue)
![EF Core](https://img.shields.io/badge/EF-Core-green)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-red)
![Architecture](https://img.shields.io/badge/Architecture-Onion-orange)
![License](https://img.shields.io/badge/License-MIT-lightgrey)

# Salesync

Enterprise Sales Force Automation (SFA) Backend System built with ASP.NET Core using Onion Architecture.

## Overview

Salesync is a backend system designed to automate sales operations for distribution companies.

The system manages:

- Sales Representatives
- Customers
- Products
- Warehouses
- Daily Sales Sessions
- Invoices
- Payments
- Returns
- Authentication & Authorization

The project is designed to be consumed by:

- Mobile Application (Sales Representatives)
- Web Dashboard (Admin & Supervisors)

---

# Architecture

The project follows Onion Architecture.

```
Presentation (API)
        ↓
Application
        ↓
Domain
        ↓
Infrastructure
```

Projects

```
Salesync.API
Salesync.Application
Salesync.Domain
Salesync.Infrastructure
```

---

# Technologies

- ASP.NET Core Web API
- .NET 9
- Entity Framework Core
- SQL Server
- ASP.NET Identity
- JWT Authentication
- AutoMapper
- FluentValidation
- Repository Pattern
- Unit Of Work
- Onion Architecture

---

# Features

## Authentication

- JWT Authentication
- ASP.NET Identity
- Role Based Authorization

Roles

- Admin
- Supervisor
- SalesRep

---

# Master Data

- Branches
- Warehouses
- Products
- Customers

---

# Sales Representative

- Start Session
- Close Session
- Session Summary
- Assigned Routes

---

# Sales Module

## Invoice

- Create Invoice
- Confirm Invoice
- Cancel Invoice

Business Rules

- Active Customer only
- Active Warehouse only
- Active Products only
- SalesRep Session validation
- Automatic Invoice Number
- Automatic Invoice Totals

---

## Payments

- Payment only for Confirmed Invoice
- Prevent Over Payment
- Partial Payment
- Full Payment
- SalesRep Validation

---

## Invoice Returns

- Return using InvoiceItem
- Prevent invalid products
- Prevent returning more than sold quantity
- Approve Return
- Reject Return

---

# Session Summary

Closing a SalesRep session automatically calculates:

- Gross Sales
- Net Sales
- Total Collection
- Total Return Amount
- Total Confirmed Invoices

---

# Security

- JWT Authentication
- Role Based Authorization
- Business Rule Validation
- FluentValidation
- Current User Context

---

# Project Structure

```
Salesync
│
├── Salesync.API
│
├── Salesync.Application
│   ├── DTOs
│   ├── Interfaces
│   ├── Services
│   ├── Validators
│
├── Salesync.Domain
│   ├── MasterData
│   ├── Sales
│   ├── SalesRep
│
├── Salesync.Infrastructure
│   ├── Identity
│   ├── Configurations
│   ├── Repositories
│   ├── Context
│
```

---

# API Modules

## Identity

- Login
- JWT
- Roles

## Master Data

- Customers
- Products
- Warehouses
- Branches

## Sales

- SalesRepSession
- Invoice
- Payment
- InvoiceReturn

---

# Future Roadmap

- Inventory Module
- Load Requests
- Stock Movements
- Mobile Synchronization
- Dashboard & Reports
- Notifications
- Offline Mobile Sync
- SAP Integration

---

# Author

Ahmed Elgendy

Backend Developer (.NET)

GitHub

https://github.com/ahmedellgendy
