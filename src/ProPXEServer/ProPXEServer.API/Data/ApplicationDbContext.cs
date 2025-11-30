using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ProPXEServer.API.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser> {
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options) {
    }
    
    public DbSet<BootFile> BootFiles => Set<BootFile>();
    public DbSet<PxeEvent> PxeEvents => Set<PxeEvent>();
    public DbSet<BootConfiguration> BootConfigurations => Set<BootConfiguration>();
    
    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);
        
        builder.Entity<BootFile>()
            .HasIndex(b => b.UserId);
        
        builder.Entity<PxeEvent>()
            .HasIndex(p => p.Timestamp);
        
        builder.Entity<PxeEvent>()
            .HasIndex(p => p.MacAddress);
        
        builder.Entity<BootConfiguration>()
            .HasIndex(bc => bc.MacAddress)
            .IsUnique();
    }
}


