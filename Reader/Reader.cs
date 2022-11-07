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
                var rnd = new Random();

                while (true)
                {
                    log.Info("");
                    // takes a random book from the library
                    int bookNumber = rnd.Next(0, client.GetLibraryCapacity(new Empty { }).Capacity);
                    var result = client.TakeBook(new Id { Id_ = bookNumber });
                    Book book = new Book();
                    book.Id = bookNumber;
                    book.LoanDuration = result.LoanDuration;
                    book.LoanTime = DateTime.Parse(result.LoanTime);
                    book.Taken = result.Taken;
                    book.Wear = result.Wear;
                    book.RepairPrice = result.RepairPrice;

                    log.Info($"Book {book.Id} taken");

                    // if the book is null, it means that it is already taken
                    if (book == null)
                    {
                        log.Info($"Book {bookNumber} is not available");
                        Thread.Sleep(2000);
                        continue;
                    }

                    // decides a random time to take reading the book
                    int timeToRead = rnd.Next(5000, 10000);
                    log.Info($"Reading book {bookNumber} for {timeToRead} ms");
                    Thread.Sleep(timeToRead);

                    // Returns the book
                    client.ReturnBook(new BookInput { Id = book.Id, LoanDuration = book.LoanDuration, LoanTime = book.LoanTime.ToString(), Taken = book.Taken, Wear = book.Wear, RepairPrice = book.RepairPrice });
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