namespace Client;

using NLog;
using Grpc.Net.Client;

//this comes from GRPC generated code
using Services;


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
                //connect to the server, get service proxy
                var channel = GrpcChannel.ForAddress("http://127.0.0.1:5000");
                var client = new Service.ServiceClient(channel);

                //use service
                var random = new Random();

                while (true)
                {
                    var canAdd = client.CanAddLiquid(new Empty { }).Value;

                    Thread.Sleep(2000);

                    var liquidToAdd = random.Next(1, 20);

                    if (canAdd)
                    {
                        log.Info($"Generated amount to add: {liquidToAdd}.");
                        var addedLiquid = client.AddLiquid(new Liquid { Amount = liquidToAdd }).Value;
                        log.Info($"Amount of liquid added: {addedLiquid}.");
                        log.Info("\n");
                    }
                    else
                    {
                        log.Info("I cannot add any more liquid.");
                        log.Info("\n");
                    }
                    log.Info("---");

                    Thread.Sleep(2000);
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