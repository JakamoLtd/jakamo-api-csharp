using System.IO;
using System.Threading.Tasks;
using Ardalis.Result;

namespace Jakamo.Api.Interfaces
{
    public interface IPurchaseOrderClient
    {
        Task<Result<bool>> SendOrder(Stream orderXml);
        Task<Result<bool>> UpdateOrder(int orderId, Stream orderXml);
        Task<Result<bool>> CancelOrder(int orderId, Stream cancellationXml);
        Task<Result<bool>> OrderReceived(int orderId, Stream orderReceivedXml);
        Task<Result<Stream>> GetOrderResponse();
    }
}