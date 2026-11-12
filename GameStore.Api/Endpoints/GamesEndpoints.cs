using GameStore.Api.Dtos;
using GameStore.Api.Services;
// this file will contain our endpoints (GET,POST,etc)
namespace GameStore.Api.Endpoints
{
    // static so it can keep its value across multiple fucntion calls instead of 
    // being removed and re created each time
    public static class GamesEndpoints
    {
        const string GetGameEndPointName = "GetGame";

        public static void MapGamesEndpoints(this WebApplication app)
        {
            // WHY: A route group lets all endpoints in this method share the "/games" prefix.
            // This keeps endpoint definitions DRY (Don't Repeat Yourself).
            var group = app.MapGroup("/games");
                            

            // WHY: Parameters that aren't in the route are read from the query string, so a request
            // like GET /games?page=2&pageSize=5 fills them in. The "= 1" and "= 10" are defaults,
            // used when the client doesn't send the value.
            // "string? name = null" is an optional query parameter. The "?" means it can be null,
            // so we can tell "client didn't send it" apart from "client sent a value". The default
            // "= null" is what keeps it optional. Example: GET /games?name=zel
            group.MapGet("/", async (IGamesService gamesService, int page = 1, int pageSize = 10,
                string? name=null, int? genreId=null) => {

                    // The endpoint now only does HTTP work: read the query string, call the service,
                    // return the result. IGamesService is supplied by dependency injection.
                    return await gamesService.GetGamesAsync(page, pageSize, name, genreId);
                });


            // GET /games/1
            
            group.MapGet("/{id}", async (int id, IGamesService gamesService) =>
            {
                var game = await gamesService.GetGameByIdAsync(id);

                // The service returns null for "not found". Deciding that means 404 is an
                // HTTP concern, so it stays in the endpoint.

                return game is null ? Results.NotFound(): Results.Ok(game);

            })
            .WithName(GetGameEndPointName); // WithName lets us reference this endpoint by name
                                            // in CreatedAtRoute() — so the POST can return the
                                            // correct Location header pointing to the new resource.

            // POST /games
            group.MapPost("/", async (CreateGameDTO newGame, IGamesService gamesService) =>
            {
                var createdGame = await gamesService.CreateGameAsync(newGame);

                // null means the genre doesn't exist, which is the client's mistake -> 400.
                if (createdGame is null)
            {
                return Results.BadRequest($"Genre with id {newGame.GenreId} does not exist.");
            }
           
                return Results.CreatedAtRoute(GetGameEndPointName, new { id = createdGame.Id }, createdGame);
            });

            // PUT /games/1
            group.MapPut("/{id}", async(int id, UpdateGameDTO updatedGame, IGamesService gamesService) => // here we have two params in handler,
                                                                             // this is because first one is calling the route while second one is for request body
            {
                var result = await gamesService.UpdateGameAsync(id, updatedGame);

                //A switch expression maps each outcome to a status code. The "_" case is
                // the default and covers Success.

                return result switch
                {
                    UpdateGameResult.NotFound => Results.NotFound(),
                    UpdateGameResult.InvalidGenre => Results.BadRequest($"Genre with id {updatedGame.GenreId} does not exist."),
                    _ => Results.NoContent()
                };
            });

            // DELETE /games/1
            group.MapDelete("/{id}",async (int id, IGamesService gamesService) =>
            {
                var deleted = await gamesService.DeleteGameAsync(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            });
        }
    }
}
