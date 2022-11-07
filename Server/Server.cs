namespace Server;

using System.Net;

using NLog;


/// <summary>
/// Application entry point.
/// </summary>
public class Book
{
    public int Id { get; set; } // Id (Primary key)
    public float LoanDuration { get; set; } // LoanDuration
    public bool Taken { get; set; } = false; // Indicates if the book is taken or not
    public DateTime LoanTime { get; set; } // Time when the book was taken
    public float Wear { get; set; } = 0f; // Indicates how much the book is worn
    public float RepairPrice { get; set; } = 1f; // Price of the book per 1 wear to repair
}
public class Server
{
    public static float money = 0;
    public static int capacity = 5;
    public static Book[] books = new Book[capacity];

    /// <summary>
    /// Logger for this class.
    /// </summary>
    Logger log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Configure loggin subsystem.
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
    /// Program entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public static void Main(string[] args)
    {
        var self = new Server();
        self.Run(args);
    }

    /// <summary>
    /// Program body.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    private void Run(string[] args)
    {
        //configure logging
        ConfigureLogging();

        // populate the library
        for (int i = 0; i < capacity; i++)
        {
            books[i] = new Book();
            books[i].Id = i;
            books[i].RepairPrice = (float)(50 + 10.0 * new Random().NextDouble());
        }

        //indicate server is about to start
        log.Info("Server is about to start");

        //start the server
        StartServer(args);

        while (true)
        {
            Thread.Sleep(4000);
        }
    }

    /// <summary>
    /// Starts integrated server.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    private void StartServer(string[] args)
    {
        //create web app builder
        var builder = WebApplication.CreateBuilder(args);

        //configure integrated server
        builder.WebHost.ConfigureKestrel(opts =>
        {
            opts.Listen(IPAddress.Loopback, 5000);
        });


        //add support for GRPC services
        builder.Services.AddGrpc();

        //add the actural services
        builder.Services.AddSingleton(new Service());

        //build the server
        var app = builder.Build();

        //turn on request routing
        app.UseRouting();

        //configure routes
        app.UseEndpoints(ep =>
        {
            ep.MapGrpcService<Service>();
        });

        //run the server
        //app.Run();
        app.RunAsync(); //use this if you need to implement background processing in the main thread
    }
}