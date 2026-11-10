namespace GameStore.Api.Models
{
    // this model will create a database table with the name game
    public class Game
    {
        //getters & setters = add security to fields by encapsulation
        //They're accessors found within properties
        // properties = combine aspects of both fields and methods (share name with a field)
        // get accessor = used to return the property value (read)
        // set accessor = used to assign a new value (write)
        public int Id { get; set; }

        public required string Name { get; set; }

        public Genre? Genre { get; set; } // ? will make this nullable

        public int GenreId { get; set; } // EF Core will assume Genre will be the foreign key,
                                         // however as good practice its good to assign another variable to be the foreign key instead this is good for efficency

        public decimal Price { get;set;  }

        public DateOnly ReleaseDate { get; set; }
    }
}
