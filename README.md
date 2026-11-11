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
| GET | /games | List all games |
| GET | /games/{id} | Get one game |
| POST | /games | Create a game |
| PUT | /games/{id} | Update a game |
| DELETE | /games/{id} | Delete a game |
| GET | /genres | List all genres |

## Logs
Written to `logs/gamestore-<date>.log` and the console.
