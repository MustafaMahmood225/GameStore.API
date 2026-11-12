namespace GameStore.Api.Services
{
    // WHY: The service can't return Results.NotFound() because it knows nothing about HTTP.
    // An enum lists the possible outcomes, and the endpoint turns each into a status code.
    public enum UpdateGameResult
    {
        Success,
        NotFound,
        InvalidGenre
    }
}
