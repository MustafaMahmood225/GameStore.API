using GameStore.Api.Data;
using GameStore.Api.Dtos;
using GameStore.Api.Models;
using GameStore.Api.Services;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameStore.Api.Tests
{
    // WHY: xUnit creates a NEW instance of this class for every single test. So the constructor
    // runs before each test (fresh database, fresh service) and Dispose() runs after it. Tests
    // can't affect each other, which is the most important rule of unit testing.
    public sealed class GamesServiceTests : IDisposable //IDisposable is a promise that the class has that method,
                                                        //and the compiler refuses to build if it doesn’t. xUnit calls Dispose() after every test.
    {
        // sealed means "can't be inherited from". This is a good idea for test classes
        private readonly SqliteConnection _connection;
        private readonly GameStoreContext _dbContext;
        private readonly GamesService _service;
        private readonly Genre _figthing = new() { Name = "Fighting" };
        private readonly Genre _rpg = new() { Name = "RPG" };

        public GamesServiceTests()
        {
            // WHY: ":memory:" is a SQLite database that lives only in RAM. It exists only while
            // the connection is open, so we open it ourselves and keep it open until Dispose().
            // If EF Core opened and closed it per query, the tables would vanish between calls.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<GameStoreContext>()
                .UseSqlite(_connection)
                .Options;

            _dbContext = new GameStoreContext(options);

            // WHY: EnsureCreated() builds the tables straight from your model (including the
            // unique index and max lengths from OnModelCreating). Your migrations aren't
            // needed here. Note the seeding in DataExtensions isn't used either, so the test
            // adds the genres it needs itself.
            _dbContext.Database.EnsureCreated();

            _dbContext.Genres.AddRange(_figthing, _rpg);
            _dbContext.SaveChanges();

            // WHY: This is the payoff of the service layer. We build GamesService by hand,
            // with no web server and no dependency injection container. NullLogger is a
            // logger that throws every message away, since the tests don't read logs.
            _service=new GamesService(_dbContext, NullLogger<GamesService>.Instance);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            _connection.Dispose(); // closing the connection deletes the in-memory database
        }

        // Helper: adds games to the database for the "arrange" part of a test.
        private void AddGames(Genre genre, params string[] names)
        {
         foreach (var name in names)
            {
                _dbContext.Games.Add(new Game { 
                    Name=name,
                    GenreId = genre.Id,
                    Price = 59.99m,
                    ReleaseDate = new DateOnly(2000, 1, 1)
                });
            }
            _dbContext.SaveChanges();

            // WHY: The DbContext remembers every object it has seen. A real web request gets a
            // fresh context, so we clear the memory to behave the same way. Otherwise a test
            // could pass just because the object was still cached, hiding a real bug.
            _dbContext.ChangeTracker.Clear();
        }

        // WHY: [Theory] runs the same test once per [InlineData] row, so one method checks
        // several inputs. [Fact] (below) is a test with no inputs.
        [Theory]
        [InlineData(0,1)]       // page 0 is invalid, so it becomes 1
        [InlineData(-5, 1)]     // negative too
        [InlineData(3, 3)]      // a valid page is left alone

        public async Task GetGamesAsync_ClampsPageToAtLeastOne(int requested,int expected)
        {
            // Act: call the method being tested (nothing to arrange, an empty database is fine)
            var result = await _service.GetGamesAsync(requested, 10,null,null);
            // Assert: check the outcome
            Assert.Equal(expected, result.Page);
        }

        [Theory]
        [InlineData(0,1)]
        [InlineData(9999,50)]
        [InlineData(20,20)]

        public async Task GetGamesAsync_ClampsPageSizeBetweenOneAndFifty(int requested, int expected)
        {
            var result = await _service.GetGamesAsync(1, requested, null, null);

            Assert.Equal(expected, result.PageSize);
        }

        [Fact]
        public async Task GetGamesAsync_ReturnsOnlyTheRequestedPage()
        {
            // Arrange: add 25 games to the database, so we can test paging
            AddGames(_figthing, Enumerable.Range(1, 25).Select(i => $"Game {i}").ToArray());

            // Act: ask for page 2 of size 10 (should return games 11-20)
            var result = await _service.GetGamesAsync(2, 10, null, null);

            // Assert: check the outcome
            Assert.Equal(2, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(25, result.TotalCount);
            Assert.Equal(3, result.TotalPages);
            Assert.Equal(10, result.Items.Count());
            Assert.Equal("Game 11", result.Items.First().Name);
            Assert.Equal("Game 20", result.Items.Last().Name);
            //Assert.Equal("Game 15", result.Items[0].Name); 
            // this test should fail
        }

        [Fact]
        public async Task GetGamesAsync_NameFilterIgnoresCase() 
        {
            AddGames(_rpg, "Mario", "Zelda");

            var result = await _service.GetGamesAsync(1, 10, "zEL", null);

            var game = Assert.Single(result.Items); // exactly one item, and gives it back to us

            Assert.Equal("Zelda", game.Name);
        }

        [Fact]
        public async Task GetGamesAsync_GenreFilterReturnsOnlyThatGenre_AndCountsFilteredRows()
        {
            AddGames(_figthing, "Street Fighter", "Tekken");
            AddGames(_rpg, "Final Fantasy", "Dragon Quest");

            var result = await _service.GetGamesAsync(1, 10, null, _figthing.Id);

            Assert.Equal(2, result.TotalCount); // 2, not 3: the count is taken AFTER filtering

            Assert.All(result.Items, game => Assert.Equal("Fighting", game.Genre));
        }

        [Fact]
        public async Task DeleteGameAsync_MissingGame_ReturnsFalse()
        {
            var result = await _service.DeleteGameAsync(123); // no game with that Id

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateGameAsync_MissingGame_ReturnsNotFound()
        {
            var result = await _service.UpdateGameAsync(123, new UpdateGameDTO(
                "Game 67",
                _figthing.Id,
                59.99m,
                new DateOnly(2000, 1, 1)));

            Assert.Equal(UpdateGameResult.NotFound, result); // should return NotFound because the game doesn't exist
        }

        [Fact]
        public async Task CreateGameAsync_UnknownGenre_ReturnsNull()
        {
            var result = await _service.CreateGameAsync(new CreateGameDTO(
                "Game 62",
                123, // no genre with that Id
                59.99m,
                new DateOnly(2000, 1, 1)));

            Assert.Null(result); // should return null because the genre doesn't exist
        }

        [Fact]
        public async Task DeleteGameAsync_ExistingGame_ReturnsTrue_AndRemovesIt()
        {
            AddGames(_figthing, "Mortal Kombat", "Tekken");
            AddGames(_rpg, "Final Fantasy", "Dragon Quest");

            var result = await _service.DeleteGameAsync(1); // delete the first game (Mortal Kombat)
            var result2 = await _service.GetGamesAsync(1, 10, null, null); // get all games

            Assert.True(result); // should return true because the game was deleted
            Assert.Equal(3, result2.TotalCount); // should have 3 games left
        }

        [Fact]
        public async Task UpdateGameAsync_UnknownGenre_ReturnsInvalidGenre_AndChangesNothing()
        {
            AddGames(_rpg, "The Elder Scrolls V Skyrim");

            var result = await _service.UpdateGameAsync(1, new UpdateGameDTO(
                "The Elder Scrolls V Skyrim",
                999, // no genre with that Id
                59.99m,
                new DateOnly(2000, 1, 1)));

            Assert.Equal(UpdateGameResult.InvalidGenre, result); // should return InvalidGenre because the genre doesn't exist
        }

    }
}
