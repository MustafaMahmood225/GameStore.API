# GameStore API

A small REST API for managing a video game catalogue, built to learn .NET.

## Tech stack
- .NET 10, ASP.NET Core minimal APIs
- Entity Framework Core with SQLite
- Serilog for logging (console + daily log files)
- Scalar for interactive API docs
- xUnit for unit tests, run against an in-memory SQLite database

## Getting started
```bash
git clone https://github.com/MustafaMahmood225/GameStore.Api.git
cd GameStore.Api
dotnet run --project GameStore.Api
```
The database is created and migrated automatically on startup, and five genres are seeded
(Fighting, RPG, Platformer, Racing, Sports).

In Development, interactive API docs are available at `http://localhost:5250/scalar`.

## Endpoints
| Method | Route | Description | Success |
|---|---|---|---|
| GET | /games | List games (paged, with optional search and filter, see below) | 200 |
| GET | /games/{id} | Get one game | 200 |
| POST | /games | Create a game | 201, with a `Location` header |
| PUT | /games/{id} | Update a game | 204 |
| DELETE | /games/{id} | Delete a game | 204 |
| GET | /genres | List all genres | 200 |

### Listing games

`GET /games` supports paging, searching and filtering through optional query parameters:

| Parameter | Type | Default | Description |
|---|---|---|---|
| `page` | int | 1 | Page number. Values below 1 become 1 |
| `pageSize` | int | 10 | Games per page. Limited to 1 to 50 |
| `name` | text | none | Returns games whose name contains this text (not case-sensitive) |
| `genreId` | int | none | Returns only games in this genre |

The parameters can be combined:

```
GET /games?name=mario&genreId=3&page=1&pageSize=5
```

The response includes the games plus paging information:

```json
{
  "items": [
    { "id": 8, "name": "Street Fighter II", "genre": "Fighting", "price": 14.99, "releaseDate": "1991-02-06" }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 20,
  "totalPages": 2
}
```

`totalCount` and `totalPages` describe the results after filtering, not the whole table.

### Getting, creating and updating a game

`GET /games/{id}` and `POST /games` return a single game. Note that it contains the genre's **id**
(`genreId`), while the list above shows the genre's **name** (`genre`):

```json
{ "id": 8, "name": "Street Fighter II", "genreId": 1, "price": 14.99, "releaseDate": "1991-02-06" }
```

`POST /games` and `PUT /games/{id}` take this body:

```json
{ "name": "Street Fighter II", "genreId": 1, "price": 14.99, "releaseDate": "1991-02-06" }
```

Rules: `name` is required and at most 50 characters, `price` must be between 0.01 and 1000,
and `genreId` must be an existing genre.

## Error responses

| Situation | Status | Body |
|---|---|---|
| A query value that isn't a number, e.g. `?genreId=abc` | 400 | Problem details JSON (`"title": "Invalid request"`) |
| Broken JSON in a POST or PUT body | 400 | Problem details JSON (`"title": "Invalid request"`) |
| A body rule is broken (empty name, name too long, price out of range) | 400 | Validation problem JSON with an `errors` list naming the field |
| `genreId` doesn't exist on POST or PUT | 400 | A message: `Genre with id X does not exist.` |
| No game with that id on GET, PUT or DELETE | 404 | Problem details JSON |

Example:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Invalid request",
  "status": 400,
  "detail": "One or more request values are invalid."
}
```

## Running the tests

From the repo root:

```bash
dotnet test
```

The tests in `GameStore.Api.Tests` cover `GamesService`: paging limits, name search, genre
filtering, and the create, update and delete outcomes. Each test gets its own fresh in-memory
SQLite database, so tests never touch `GameStore.db` or affect each other.

## Project structure

| Folder | Contains |
|---|---|
| `GameStore.Api/Endpoints` | Route definitions. They read the request and choose the status code |
| `GameStore.Api/Services` | `GamesService`: queries, rules and logging, with no HTTP code |
| `GameStore.Api/Dtos` | The shapes of request and response bodies |
| `GameStore.Api/Models` | The database entities (`Game`, `Genre`) |
| `GameStore.Api/Data` | The EF Core `DbContext`, migrations and seeding |
| `GameStore.Api/ExceptionHandlers` | Turns unreadable requests into 400 responses instead of 500 |
| `GameStore.Api.Tests` | xUnit unit tests |

## Logs
Written to `logs/gamestore-<date>.log` and the console, with one summary line per request
(for example `HTTP GET /games responded 200 in 12.3 ms`).