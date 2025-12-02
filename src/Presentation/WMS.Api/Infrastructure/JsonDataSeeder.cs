using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;
using WMS.Domain.Entities.Transaction;
using WMS.Domain.Aggregates.Warehouse;
using WMS.Infrastructure.Persistence;

namespace WMS.Api.Infrastructure;

public static class JsonDataSeeder
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static async Task ExportDataAsync(WmsDbContext context, string outputPath)
    {
        Directory.CreateDirectory(outputPath);

        await ExportEntity(context.Roles, outputPath, "Roles.json");
        await ExportEntity(context.Users, outputPath, "Users.json");
        await ExportEntity(context.UserRoles, outputPath, "UserRoles.json");
        
        await ExportEntity(context.Companies, outputPath, "Companies.json");
        await ExportEntity(context.Accounts, outputPath, "Accounts.json");
        await ExportEntity(context.Carriers, outputPath, "Carriers.json");
        await ExportEntity(context.Suppliers, outputPath, "Suppliers.json");
        await ExportEntity(context.Trucks, outputPath, "Trucks.json");
        await ExportEntity(context.UnitsOfMeasure, outputPath, "UnitsOfMeasure.json");
        await ExportEntity(context.PalletTypes, outputPath, "PalletTypes.json");
        await ExportEntity(context.Rates, outputPath, "Rates.json");
        await ExportEntity(context.Warehouses, outputPath, "Warehouses.json");
        await ExportEntity(context.Rooms, outputPath, "Rooms.json");
        await ExportEntity(context.Locations, outputPath, "Locations.json");
        await ExportEntity(context.Docks, outputPath, "Docks.json");
        await ExportEntity(context.YardSpots, outputPath, "YardSpots.json");
        await ExportEntity(context.MaterialCategories, outputPath, "MaterialCategories.json");
        await ExportEntity(context.Materials, outputPath, "Materials.json");
        await ExportEntity(context.BillOfMaterials, outputPath, "BillOfMaterials.json");
        await ExportEntity(context.BillOfMaterialLines, outputPath, "BillOfMaterialLines.json");
        await ExportEntity(context.Pallets, outputPath, "Pallets.json");
        // Add other entities as needed
    }

    private static async Task ExportEntity<T>(IQueryable<T> query, string outputPath, string filename) where T : class
    {
        var data = await query.AsNoTracking().ToListAsync();
        var json = JsonSerializer.Serialize(data, _options);
        await File.WriteAllTextAsync(Path.Combine(outputPath, filename), json);
        Console.WriteLine($"Exported {data.Count} records to {filename}");
    }

    public static async Task SeedAsync(WmsDbContext context, string inputPath)
    {
        if (!Directory.Exists(inputPath))
        {
            Console.WriteLine($"Seed data directory not found: {inputPath}");
            return;
        }

        // Identity Tables
        await SeedEntity<IdentityRole<Guid>>(context, inputPath, "Roles.json");
        await context.SaveChangesAsync();
        
        await SeedEntity<Company>(context, inputPath, "Companies.json"); // Users depend on Company
        await context.SaveChangesAsync();

        // Custom User Seeding to fix FK and ensure valid CompanyId
        if (!await context.Users.AnyAsync())
        {
            try 
            {
                Console.WriteLine("Starting User seed...");
                var userPath = Path.Combine(inputPath, "Users.json");
                if (File.Exists(userPath))
                {
                    var json = await File.ReadAllTextAsync(userPath);
                    var users = JsonSerializer.Deserialize<List<User>>(json, _options);
                    
                    if (users != null && users.Any())
                    {
                        // Fix FK: Get the actual Company ID from the DB
                        var validCompanyId = await context.Companies.Select(c => c.Id).FirstOrDefaultAsync();
                        if (validCompanyId != Guid.Empty)
                        {
                            var companyIdProp = typeof(User).GetProperty(nameof(User.CompanyId));
                            foreach (var user in users)
                            {
                                // Use reflection to set private setter, ensuring it matches the DB's Company
                                companyIdProp?.SetValue(user, validCompanyId);
                            }
                            Console.WriteLine($"Fixed CompanyId for {users.Count} users to {validCompanyId}");
                        }
                        
                        await context.Users.AddRangeAsync(users);
                        Console.WriteLine($"Tracker has {context.ChangeTracker.Entries().Count(e => e.State == EntityState.Added)} added entities.");
                        await context.SaveChangesAsync();
                        Console.WriteLine("User seed saved successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR seeding Users: {ex}");
                throw; 
            }
        }
        else
        {
             Console.WriteLine("Skipping User seed (already exists).");
        }

        await SeedEntity<IdentityUserRole<Guid>>(context, inputPath, "UserRoles.json");
        await context.SaveChangesAsync();

        // Group 1: Parties & Core Definitions
        await SeedEntity<Account>(context, inputPath, "Accounts.json");
        await SeedEntity<Carrier>(context, inputPath, "Carriers.json");
        await SeedEntity<Supplier>(context, inputPath, "Suppliers.json");
        await SeedEntity<Truck>(context, inputPath, "Trucks.json");
        await SeedEntity<UnitOfMeasure>(context, inputPath, "UnitsOfMeasure.json");
        await SeedEntity<PalletType>(context, inputPath, "PalletTypes.json");
        await SeedEntity<Rate>(context, inputPath, "Rates.json");
        await context.SaveChangesAsync();
        Console.WriteLine("Seeded Parties & Core Definitions.");

        // Group 2: Warehouse Structure
        await SeedEntity<Warehouse>(context, inputPath, "Warehouses.json");
        await SeedEntity<Room>(context, inputPath, "Rooms.json");
        await SeedEntity<Location>(context, inputPath, "Locations.json");
        await SeedEntity<Dock>(context, inputPath, "Docks.json");
        await SeedEntity<YardSpot>(context, inputPath, "YardSpots.json");
        await context.SaveChangesAsync();
        Console.WriteLine("Seeded Warehouse Structure.");

        // Group 3: Material Master
        await SeedEntity<MaterialCategory>(context, inputPath, "MaterialCategories.json");
        await SeedEntity<Material>(context, inputPath, "Materials.json");
        await SeedEntity<BillOfMaterial>(context, inputPath, "BillOfMaterials.json");
        await SeedEntity<BillOfMaterialLine>(context, inputPath, "BillOfMaterialLines.json");
        await context.SaveChangesAsync();
        Console.WriteLine("Seeded Material Master.");

        // Group 4: Inventory
        await SeedEntity<Pallet>(context, inputPath, "Pallets.json");
        await context.SaveChangesAsync();
        Console.WriteLine("Seeded Inventory.");
        Console.WriteLine("Database seeded successfully.");
    }

    private static async Task SeedEntity<T>(WmsDbContext context, string inputPath, string filename) where T : class
    {
        var filePath = Path.Combine(inputPath, filename);
        if (!File.Exists(filePath)) return;

        if (await context.Set<T>().AnyAsync())
        {
             Console.WriteLine($"Skipping {typeof(T).Name} seed (already exists).");
             return;
        }

        var json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<List<T>>(json, _options);
        
        if (data != null && data.Any())
        {
            // Reset IDs if necessary or ensure Identity Insert is on (EF Core handles this for explicit values usually)
            await context.Set<T>().AddRangeAsync(data);
            Console.WriteLine($"Seeded {data.Count} records for {typeof(T).Name}");
        }
    }
    public static async Task SeedUsersAsync(UserManager<User> userManager)
    {
        if (!await userManager.Users.AnyAsync())
        {
            var adminUser = new User(
                "admin@example.com",
                "admin@example.com",
                "Admin",
                "User",
                Domain.Enums.UserRole.Admin
            );
            
            adminUser.EmailConfirmed = true;

            var result = await userManager.CreateAsync(adminUser, "Password123!");
            if (result.Succeeded)
            {
                Console.WriteLine("Default admin user seeded.");
            }
            else
            {
                Console.WriteLine($"Failed to seed admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}
