using GameStore.Api.Dtos;
namespace GameStore.Api.Services
{
    // WHY: An interface is a contract: it lists WHAT the service can do, not HOW.
    // Endpoints depend on this contract, so the implementation can be replaced
    // (for example with a fake in tests) without changing the endpoints.
    public interface IGamesService
    {
        Task<PagedResponse<GameSummaryDTO>> GetGamesAsync(int page, int pageSize, string? name, int? genreId);

        // this will return null when game has this id
        Task<GameDetailsDTO?> GetGameByIdAsync(int id);

        // returns null when the genre does not exist
        Task<GameDetailsDTO?> CreateGameAsync(CreateGameDTO newGame);

        Task<UpdateGameResult> UpdateGameAsync(int id, UpdateGameDTO updatedGame);

        // returns false when no game has this ID
        Task<bool> DeleteGameAsync(int id);
    }
}
