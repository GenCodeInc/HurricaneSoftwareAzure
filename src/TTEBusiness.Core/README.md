# TTEBusiness.Core

This is the new reusable .NET 8 library for cloud-hosted TTE workloads.

Current scope:

- reusable NHC parser orchestration
- HTTP fetch abstraction
- advisory classification logic
- placeholder processing pipeline for Azure-hosted execution

Still to migrate from the existing `TTEBusiness` implementation:

- database access currently implemented through DBML and EDMX designers
- SMTP/config access that currently depends on `.config`, `System.Web`, and `HttpContext`
- persistence and business operations currently hidden behind stored procedure wrappers

This project intentionally does not modify the existing production codebase.