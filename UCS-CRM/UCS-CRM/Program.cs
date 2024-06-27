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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(connectionString));

var provider = builder.Configuration["ServerSettings:ServerName"];
string mySqlConnectionStr = builder.Configuration.GetConnectionString("MySqlConnection");

builder.Services.AddDbContext<ApplicationDbContext>(
options => _ = provider switch
{
    "MySQL" => options.UseMySql(mySqlConnectionStr, ServerVersion.AutoDetect(mySqlConnectionStr),
 b => b.SchemaBehavior(MySqlSchemaBehavior.Ignore)),

    // "SqlServer" => options.UseSqlServer(
    //     Configuration.GetConnectionString("DefaultConnection")),

    _ => throw new Exception($"Unsupported provider: {provider}")
});

//builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser,Role>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequireNonAlphanumeric = true;
    
   
})  .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();

builder.Services.AddHangfire(config =>
           config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
           .UseSimpleAssemblyNameTypeSerializer()
           .UseDefaultTypeSerializer()
           .UseMemoryStorage());

builder.Services.AddHangfireServer();

//configure services
builder.Services.AddControllersWithViews().AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);
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
//builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IFintechMemberService,FintechMemberService>();
builder.Services.AddSingleton<HangfireJobEnqueuer>();
builder.Services.AddSingleton<IErrorLogServiceFactory, ErrorLogServiceFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient(); 


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.MapControllerRoute(
   name: "areas",
            pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    //pattern: "{controller=Home}/{action=Index}/{id?}");
    pattern: "{controller=Auth}/{action=Create}/{id?}");


app.MapRazorPages();

app.UseHangfireDashboard();

using var scope = app.Services.CreateScope();
ITicketRepository ticket = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
IFintechMemberService fintechMemberService = scope.ServiceProvider.GetRequiredService<IFintechMemberService>();


RecurringJob.AddOrUpdate("SyncFintechMemberRecords", () => fintechMemberService.SyncFintechMembersWithLocalDataStore(), Cron.HourInterval(1));
RecurringJob.AddOrUpdate(() => ticket.UnAssignedTickets(), Cron.MinuteInterval(1));
RecurringJob.AddOrUpdate(() => ticket.SendTicketReminders(), Cron.MinuteInterval(1));

//BackgroundJob.Schedule(() => ticket.EscalatedTickets(), TimeSpan.FromHours(1));
//RecurringJob.AddOrUpdate("TicketReminder", () => ticket.SendTicketReminders(), Cron.MinuteInterval(10));
//RecurringJob.AddOrUpdate("EscalatedTickets", () => ticket.SendEscalatedTicketsReminder(), Cron.MinuteInterval(10));


app.Run();
