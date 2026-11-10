using GameStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Data
    // this class will act as our DB context this will be used to query and save instances between api and database
{
    
    public class GameStoreContext(DbContextOptions<GameStoreContext> options) 
        : DbContext(options) //inherit from dbcontext and also pass its options to tell class how to connect to database
    {
        public DbSet<Game> Games => Set<Game>(); // DBset represents a table in database the will store game objects,
                                                 // games is the property that will be refered when we interact with the table
                                                 // the arrow is basically a return statement
                                                 // the set basically says find the toold nedeed to work with game objects and hand them to games

        public DbSet<Genre> Genres => Set<Genre>();
        // WHY: OnModelCreating is where you describe how your C# classes map to database
        // columns. The Fluent API (the chained .Property(...).HasX() calls) is more powerful
        // than attributes and keeps database rules out of your model classes.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // WHY: Matches the [StringLength(50)] on the DTOs, so the database model and the
            // API rules agree. Without it, the column has no declared length.
            modelBuilder.Entity<Game>()
                .Property(game => game.Name)
                .HasMaxLength(50);

            // WHY: Precision (18, 2) means up to 18 digits total, 2 after the decimal point,
            // which is the standard for money. Never store prices as float/double, because
            // they round badly (0.1 + 0.2 != 0.3).
            modelBuilder.Entity<Game>()
                .Property(game => game.Price)
                .HasPrecision(18, 2);

            // WHY: Genre names should also have a limit, and a unique index stops two genres
            // with the same name from being inserted.
            modelBuilder.Entity<Genre>()
                .Property(genre => genre.Name)
                .HasMaxLength(50);

            modelBuilder.Entity<Genre>()
                .HasIndex(genre => genre.Name)
                .IsUnique();
        }
    }
}
