using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

public class DatabaseManager
{
    private readonly string _connectionString;

    public DatabaseManager(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Создать таблицы в базе данных, если они не существуют
    public async Task InitializeDatabaseAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new NpgsqlCommand(@"
        CREATE TABLE IF NOT EXISTS users (
            id BIGINT PRIMARY KEY,
            first_name TEXT,
            last_name TEXT,
            username TEXT
        );

        CREATE TABLE IF NOT EXISTS visited_events (
            user_id BIGINT,
            event_id INT,
            FOREIGN KEY (user_id) REFERENCES users(id)
        );

        CREATE TABLE IF NOT EXISTS wishlist_events (
            user_id BIGINT,
            event_id INT,
            FOREIGN KEY (user_id) REFERENCES users(id)
        );
    ", connection);

        await command.ExecuteNonQueryAsync();
        
    }

    //private async Task<NpgsqlConnection> OpenConnectionAsync()
    //{
    //    var connection = new NpgsqlConnection(_connectionString);
    //    await connection.OpenAsync();
    //    return connection;
    //}
  

    // Добавить событие в список "Посещено" пользователя
    public async Task AddVisitedEventAsync(long userId, int eventId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(@"
        INSERT INTO visited_events (user_id, event_id)
        VALUES (@userId, @eventId)
        ON CONFLICT DO NOTHING;
    ", connection);

        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("eventId", eventId);

        await command.ExecuteNonQueryAsync();
    }
    // Добавить событие в список "Хочу посетить" пользователя
    public async Task AddWishlistEventAsync(long userId, int eventId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(@"
        INSERT INTO wishlist_events (user_id, event_id)
        VALUES (@userId, @eventId)
        ON CONFLICT DO NOTHING;
    ", connection);

        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("eventId", eventId);

        await command.ExecuteNonQueryAsync();
    }
    public async Task AddUserAsync(long userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(@"
    INSERT INTO users (id)
    VALUES (@userId)
    ON CONFLICT DO NOTHING;
", connection);

        command.Parameters.AddWithValue("userId", userId);

        await command.ExecuteNonQueryAsync();
    }
    public async Task RemoveVisitedEventAsync(long userId, int eventId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(@"
    DELETE FROM visited_events
    WHERE user_id = @userId AND event_id = @eventId;
    ", connection);

        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("eventId", eventId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveWishlistEventAsync(long userId, int eventId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(@"
    DELETE FROM wishlist_events
    WHERE user_id = @userId AND event_id = @eventId;
    ", connection);

        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("eventId", eventId);

        await command.ExecuteNonQueryAsync();
    }
    // Получить список "Посещено" пользователя
    public async Task<List<int>> GetVisitedEventsAsync(long userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(@"
        SELECT event_id FROM visited_events WHERE user_id = @userId;
    ", connection);

        command.Parameters.AddWithValue("userId", userId);

        using var reader = await command.ExecuteReaderAsync();
        var eventIds = new List<int>();
        while (await reader.ReadAsync())
        {
            eventIds.Add(reader.GetInt32(0));
        }

        return eventIds;
    }
    // Получить список "Хочу посетить" пользователя
    public async Task<List<int>> GetWishlistEventsAsync(long userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(@"
        SELECT event_id FROM wishlist_events WHERE user_id = @userId;
    ", connection);

        command.Parameters.AddWithValue("userId", userId);

        using var reader = await command.ExecuteReaderAsync();
        var eventIds = new List<int>();
        while (await reader.ReadAsync())
        {
            eventIds.Add(reader.GetInt32(0));
        }

        return eventIds;
    }
    
}
