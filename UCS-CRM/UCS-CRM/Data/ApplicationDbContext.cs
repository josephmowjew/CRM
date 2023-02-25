using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UCS_CRM.Core.Models;

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


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
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
