# UCS-CRM (United Civil Society - Customer Relationship Management)

UCS-CRM is a web-based customer support and relationship management system developed using Microsoft ASP.NET Core MVC with MySQL database using Pomelo.EntityFrameworkCore.MySql.

## Functionality

UCS-CRM provides the following core functionality:

1. **Departments Management:**
   - The system supports managing departments.
   - Departments are rated based on their performance in resolving tickets.

2. **Branches Management:**
   - Support for managing branches within the organization.

3. **Cross-Branch Ticket Handling:**
   - Authorized personnel can generate and view tickets for members from different branches.

4. **Escalation Paths:**
   - Two escalation paths are supported: from junior to senior manager and within departments.

5. **Ticket Creation:**
   - Users are disabled from setting priority when creating a ticket.
   - Additional information about a user is shown apart from the name when a clerk creates a ticket on behalf of the member.

6. **Ticket Management:**
   - Clerks can transfer assigned tickets to other clerks.
   - Tickets can be reopened.

## Technologies Used

UCS-CRM is developed using the following technologies:

- ASP.NET Core MVC
- Entity Framework Core (for data access)
- Pomelo.EntityFrameworkCore.MySql (for MySQL database integration)
- HTML, CSS, and JavaScript (for front-end development)

## Requirements

UCS-CRM has the following requirements:

- .NET SDK
- MySQL Server
- Pomelo.EntityFrameworkCore.MySql
- AutoMapper.Extensions.Microsoft.DependencyInjection
- Bogus
- EPPlus
- GravatarHelper.AspNetCore
- Hangfire
- Hangfire.MemoryStorage
- iTextSharp
- MailKit
- Microsoft.AspNet.Mvc
- Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
- Microsoft.AspNetCore.Identity.EntityFrameworkCore
- Microsoft.AspNetCore.Identity.UI
- Microsoft.AspNetCore.Mvc.NewtonsoftJson
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Tools
- Microsoft.VisualStudio.Web.CodeGeneration.Design
- MimeKit
- SendGrid
- System.Linq.Async
- System.Linq.Dynamic.Core
- System.Net.Http


## Getting Started

To run UCS-CRM locally, follow these steps:

1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/UCS-CRM.git
   ```
2. Navigate to the project directory:
   ```bash
   cd UCS-CRM
   ```
3. Set up the database:

- Ensure you have access to a MySQL database.
- Update the connection string in appsettings.json to point to your MySQL database instance.
- Run Entity Framework Core migrations to create the database schema:

   ```bash
   dotnet build
  dotnet run
    ```
 4. Access UCS-CRM in your web browser at http://localhost:5000

## License

This project is licensed under the MIT License.