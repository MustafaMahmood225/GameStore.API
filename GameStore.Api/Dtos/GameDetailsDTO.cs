namespace GameStore.Api.Dtos
//DTO: a contract between client and server that represents a shared agreement
// about how data will be transferred and used
// basically its puprose is to move data across different parts of system without carrying any business logic
{// a record is primarily used for holding data (DTO, read only state)
    public record GameDetailsDTO
    (
        int Id,
        string Name,
        int Genre, // only difference between this and gamedto is this returns int
        decimal Price,
        DateOnly ReleaseDate
    );
}
