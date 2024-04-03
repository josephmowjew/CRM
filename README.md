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
- Pomelo.EntityFrameworkCore.MySql (version 7.0.0)
- AutoMapper.Extensions.Microsoft.DependencyInjection (version 12.0.1)
- Bogus (version 34.0.2)
- EPPlus (version 7.0.0)
- GravatarHelper.AspNetCore (version 1.1.0)
- Hangfire (version 1.8.6)
- Hangfire.MemoryStorage (version 1.8.0)
- iTextSharp (version 5.5.13.3)
- MailKit (version 4.2.0)
- Microsoft.AspNet.Mvc (version 5.3.0)
- Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore (version 7.0.13)
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (version 7.0.13)
- Microsoft.AspNetCore.Identity.UI (version 7.0.13)
- Microsoft.AspNetCore.Mvc.NewtonsoftJson (version 7.0.13)
- Microsoft.EntityFrameworkCore.SqlServer (version 7.0.13)
- Microsoft.EntityFrameworkCore.Tools (version 7.0.13)
- Microsoft.VisualStudio.Web.CodeGeneration.Design (version 7.0.11)
- MimeKit (version 4.2.0)
- SendGrid (version 9.28.1)
- System.Linq.Async (version 6.0.1)
- System.Linq.Dynamic.Core (version 1.3.5)
- System.Net.Http (version 4.3.4)


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