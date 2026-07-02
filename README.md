# SAP Master Data Automation

ASP.NET Core MVC application for automating customer master data approval workflow and SAP upload file generation.

## Overview

This system helps sales teams create or upload customer master data, submit it to managers for review, and generate SAP-ready Excel upload files after approval.

## Main Features

- Customer creation from web form
- Customer upload from Excel sheet
- Draft customer management
- Submit customers to manager
- Manager approval workflow
- Approve or reject individual customers
- Rejection reason tracking
- Approve all submitted customers
- Delete approved customers before SAP generation
- Generate SAP upload Excel file
- Generated files history
- Download old generated SAP files
- Simple role-based access for Sales, Manager, and Admin

## Roles

### Sales

- Create customers
- Upload Excel sheets
- Manage draft customers
- Submit customers to manager
- Review rejected customers and rejection reasons

### Manager

- Review submitted customers
- Approve or reject customers
- Approve all submitted customers
- Delete approved customers before generation
- Generate SAP Excel file
- View generation history

### Admin

- Manage users
- Manage system settings
- Update last BP code

## Technology Stack

- ASP.NET Core MVC
- C#
- Entity Framework Core
- SQL Server
- ClosedXML
- Bootstrap
- Git & GitHub

## Project Status

MVP completed with a working approval workflow and SAP Excel generation.
