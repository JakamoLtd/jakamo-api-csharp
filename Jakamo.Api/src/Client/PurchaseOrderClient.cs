using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Ardalis.Result;
using Jakamo.Api.Interfaces;

namespace Jakamo.Api.Client {

public class PurchaseOrderClient : CommonClient, IPurchaseOrderClient
{
    
    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="client"></param>
    protected PurchaseOrderClient(HttpClient client) : base(client)
    {
    }

    public async Task<Result<bool>> SendOrder(Stream orderXml)
    {
        var result = await HttpClient.PostAsync(
            "/api/order",
                    new StreamContent(orderXml));

        return result.IsSuccessStatusCode 
                ? Result<bool>.Success(true)
                : Result<bool>.Error(result.ReasonPhrase);
    }

    public async Task<Result<bool>> UpdateOrder(int orderId, Stream orderXml)
    {
        var result = await HttpClient.PostAsync(
            $"/api/order/{orderId}",
                    new StreamContent(orderXml));

        return result.IsSuccessStatusCode
            ? Result<bool>.Success(true)
            : Result<bool>.Error(result.ReasonPhrase);
    }

    public async Task<Result<bool>> CancelOrder(int orderId, Stream cancellationXml)
    {
        
        return await UpdateOrder(orderId, cancellationXml);
    }

    public async Task<Result<bool>> OrderReceived(int orderId, Stream orderReceivedXml)
    {
        return await UpdateOrder(orderId, orderReceivedXml);
    }

    public async Task<Result<Stream>> GetOrderResponse()
    {
        var result = await HttpClient.GetAsync("/order/response");

        return result.IsSuccessStatusCode
            ? Result<Stream>.Success(await result.Content.ReadAsStreamAsync())
            : Result<Stream>.Error(result.ReasonPhrase);
    }
    
    
}

}