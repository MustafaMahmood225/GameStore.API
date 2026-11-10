using GameStore.Api.Data;
using GameStore.Api.Dtos;
using Microsoft.EntityFrameworkCore;

// WHY: We fix the namespace to match the rest of the project (GameStore.Api.Endpoints).
// A namespace is like a "last name" for your class — it must be consistent so the compiler
// and other developers can find it without confusion.
namespace GameStore.Api.Endpoints
{
    public static class GenresEndpoints
    {
        public static void MapGenresEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/genres");

            // GET /genres
            group.MapGet("/", async (GameStoreContext dbContext) => await dbContext.Genres
            .Select(genre => new GenreDTO (genre.Id, genre.Name))
                .AsNoTracking()
                .ToListAsync()
                );
        }

    }
}