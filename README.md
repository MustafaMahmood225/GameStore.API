# GameStore API

A small REST API for managing a video game catalogue, built to learn .NET framework.

## Tech stack
- .NET 10, ASP.NET Core minimal APIs
- Entity Framework Core with SQLite
- Serilog for logging (console + daily log files)
- Scalar for interactive API docs

## Getting started
```bash
git clone https://github.com/MustafaMahmood225/GameStore.Api.git
cd GameStore.Api
dotnet run --project GameStore.Api
```
The database is created and migrated automatically on startup.

## Endpoints
| Method | Route | Description |
|---|---|---|
| GET | /games | List games (paged, with optional search and filter, see below) || GET | /games/{id} | Get one game |
| POST | /games | Create a game |
| PUT | /games/{id} | Update a game |
| DELETE | /games/{id} | Delete a game |
| GET | /genres | List all genres |

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
## Logs
Written to `logs/gamestore-<date>.log` and the console.
