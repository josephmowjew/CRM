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

builder.Services.AddIdentity<ApplicationUser,IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequireNonAlphanumeric = true;
    
   
})  .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders();



//configure services
builder.Services.AddControllersWithViews().AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddScoped<IAccountTypeRepository, AccountTypeRepository>();
builder.Services.AddScoped<ITicketCategoryRepository, TicketCategoryRepository>();
builder.Services.AddScoped<ITicketPriorityRepository, TicketPriorityRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRoleRepositorycs, RoleRepository>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IStateRepository, StateRepository>();
builder.Services.AddScoped<ITicketCommentRepository, TicketCommentRepository>();
builder.Services.AddScoped<ITicketEscalationRepository, TicketEscalationRepository>();
builder.Services.AddScoped<IMemberAccountRepository, MemberAccountRepository>();


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

app.Run();
