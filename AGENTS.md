# AGENTS.md

This document describes the agent(s) or services included in this repository, including their purpose, runtime environment, dependencies, and how to run them.

---

## 🌐 Agent: JobAppRazorWeb

**Purpose**:  
A web-based job tracking application built with ASP.NET Core Razor Pages. It likely handles job listings, application tracking, and related workflows.

**Runtime**:
- .NET 9.0 (target framework: `net9.0`)
- Docker (Linux OS target configured)

**Dependencies**:
- `System.Data.SQLite.Core` (v1.0.119): Provides embedded SQLite database support.

**Startup Instructions**:

```bash
# Restore dependencies
dotnet restore

# Run the application
dotnet run --project JobAppRazorWeb/JobAppRazorWeb.csproj
