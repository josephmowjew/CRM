# CRM (Customer Relationship Management)

A web-based customer relationship management system developed using ASP.NET Core MVC with MySQL database.

## Core Features

1. **User Management:**
   - Role-based access control
   - User authentication and authorization
   - Profile management

2. **Ticket Management:**
   - Create and track support tickets
   - Ticket assignment and escalation
   - Status tracking and updates
   - Cross-branch ticket handling

3. **Department Management:**
   - Manage different departments
   - Department-based ticket routing
   - Performance tracking

4. **Branch Management:**
   - Multi-branch support
   - Branch-specific ticket handling
   - Cross-branch operations

5. **Escalation System:**
   - Hierarchical escalation paths
   - Department-based escalation
   - Manager oversight

## Technologies Used

- ASP.NET Core MVC
- Entity Framework Core
- MySQL Database
- Bootstrap for UI
- jQuery and JavaScript
- MailKit for email notifications

## Requirements

- .NET SDK (Latest version)
- MySQL Server
- Modern web browser
- SMTP server for email notifications

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/josephmowjew/CRM.git
   ```

2. Navigate to the project directory:
   ```bash
   cd CRM
   ```

3. Configure the database:
   - Update the connection string in `appsettings.json`
   - Ensure MySQL server is running
   - Update email settings in `appsettings.json` if needed

4. Run the application:
   ```bash
   dotnet build
   dotnet run
   ```

5. Access the application:
   - Open your browser and navigate to `http://localhost:5000`
   - Default credentials will be provided by your system administrator

## Configuration

Key configuration files:
- `appsettings.json`: Database connection, email settings, and API credentials
- `Program.cs`: Application startup and service configuration
- `appsettings.Development.json`: Development-specific settings

## Support

For support issues:
- Contact system administrator
- Email: support@yourdomain.com
- Phone: Your support phone number

## License

This project is proprietary and confidential. Unauthorized copying or distribution is prohibited.