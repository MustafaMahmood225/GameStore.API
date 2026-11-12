using GameStore.Api.Data;
using GameStore.Api.Dtos;
using GameStore.Api.Models;
using Microsoft.EntityFrameworkCore;
// this serves as the logic for the endpoints to call and get the data from the database and return it to the endpoints
namespace GameStore.Api.Services
{
    // WHY: A primary constructor (the parameters after the class name) lets dependency
    // injection hand over what this class needs. The DbContext and the logger are supplied
    // by the container, so the class never creates them itself.
    // ILogger<GamesService> works here because this is a normal class, not a static one.
    public class GamesService(GameStoreContext dbContext, ILogger<GamesService> logger) : IGamesService
    {
        public async Task<PagedResponse<GameSummaryDTO>> GetGamesAsync(int page, int pageSize, string? name, int? genreId)
        {
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



            if (genreId.HasValue)// this will check if the genreId is not null
            {
                query = query.Where(game => game.GenreId == genreId.Value);
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
        }

        public async Task<GameDetailsDTO?> GetGameByIdAsync(int id)
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
                return null;
            }

            logger.LogInformation("Retrieved game {GameName} (id={GameId})", game.Name, id);

            return new GameDetailsDTO(
                game.Id,
                game.Name,
                game.GenreId,
                game.Price,
                game.ReleaseDate
            );
        }
        public async Task<GameDetailsDTO?> CreateGameAsync(CreateGameDTO newGame)
        {
            // WHY: [Range] can only check that GenreId is a number. It can't know which genres are
            // in the database. Without this check, an unknown id would reach SaveChangesAsync(),
            // break the foreign key (Game.GenreId -> Genre.Id) and crash with a 500 error.
            // AnyAsync() asks the database "is there at least one genre with this id?" and returns
            // true or false. It is cheaper than loading the whole genre row.
            if (!await dbContext.Genres.AnyAsync(genre => genre.Id == newGame.GenreId))// this will check if the genre id exists in the database
            {
                return null;
            }

            Game game = new()
            {
                Name = newGame.Name,
                GenreId = newGame.GenreId,
                Price = newGame.Price,
                ReleaseDate = newGame.ReleaseDate
            };

            dbContext.Games.Add(game);// this line only tells EF Core that it needs to keep track of this

            await dbContext.SaveChangesAsync();

            logger.LogInformation("created game {GameName} (id={GameId})", game.Name, game.Id);

            return new GameDetailsDTO(game.Id,game.Name,game.GenreId, game.Price, game.ReleaseDate);

        }

        public async Task<UpdateGameResult> UpdateGameAsync(int id, UpdateGameDTO updatedGame)
        {
            var existingGame = await dbContext.Games.FindAsync(id);

            if (existingGame is null)
            {
                logger.LogWarning("update failed: game with id {GameId} was not found", id);

                return UpdateGameResult.NotFound;
            }
            // WHY: Same reason as in POST. An update can also change GenreId to one that doesn't
            // exist. We check this after the NotFound check so a missing game is reported first.
            if (!await dbContext.Genres.AnyAsync(genre => genre.Id == updatedGame.GenreId))
            {
                return UpdateGameResult.InvalidGenre;
            }


            existingGame.Name = updatedGame.Name;
            existingGame.GenreId = updatedGame.GenreId;
            existingGame.Price = updatedGame.Price;
            existingGame.ReleaseDate = updatedGame.ReleaseDate;

            await dbContext.SaveChangesAsync();

            logger.LogInformation("updated game (id={GameId})", id);

            return UpdateGameResult.Success;
        }

        public async Task<bool> DeleteGameAsync(int id)
        {
            // WHY: ExecuteDeleteAsync() runs a single DELETE statement in the database and
            // returns an int: the number of rows it deleted. We store that number instead of
            // throwing it away, because it tells us whether the game actually existed.
            var rowsDeleted = await dbContext.Games.Where(game => game.Id == id).ExecuteDeleteAsync();// this is called bold deletion

            if (rowsDeleted == 0)
            {
                logger.LogWarning("delete failed: game with id {GameId} was not found", id);
                return false;
            }

            logger.LogInformation("deleted game (id={GameId})", id);

            //true tells the caller that a game was deleted. The endpoint decides what
            // status code that means.
            return true;
        }
    }
}
