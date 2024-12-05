using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Buffers;
using UCS_CRM.Core.Models;
using UCS_CRM.Core.Services;
using UCS_CRM.Data;
using UCS_CRM.Persistence.Interfaces;
using UCS_CRM.Persistence.SQLRepositories;
using Hangfire;
using Hangfire.MemoryStorage;
using UCS_CRM.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Authentication;


var builder = WebApplication.CreateBuilder(args);

var provider = builder.Configuration["ServerSettings:ServerName"];
string mySqlConnectionStr = builder.Configuration.GetConnectionString("MySqlConnection");

builder.Services.AddDbContext<ApplicationDbContext>(
options => _ = provider switch
{
    "MySQL" => options.UseMySql(mySqlConnectionStr, ServerVersion.AutoDetect(mySqlConnectionStr),
 b => b.SchemaBehavior(MySqlSchemaBehavior.Ignore)),
    _ => throw new Exception($"Unsupported provider: {provider}")
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser,Role>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequireNonAlphanumeric = true;
})  .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Auth/Create";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHangfire(config =>
           config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
           .UseSimpleAssemblyNameTypeSerializer()
           .UseDefaultTypeSerializer()
           .UseMemoryStorage());


builder.Services.AddRazorPages()
    .AddRazorRuntimeCompilation();
    
builder.Services.AddHangfireServer();

builder.Services.AddControllersWithViews().AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddScoped<IAccountTypeRepository, AccountTypeRepository>();
builder.Services.AddScoped<ITicketCategoryRepository, TicketCategoryRepository>();
builder.Services.AddScoped<ITicketPriorityRepository, TicketPriorityRepository>();
builder.Services.AddScoped<IEmailRepository, EmailRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddScoped<IRoleRepositorycs, RoleRepository>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IStateRepository, StateRepository>();
builder.Services.AddScoped<ITicketCommentRepository, TicketCommentRepository>();
builder.Services.AddScoped<ITicketEscalationRepository, TicketEscalationRepository>();
builder.Services.AddScoped<IMemberAccountRepository, MemberAccountRepository>();
builder.Services.AddScoped<IEmailAddressRepository, EmailAddressRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<IErrorLogRepository, ErrorLogRepository>();
builder.Services.AddScoped<IErrorLogService, ErrorLogService>();
builder.Services.AddScoped<ITicketStateTrackerRepository, TicketStateTrackerRepository>();
builder.Services.AddScoped<IFailedRegistrationRepository, FailedRegistrationRepository>();
//builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IFintechMemberService,FintechMemberService>();
builder.Services.AddSingleton<HangfireJobEnqueuer>();
builder.Services.AddSingleton<IErrorLogServiceFactory, ErrorLogServiceFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient(); 

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.UseUserInactivity();

app.MapControllerRoute(
   name: "areas",
   pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Create}/{id?}");

app.MapRazorPages();

app.UseHangfireDashboard();

using var scope = app.Services.CreateScope();
ITicketRepository ticket = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
IFintechMemberService fintechMemberService = scope.ServiceProvider.GetRequiredService<IFintechMemberService>();

RecurringJob.AddOrUpdate("SyncFintechMemberRecords", () => fintechMemberService.SyncFintechMembersWithLocalDataStore(), Cron.HourInterval(1));
RecurringJob.AddOrUpdate<IFintechMemberService>("SyncMissingFintechMembers-Manual", 
    x => x.SyncMissingFintechMembers(null, null, CancellationToken.None), 
    Cron.Never());
RecurringJob.AddOrUpdate("UnassignedTicketsCheck", () => ticket.UnAssignedTickets(), Cron.HourInterval(1));
RecurringJob.AddOrUpdate("TicketReminders", () => ticket.SendTicketReminders(), Cron.HourInterval(1));

app.Run();

public class UserInactivityMiddleware
{
    private readonly RequestDelegate _next;

    public UserInactivityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity.IsAuthenticated)
        {
            var lastActivity = context.Session.GetString("LastUserActivity");
            var currentTime = DateTime.Now;

            if (string.IsNullOrEmpty(lastActivity) || 
                (currentTime - DateTime.Parse(lastActivity)).TotalMinutes > 30)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                context.Response.Redirect("/Auth/Create");
                return;
            }

            context.Session.SetString("LastUserActivity", currentTime.ToString());
        }

        await _next(context);
    }
}

public static class UserInactivityMiddlewareExtensions
{
    public static IApplicationBuilder UseUserInactivity(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserInactivityMiddleware>();
    }
}