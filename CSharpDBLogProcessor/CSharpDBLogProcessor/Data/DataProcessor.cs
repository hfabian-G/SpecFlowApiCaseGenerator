
using MySql.Data.MySqlClient;

public class DataProcessor
{
    MySqlConnection dbConnection;
    public DataProcessor()
    {
        dbConnection = new DatabaseConnection().GetConnection();
    }

    

}