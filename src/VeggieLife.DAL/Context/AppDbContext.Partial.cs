using Microsoft.EntityFrameworkCore;

namespace VeggieLife.DAL.Context;

public partial class AppDbContext
{
    // Custom configurations, global query filters, soft-delete rules, enum converters...
    // All custom extensions should be placed here because AppDbContext.cs will be overwritten on each scaffold.
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Custom partial configurations
    }
}
