using Ardalis.Result;
using Jakamo.Api.Client;

namespace Jakamo.Api.Interfaces;

public interface ISalesOrderClient
{
    Task<Result<SalesOrder>> GetSalesOrderAsync();
    Task<Result<bool>> RemoveSalesOrderFromQueueAsync(string? ackUri);
    Task<Result<bool>> SendOrderResponseAsync(Stream responseXml);
    Task<Result<bool>> ConfirmAllChangesForSalesOrderAsync(string? confirmationUri);
}