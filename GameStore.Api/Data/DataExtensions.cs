using GameStore.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace GameStore.Api.Data
// this code will automatically apply any pending db migration when app starts
// saving us from having to run the command in cli everytime the db schema changes
{
    public static class DataExtensions
    {
        // static since it has an extension method
        public static void MigrateDb(this WebApplication app) 
        {
            using var scope = app.Services.CreateScope(); //scope so we can get access to instance of gamestorecontext
            // keyword using means afterwards this is deleted from memory, important since this contains a database connection
            var dbContext =scope.ServiceProvider.GetRequiredService<GameStoreContext>();// this is to retrieve instance of dbcontext
            dbContext.Database.Migrate();
        }

        public static void AddGameStoreDb (this WebApplicationBuilder builder)
        {
            var connString = builder.Configuration.GetConnectionString("GameStore"); // contains necessary info for establishing direct connection between application and database 
            
            builder.Services.AddSqlite<GameStoreContext>(
                connString,// below is using data seeding to default values into genre table
                optionsAction: options => options.UseSeeding((context, _) => // here we use _ to skip async operations
                {
                    if (!context.Set<Genre>().Any())
                    {
                        context.Set<Genre>().AddRange(
                            new Genre { Name = "Fighting" },
                            new Genre { Name = "RPG" },
                            new Genre { Name = "Platformer" },
                            new Genre { Name = "Racing" },
                            new Genre { Name = "Sports" }
                            );
                        context.SaveChanges();
                    }
                })
                // WHY: EF Core has two seeding hooks. UseSeeding runs when you call Migrate() and
                // UseAsyncSeeding runs when you call MigrateAsync(). Microsoft recommends providing
                // both so seeding works whichever one is used. The async version uses
                // AnyAsync / SaveChangesAsync so it doesn't block a thread while waiting on the
                // database. The third lambda parameter is a CancellationToken, which lets the
                // operation stop early if the app shuts down.
                .UseAsyncSeeding(async (context,_,cancellationToken) =>
                { 
                    if (!await context.Set<Genre>().AnyAsync(cancellationToken))
                    {
                        context.Set<Genre>().AddRange(
                        new Genre { Name = "Fighting" },
                        new Genre { Name = "RPG" },
                        new Genre { Name = "Platformer" },
                        new Genre { Name = "Racing" },
                        new Genre { Name = "Sports" }
                         );
                        await context.SaveChangesAsync(cancellationToken);
                    }
                        })
                );// after this need to do database migration to sync it with application data model and create database schema
                  //migration can be done by applying the below commands to dev powershell
                  //dotnet tool install --global dotnet-ef
                  //dotnet ef migrations add InitialCreate --output-dir Data\Migrations
                  //dotnet ef database update 
                  // these steps will be skipped since we created DataExtensions class which will do the same thing
        }
    }
}
