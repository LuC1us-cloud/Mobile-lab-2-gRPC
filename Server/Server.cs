namespace Server;

using System.Net;

using NLog;


/// <summary>
/// Application entry point.
/// </summary>
public class Server
{
    public static int capacity = new Random().Next(0, 100);
    public static int lowerBound = 0;
    public static int upperBound = 0;
    public static bool clientIsActive = false;

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

        //indicate server is about to start
        log.Info("Server is about to start");

        //start the server
        StartServer(args);

        while (true)
        {
            if (clientIsActive)
            {
                log.Info("Client is working...");
                Thread.Sleep(2000);
                Server.clientIsActive = false;
            }
            lowerBound = new Random().Next(0, 50);
            upperBound = new Random().Next(lowerBound + 1, 100);
            log.Info("Bounds changed to: " + lowerBound + " " + upperBound + " and current capacity is: " + capacity);
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