using Microsoft.EntityFrameworkCore;
using RiftVeil.Domain.Entities;

namespace RiftVeil.Infrastructure.Data;

public class RiftVeilDbContext(DbContextOptions<RiftVeilDbContext> options) : DbContext(options)
{
    public DbSet<DbSmokeTest> DbSmokeTest => Set<DbSmokeTest>();
}
