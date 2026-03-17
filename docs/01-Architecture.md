\# 01 - Architecture



\## Constitution

1\. Web and API are separate projects.

2\. WPF only calls API.

3\. Web uses Cookie auth.

4\. API and WPF use JWT + Refresh Token.

5\. SQL Server + EF Core Code First + migrations.

6\. MVC is the main web shell.

7\. Razor Pages are for page-centric transactional flows.

8\. Blazor is only for local interactive widgets/components.

9\. Built-in DI is the default.

10\. Async end-to-end.

11\. Business logic must not live in controllers, PageModels, or WPF ViewModels.

12\. File metadata is stored in DB; physical files are stored on local disk.



\## Project Goal

Academic Management system for a single school.



\## Core Architecture

\- WebApp and API are separate projects.

\- WPF AdminApp only calls API, never accesses DB directly.

\- Clean Architecture:

&#x20; - SharedKernel

&#x20; - Contracts

&#x20; - Domain

&#x20; - Application

&#x20; - Infrastructure

&#x20; - Web

&#x20; - Api

&#x20; - AdminApp



\## Auth Strategy

\- Web uses Cookie Authentication.

\- API and WPF use JWT + Refresh Token.

\- Permission-based authorization.



\## Data Strategy

\- SQL Server

\- EF Core Code First + migrations

\- metadata file stored in DB

\- physical file stored on local disk

\- rowversion for sensitive concurrency cases

\- soft delete + query filters where appropriate



\## UI Strategy

\- MVC = main web framework

\- Razor Pages = transactional/page-centric flows

\- Blazor = local interactive components only

\- WPF = internal admin/staff client

