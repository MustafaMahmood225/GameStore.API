using GameStore.Api.Data;
using GameStore.Api.Dtos;
using GameStore.Api.Models;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
// this file will contain our endpoints (GET,POST,etc)
namespace GameStore.Api.Endpoints
{
    // static so it can keep its value across multiple fucntion calls instead of 
    // being removed and re created each time
    public static class GamesEndpoints
    {
        const string GetGameEndPointName = "GetGame";

        // WHY: We removed the old in-memory "games" list because we now use the real database.
        // Keeping stale in-memory data alongside a real DB would cause confusion about where
        // the data actually lives.

        public static void MapGamesEndpoints(this WebApplication app)
        {
            // WHY: A route group lets all endpoints in this method share the "/games" prefix.
            // This keeps endpoint definitions DRY (Don't Repeat Yourself).
            var group = app.MapGroup("/games");

            // WHY: We removed the old "/basic" test route. It was only useful during early
            // development to confirm routing worked. It has no place in a real API.

            // WHY: Creating the logger once here means every endpoint shares it, instead of each
            // handler asking for ILoggerFactory. Loggers are safe to share across requests.
            // The category name "GamesEndpoints" appears in every log line so you can filter by it.
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("GamesEndpoints");

            // WHY: Parameters that aren't in the route are read from the query string, so a request
            // like GET /games?page=2&pageSize=5 fills them in. The "= 1" and "= 10" are defaults,
            // used when the client doesn't send the value.
            // "string? name = null" is an optional query parameter. The "?" means it can be null,
            // so we can tell "client didn't send it" apart from "client sent a value". The default
            // "= null" is what keeps it optional. Example: GET /games?name=zel
            group.MapGet("/", async (GameStoreContext dbContext, int page = 1, int pageSize = 10,string? name=null) => {

                // WHY: Never trust client input. page 0 or -3 would make Skip((page - 1) * pageSize)
                // negative, and pageSize 1,000,000 would defeat the point of paging.
                // Math.Max keeps page at 1 or more. Math.Clamp keeps pageSize between 1 and 50.
                page = Math.Max(page, 1); // this means if page is less than 1 it will be set to 1
                pageSize = Math.Clamp(pageSize, 1, 50); // this means if pageSize is less than 1 it will be set to 1 and if its greater than 50 it will be set to 50


                // WHY: Start with "all games" as an IQueryable. This line doesn't touch the database.
                // It only describes the query, so we can add conditions to it below.
                IQueryable<Game> query = dbContext.Games;

                if (!string.IsNullOrWhiteSpace(name))// this will check if the name is not null or empty
                {
                    // WHY: EF.Functions.Like becomes SQL "LIKE". The % symbols mean "anything", so
                    // "%zel%" matches any name that contains "zel" anywhere. We use Like instead of
                    // game.Name.Contains(name) because on SQLite Like ignores upper/lower case for
                    // English letters, but Contains is case-sensitive. EF Core sends the pattern as
                    // a parameter, so user input can't inject SQL.
                    query = query.Where(game => EF.Functions.Like(game.Name, $"%{name}%"));
                }

                //CountAsync runs SELECT COUNT(*) so the client knows the total across all pages.
                var totalCount = await query.CountAsync();

                var games = await query

                //Paging needs a stable order. Ordering by Id means each game appears on
                // exactly one page.
                .OrderBy(game => game.Id)

                // WHY: Page 1 skips 0 rows, page 2 skips pageSize rows, page 3 skips 2 * pageSize...
                .Skip((page - 1) * pageSize)

                // WHY: Take limits how many rows come back. EF Core turns Skip and Take into SQL
                // (LIMIT and OFFSET in SQLite), so the database does the slicing and your app
                // never loads the extra rows.
                .Take(pageSize)
            .Select(game => new GameSummaryDTO( // we want to project to user the elements not show it exactly as is
                        game.Id,
                        game.Name,
                        game.Genre!.Name,// The ! tells the compiler we know Genre won't be null here
                        game.Price,
                        game.ReleaseDate
                ))
            .AsNoTracking()// this will make query faster since program doesnt need to track this
            .ToListAsync();

                return new PagedResponse<GameSummaryDTO>(games, page, pageSize, totalCount);
            });


            // GET /games/1
            // WHY: We inject ILoggerFactory so the framework can give us a named logger.
            // We can't use ILogger<GamesEndpoints> directly because GamesEndpoints is a
            // static class and C# does not allow static types as generic type arguments.
            // ILoggerFactory.CreateLogger("GamesEndpoints") produces the same result:
            // a logger whose category name appears in every log line so you can filter by it.
            group.MapGet("/{id}", async (int id, GameStoreContext dbContext, ILoggerFactory loggerFactory) =>
            {
                
                var game = await dbContext.Games.FindAsync(id);

                if (game is null)
                {
                    // WHY: We log a Warning (not an Error) because a missing game is expected
                    // behaviour — it's the client's fault, not our server crashing.
                    // WHY: We use {GameId} curly-brace syntax rather than string interpolation ($"").
                    // This preserves the value as a typed property in the log, making it
                    // searchable in tools like Azure Monitor or Seq.
                    logger.LogWarning("Game with id {GameId} was not found", id);
                    return Results.NotFound();
                }

                logger.LogInformation("Retrieved game {GameName} (id={GameId})", game.Name, id);

                return Results.Ok(
                    new GameDetailsDTO(
                        game.Id,
                        game.Name,
                        game.GenreId,
                        game.Price,
                        game.ReleaseDate
                    )
                ); // returns 404 if null, 200 OK with the game DTO if found
            })
            .WithName(GetGameEndPointName); // WithName lets us reference this endpoint by name
                                            // in CreatedAtRoute() — so the POST can return the
                                            // correct Location header pointing to the new resource.

            // POST /games
            group.MapPost("/", async (CreateGameDTO newGame, GameStoreContext dbContext) => // this container will inject an instance of gamestorecontext into this enpoint
            {
                // WHY: [Range] can only check that GenreId is a number. It can't know which genres are
                // in the database. Without this check, an unknown id would reach SaveChangesAsync(),
                // break the foreign key (Game.GenreId -> Genre.Id) and crash with a 500 error.
                // AnyAsync() asks the database "is there at least one genre with this id?" and returns
                // true or false. It is cheaper than loading the whole genre row.
                if (!await dbContext.Genres.AnyAsync(genre => genre.Id == newGame.GenreId))// this will check if the genre id exists in the database
                {
                    return Results.BadRequest($"Genre with id {newGame.GenreId} does not exist.");
                }

                Game game = new()
                {
                    Name = newGame.Name,
                    GenreId = newGame.GenreId,
                    Price=newGame.Price,
                    ReleaseDate=newGame.ReleaseDate
                };

                dbContext.Games.Add(game);// this line only tells EF Core that it needs to keep track of this

                await dbContext.SaveChangesAsync();

                logger.LogInformation("created game {GameName} (id={GameId})", game.Name, game.Id);

                GameDetailsDTO gamesDTO=new(
                    game.Id,
                    game.Name,
                    game.GenreId,
                    game.Price,
                    game.ReleaseDate
                    );

                return Results.CreatedAtRoute(GetGameEndPointName, new { id = gamesDTO.Id }, gamesDTO ); // first param is the referal to the get second is to search by ID
                                                                                                // third is the payload or body of resource created just now
            });

            // PUT /games/1
            group.MapPut("/{id}", async(int id, UpdateGameDTO updatedGame, GameStoreContext dbContext) => // here we have two params in handler,
                                                                             // this is because first one is calling the route while second one is for request body
            {
                var existingGame = await dbContext.Games.FindAsync(id);

                if (existingGame is null)
                {
                    logger.LogWarning("update failed: game with id {GameId} was not found", id);

                    return Results.NotFound();
                }
                // WHY: Same reason as in POST. An update can also change GenreId to one that doesn't
                // exist. We check this after the NotFound check so a missing game is reported first.
                if (!await dbContext.Genres.AnyAsync(genre => genre.Id == updatedGame.GenreId))
            {
                return Results.BadRequest($"Genre with id {updatedGame.GenreId} does not exist.");
            }

                

                

                existingGame.Name = updatedGame.Name;
                existingGame.GenreId = updatedGame.GenreId;
                existingGame.Price = updatedGame.Price;
                existingGame.ReleaseDate = updatedGame.ReleaseDate;

                await dbContext.SaveChangesAsync();

                logger.LogInformation("updated game (id={GameId})", id);

                return Results.NoContent();
            });

            // DELETE /games/1
            group.MapDelete("/{id}",async (int id, GameStoreContext dbContext) =>
            {
                // WHY: ExecuteDeleteAsync() runs a single DELETE statement in the database and
                // returns an int: the number of rows it deleted. We store that number instead of
                // throwing it away, because it tells us whether the game actually existed.
                var rowsDeleted = await dbContext.Games.Where(game => game.Id == id).ExecuteDeleteAsync();// this is called bold deletion

                if (rowsDeleted == 0)
                {
                    logger.LogWarning("delete failed: game with id {GameId} was not found", id);
                    return Results.NotFound();
                }

                logger.LogInformation("deleted game (id={GameId})", id);

                // WHY: 0 rows deleted means no game had that id, so the client asked for something
                // that doesn't exist -> 404 Not Found. Otherwise the delete worked -> 204 No Content.
                // The ternary (condition ? ifTrue : ifFalse) is a compact if/else that returns a value.
                return Results.NoContent();
            });
        }
    }
}
