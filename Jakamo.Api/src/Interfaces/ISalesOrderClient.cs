using System.IO;
using System.Threading.Tasks;
using Ardalis.Result;
using Jakamo.Api.Client;
using Jakamo.Api.DTO;

namespace Jakamo.Api.Interfaces
{
    public interface ISalesOrderClient
    {
        Task<Result<SalesOrderDto>> GetSalesOrderAsync();
        Task<Result<bool>> RemoveSalesOrderFromQueueAsync(string ackUri);
        Task<Result<bool>> SendOrderResponseAsync(Stream responseXml);
        Task<Result<bool>> ConfirmAllChangesForSalesOrderAsync(string confirmationUri);
    }   
}