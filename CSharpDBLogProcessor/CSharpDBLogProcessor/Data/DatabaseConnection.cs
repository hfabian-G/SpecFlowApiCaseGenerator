using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

public class DatabaseConnection
{
    private readonly string _connectionString;

    public DatabaseConnection()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public MySqlConnection GetConnection()
    {
        try
        {
            MySqlConnection connection = new MySqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    // Example method to test the connection
    public bool TestConnection()
    {
        using (MySqlConnection connection = GetConnection())
        {
            return connection != null && connection.State == System.Data.ConnectionState.Open;
        }
    }
}
