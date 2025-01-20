class Program
{
    static void Main(string[] args)
    {
        var db = new DatabaseConnection();
        var context = db.GetConnection();

        if (db.TestConnection())
        {
            Console.WriteLine("Successfully connected to MySQL!");
        }
        else
        {
            Console.WriteLine("Failed to connect to MySQL.");
        }
    }
}