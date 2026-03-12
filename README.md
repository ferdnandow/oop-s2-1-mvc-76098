Community Library Desk

A simple internal library management system built with ASP.NET Core MVC.

Tech Stack
- ASP.NET Core MVC (.NET 10)
- EF Core + SQLite
- ASP.NET Identity
- xUnit Tests
- GitHub Actions CI

How to Run
1- Clone the repository
2- Navigate to the project folder
3- Run the following commands:
```bash
cd Library.MVC
dotnet ef database update
dotnet run
```

4- Open your browser and go to `http://localhost:5208`

Admin Login
- **Email:** admin@library.com
- **Password:** Admin123!

Features
- Books: list, create, edit, delete, search and filter
- Members: list, create, edit, delete
- Loans: create, mark as returned, overdue tracking
- Admin Role Management page

Running Tests
```bash
cd Library.Tests
dotnet test
```