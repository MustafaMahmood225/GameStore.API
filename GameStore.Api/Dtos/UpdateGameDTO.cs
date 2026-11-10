using System.ComponentModel.DataAnnotations;

namespace GameStore.Api.Dtos
{
    public record UpdateGameDTO(
    [Required][StringLength(50)] string Name,// this will make sure that name will have a value ( no null) adding this is not enough need to register validation services aswell
    // string length will make the maximum number of chars be 50
    [Range(1, int.MaxValue)] int GenreId,
    [Range(0.01, 1000)] decimal Price,
    DateOnly ReleaseDate
    );
    
}
