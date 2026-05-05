using DrugRegistry.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace DrugRegistry.API.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<Pharmacy> Pharmacies => Set<Pharmacy>();
    public DbSet<DrugEanData> DrugEanData => Set<DrugEanData>();
}