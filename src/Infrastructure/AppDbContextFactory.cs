using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using  BankingService.Data;
using Microsoft.AspNetCore.Http.Features;
using Azure.Monitor.OpenTelemetry.Exporter;

namespace BankingService.Data;
public class AppDbContextFactory : IDesignTimeDbContextFactory<BankingDbContext>
{
    public BankingDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var provider = config["Database:Provider"];

        var optionsBuilder = new DbContextOptionsBuilder<BankingDbContext>();

        if (provider == "sqlite")
        {
            var sqliteConn = config.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlite(sqliteConn);
        }
        else
        {
            var sqlConn = config.GetConnectionString("SqlServer");
            optionsBuilder.UseSqlServer(sqlConn);
        }

        return new BankingDbContext(optionsBuilder.Options);
    }
}
