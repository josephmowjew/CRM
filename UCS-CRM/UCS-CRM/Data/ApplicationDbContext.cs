using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using UCS_CRM.Core.Models;
using UCS_CRM.Models;

namespace UCS_CRM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
           
        }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketCategory> TicketCategories { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<MemberAccount> MemberAccounts { get; set; }
        public DbSet<AccountType> AccountTypes { get; set; }
        public DbSet<TicketComment> TicketComments { get; set; }
        public DbSet<TicketPriority> TicketPriorities { get; set; }
        public DbSet<TicketAttachment> TicketAttachments { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<TicketEscalation> TicketEscalations { get; set; } 
        public DbSet<EmailAddress> EmailAddresses { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<TicketStateTracker> TicketStateTrackers { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<SystemDateConfiguration> SystemDateConfigurations { get; set; }
        public DbSet<FailedRegistration> FailedRegistrations { get; set; }
        public DbSet<WorkingHours> WorkingHours { get; set; }




        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>()
              .HasOne(a => a.Member)
              .WithOne(i => i.User)
              .HasForeignKey<ApplicationUser>(b => b.MemberId);

            builder.Entity<ApplicationUser>()
            .HasMany(a => a.Members)
            .WithOne(i => i.CreatedBy);

            //manual annotations for department
            builder.Entity<ApplicationUser>()
            .HasOne(i => i.Department)
            .WithMany(d => d.Users);
            
            builder.Entity<ApplicationUser>()
                .HasMany(u => u.Departments)
                .WithOne( i => i.CreatedBy);

            //manual annotations for branch

            builder.Entity<ApplicationUser>()
                .HasOne(i => i.Branch)
                .WithMany(d => d.Users);

            builder.Entity<ApplicationUser>()
                .HasMany(u => u.Branches)
                .WithOne(i => i.CreatedBy);

            builder.Entity<Member>()
            .HasIndex(m => m.Fidxno)
            .IsUnique();

            //manual annotation for position
            //builder.Entity<ApplicationUser>()
            //   .HasOne(i => i.Position)
            //   .WithMany(d => d.Users);

            builder.Entity<ApplicationUser>()
                .HasMany(u => u.Escalations)
                .WithOne(i => i.EscalatedTo);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable(name: "Users");
                entity.Property(u => u.NormalizedEmail).HasMaxLength(200);
                entity.Property(u => u.Id).HasMaxLength(200);
                entity.Property(u => u.NormalizedUserName).HasMaxLength(200);
                entity.Property(u => u.UserName).IsUnicode(false);
                entity.Property(u => u.Email).IsUnicode(false);
                entity.Property(u => u.ConcurrencyStamp).HasMaxLength(200);
            });
            builder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable(name: "Roles");
                entity.Property(u => u.NormalizedName).HasMaxLength(85);
                entity.Property(u => u.Id).HasMaxLength(85);
            });
            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.Property(u => u.UserId).HasMaxLength(85);
                entity.Property(u => u.RoleId).HasMaxLength(85);
            });
            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaims");
                entity.Property(u => u.UserId).HasMaxLength(200);
            });
            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogins");
                entity.Property(u => u.UserId).HasMaxLength(200);
                entity.Property(m => m.LoginProvider).HasMaxLength(85);
                entity.Property(m => m.ProviderDisplayName).HasMaxLength(85);
                entity.Property(m => m.LoginProvider).HasMaxLength(85);
                entity.Property(m => m.ProviderKey).HasMaxLength(85);
            });
            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("RoleClaims");
                entity.Property(u => u.RoleId).HasMaxLength(200);
            });
            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserTokens");
                entity.Property(u => u.UserId).HasMaxLength(85);
                entity.Property(u => u.Name).HasMaxLength(85);
                entity.Property(u => u.LoginProvider).HasMaxLength(85);
               
            });
        }


    }
}
