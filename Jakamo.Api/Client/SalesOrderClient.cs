using System.Xml;
using System.Xml.Linq;
using Ardalis.Result;
using Jakamo.Api.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jakamo.Api.Client;

public class SalesOrderClient : CommonClient, ISalesOrderClient
{
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor
    /// 
    /// Only calls parent constructor
    /// </summary>
    /// <param name="client">A HttpClient instance to use for communication</param>
    /// <param name="logger">An ILogger implementation for logging</param>
    public SalesOrderClient(HttpClient client, ILogger logger) : base(client)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get a sales order from the Jakamo queue
    ///
    /// HttpClient in the base class must have any Content-type and
    /// Authorization headers set
    /// 
    /// <see cref="CommonClient"/>
    /// <seealso cref="SalesOrder"/>
    /// </summary>
    /// <returns>A Result object containing the sales order as an XML stream and the ACK-uri</returns>
    public async Task<Result<SalesOrder>> GetSalesOrderAsync()
    {
        var result = await HttpClient.GetAsync("/api/order/response");
        
        if (!result.IsSuccessStatusCode) return Result<SalesOrder>.Error(result.ReasonPhrase);
        
        var xmlData = await result.Content.ReadAsStreamAsync();
        
        if (IsQueueEmpty(xmlData)) return Result<SalesOrder>.NotFound();
        
        // Rewind the stream
        xmlData.Position = 0;

        var orderNumber = result
            .Headers
            .FirstOrDefault(h => h.Key == "Jakamo-Order-Number")
            .Value
            .FirstOrDefault();

        var confirmationUri = result
            .Headers
            .FirstOrDefault(h => h.Key == "Jakamo-Confirm-Url")
            .Value
            .FirstOrDefault();

        var ackUri = result
            .Headers
            .FirstOrDefault(h => h.Key == "X-Acknowledge-Uri")
            .Value
            .FirstOrDefault();
        
        return Result<SalesOrder>.Success(new SalesOrder
        {
            XmlStream = xmlData,
            AcknowledgementUri = ackUri,
            ConfirmationUri = confirmationUri,
            OrderNumber = orderNumber
        });

    }

    /// <summary>
    /// Check if the queue is empty.
    ///
    /// Jakamo returns a status-element with the content
    /// "No more messages available." when the queue is empty
    /// </summary>
    /// <param name="xmlData">The raw XML stream from GetSalesOrder</param>
    /// <returns>true if the queue is empty, false if there are more sales orders</returns>
    private static bool IsQueueEmpty(Stream xmlData)
    {
        var xml = XElement.Load(xmlData);
        return xml.ToString().Contains("No more messages available");
    }


    /// <summary>
    /// Remove a sales order from the queue, indicating that the API consumer
    /// has processed the order successfully
    /// </summary>
    /// <param name="ackUri">The acknowledgement URI to post to</param>
    /// <returns>A Success-result on success, and an Error result if there was a failure</returns>
    public async Task<Result<bool>> RemoveSalesOrderFromQueueAsync(string? ackUri)
    {
        var result = await HttpClient.PostAsync(ackUri, null);

        return result.IsSuccessStatusCode
            ? Result<bool>.Success(true)
            : Result<bool>.Error(result.ReasonPhrase);
    }

    /// <summary>
    /// Post an order response to Jakamo
    /// </summary>
    /// <param name="responseXml">The XML stream containing the order response</param>
    /// <returns>A Success-result on success, and an Error result if there was a failure</returns>
    public async Task<Result<bool>> SendOrderResponseAsync(Stream responseXml)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/orderresponse");

        request.Content = new StreamContent(responseXml);
        request.Content.Headers.Add("Content-Type", "application/xml");

        var result = await HttpClient.SendAsync(request);

        // It was successful, return the success code
        if (result.IsSuccessStatusCode) return Result<bool>.Success(true);

        // It was not successful, try to find the error messages
        var content = await result.Content.ReadAsStreamAsync();

        var xmlDoc = XElement.Load(content);
        var errors = xmlDoc.DescendantsAndSelf("error").Select(x => x.Value).ToArray();

        return Result<bool>.Error(errors);
        
    }

    /// <summary>
    /// Confirm all proposed changes to a Sales Order without an explicit OrderConfirmation message
    /// </summary>
    /// <see cref="SalesOrder.ConfirmationUri"/>
    /// <param name="confirmationUri">The URI endpoint to post to.</param>
    /// <returns>A Success-result on success, and an Error result if there was a failure</returns>
    public async Task<Result<bool>> ConfirmAllChangesForSalesOrderAsync(string? confirmationUri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, confirmationUri);

        request.Content = new StringContent("");
        request.Content.Headers.Remove("Content-Type");
        request.Content.Headers.Add("Content-Type", "application/xml");
        
        var result = await HttpClient.SendAsync(request);
        
        return result.IsSuccessStatusCode
            ? Result<bool>.Success(true)
            : Result<bool>.Error(result.ReasonPhrase);
    }
}
