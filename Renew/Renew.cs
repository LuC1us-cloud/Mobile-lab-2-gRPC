namespace Client;

using NLog;
using Grpc.Net.Client;

//this comes from GRPC generated code
using Services;



public class Book
{
    public int Id { get; set; } // Id (Primary key)
    public float LoanDuration { get; set; } // LoanDuration
    public bool Taken { get; set; } = false; // Indicates if the book is taken or not
    public DateTime LoanTime { get; set; } // Time when the book was taken
    public float Wear { get; set; } = 0f; // Indicates how much the book is worn
    public float RepairPrice { get; set; } = 1f; // Price of the book per 1 wear to repair
}
/// <summary>
/// Client example.
/// </summary>
class Client
{
    /// <summary>
    /// Logger for this class.
    /// </summary>
    Logger log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Configures logging subsystem.
    /// </summary>
    private void ConfigureLogging()
    {
        var config = new NLog.Config.LoggingConfiguration();

        var console =
            new NLog.Targets.ConsoleTarget("console")
            {
                Layout = @"${date:format=HH\:mm\:ss}|${level}| ${message} ${exception}"
            };
        config.AddTarget(console);
        config.AddRuleForAllLevels(console);

        LogManager.Configuration = config;
    }

    /// <summary>
    /// Program body.
    /// </summary>
    private void Run()
    {
        //configure logging
        ConfigureLogging();

        //run everythin in a loop to recover from connection errors
        while (true)
        {
            try
            {
                //connect to the server, get client proxy
                var channel = GrpcChannel.ForAddress("http://127.0.0.1:5000");
                var client = new Service.ServiceClient(channel);

                //use client
                var random = new Random();

                while (true)
                {
                    log.Info("--------------------");
                    Thread.Sleep(10000);

                    var booksRaw = client.GetWornOutBooks(new Empty { }).Books;

                    if (booksRaw.Count == 0)
                    {
                        log.Info("No worn out books found");
                        continue;
                    }
                    log.Info($"Found {booksRaw.Count} worn out books");

                    List<Book> books = new List<Book>();
                    foreach (var id in booksRaw)
                    {
                        // convert to Book
                        var book = new Book
                        {
                            Id = id.Id,
                            LoanDuration = id.LoanDuration,
                            Taken = id.Taken,
                            LoanTime = DateTime.Parse(id.LoanTime),
                            Wear = id.Wear,
                            RepairPrice = id.RepairPrice
                        };
                        books.Add(book);
                    }

                    float budget = client.GetLibraryBudget(new Empty { }).Budget;
                    log.Info($"Library budget: {budget.ToString("0.00")}$");
                    if (budget == 0)
                    {
                        log.Info("No budget left");
                        continue;
                    }

                    Book mostWornOutBook = books.First();
                    foreach (var book in books)
                    {
                        if (book.Wear > mostWornOutBook.Wear)
                        {
                            mostWornOutBook = book;
                        }
                    }

                    log.Info($"Most worn out book: Id {mostWornOutBook.Id}, ({mostWornOutBook.Wear.ToString("0.00")})");

                    float repairPrice = mostWornOutBook.RepairPrice * mostWornOutBook.Wear;
                    float repairAmount = budget / repairPrice;
                    float repairAmountPercentage = repairAmount * 100;
                    log.Info($"Available repair amount: {(repairAmountPercentage).ToString("0.00")}% of {mostWornOutBook.Wear.ToString("0.00")} | Full repair price: {repairPrice.ToString("0.00")}$");

                    if (repairAmountPercentage > 100) repairAmountPercentage = 100;

                    float wearToRepair = mostWornOutBook.Wear * repairAmountPercentage / 100;

                    log.Info($"Repairing {wearToRepair.ToString("0.00")} wear");

                    client.RepairBook(new IdAndWear { Id = mostWornOutBook.Id, Wear = wearToRepair });

                    Thread.Sleep(10000);
                }
            }
            catch (Exception e)
            {
                //log whatever exception to console
                log.Warn(e, "Unhandled exception caught. Will restart main loop.");

                //prevent console spamming
                Thread.Sleep(2000);
            }
        }
    }

    /// <summary>
    /// Program entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    static void Main(string[] args)
    {
        var self = new Client();
        self.Run();
    }
}