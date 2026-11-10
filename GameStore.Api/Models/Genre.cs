namespace GameStore.Api.Models
{
    // genre is set as its own model since we want it to have its own table in database
    public class Genre
    {
        public int Id { get; set; }

        public required string Name { get; set; }
    }
}
