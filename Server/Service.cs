namespace Server;

using NLog;
using Grpc.Core;

//this comes from GRPC generated code
using Services;


/// <summary>
/// Service. This is made to run as a singleton instance.
/// </summary>
public class Service : Services.Service.ServiceBase
{
    /// <summary>
    /// Logger for this class.
    /// </summary>
    Logger log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Access lock.
    /// </summary>
    private readonly Object accessLock = new Object();

    /// <summary>
    /// Service logic implementation.
    /// </summary>
    private ServiceLogic logic = new ServiceLogic();

    public override Task<BookResult> TakeBook(Id request, ServerCallContext context)
    {
        lock (accessLock)
        {
            var book = logic.TakeBook(request.Id_);
            if (book == null)
            {
                return Task.FromResult(new BookResult { });
            }
            else
            {
                return Task.FromResult(new BookResult
                {
                    Id = book.Id,
                    LoanDuration = book.LoanDuration,
                    Taken = book.Taken,
                    LoanTime = book.LoanTime.ToString(),
                    Wear = book.Wear,
                    RepairPrice = book.RepairPrice,
                });
            }
        }
    }

    public override Task<Empty> ReturnBook(BookInput request, ServerCallContext context)
    {
        lock (accessLock)
        {
            logic.ReturnBook(new Book
            {
                Id = request.Id,
                LoanDuration = request.LoanDuration,
                Taken = request.Taken,
                LoanTime = DateTime.Parse(request.LoanTime),
                Wear = request.Wear,
                RepairPrice = request.RepairPrice,
            });
            return Task.FromResult(new Empty { });
        }
    }

    public override Task<Empty> RepairBook(IdAndWear request, ServerCallContext context)
    {
        lock (accessLock)
        {
            logic.RepairBook(request.Id, request.Wear);
            return Task.FromResult(new Empty { });
        }
    }

    public override Task<GetLibraryCapacityOutput> GetLibraryCapacity(Empty request, ServerCallContext context)
    {
        lock (accessLock)
        {
            var capacity = logic.GetLibraryCapacity();
            return Task.FromResult(new GetLibraryCapacityOutput { Capacity = capacity });
        }
    }

    public override Task<GetWornOutBooksOutput> GetWornOutBooks(Empty request, ServerCallContext context)
    {
        lock (accessLock)
        {
            var books = logic.GetWornOutBooks();
            var output = new GetWornOutBooksOutput();
            foreach (var book in books)
            {
                output.Books.Add(new BookResult
                {
                    Id = book.Id,
                    LoanDuration = book.LoanDuration,
                    Taken = book.Taken,
                    LoanTime = book.LoanTime.ToString(),
                    Wear = book.Wear,
                    RepairPrice = book.RepairPrice,
                });
            }
            return Task.FromResult(output);
        }
    }

    public override Task<GetLibraryBudgetOutput> GetLibraryBudget(Empty request, ServerCallContext context)
    {
        lock (accessLock)
        {
            var budget = logic.GetLibraryBudget();
            return Task.FromResult(new GetLibraryBudgetOutput { Budget = budget });
        }
    }
}