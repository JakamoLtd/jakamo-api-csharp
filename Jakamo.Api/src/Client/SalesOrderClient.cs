using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Ardalis.Result;
using Jakamo.Api.DTO;
using Jakamo.Api.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jakamo.Api.Client
{
    
    /// <summary>
    /// A client for handling sales order communication with Jakamo
    /// </summary>
    public class SalesOrderClient : CommonClient, ISalesOrderClient
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor
        /// 
        /// Calls parent constructor to set the HTTP client
        /// </summary>
        /// <param name="client">A HttpClient instance to use for communication</param>
        /// <param name="logger">An ILogger implementation for logging</param>
        public SalesOrderClient(HttpClient client, ILogger logger) : base(client)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get a sales order from the Jakamo queue.
        /// 
        /// See: <see cref="CommonClient"/>
        /// See also: <seealso cref="SalesOrderDto"/>
        /// </summary>
        /// <returns>A Result object containing the sales order as an XML stream and the ACK-uri</returns>
        public async Task<Result<SalesOrderDto>> GetSalesOrderAsync()
        {
            
            _logger.LogInformation("Retrieving Sales Order");

            var result = await TryGetSalesOrderAsync();
            
            if (!result.IsSuccessStatusCode)
            {
                return Result<SalesOrderDto>.Error(result.ReasonPhrase);
            }

            var xmlData = await result.Content.ReadAsStreamAsync();

            _logger.LogDebug("Checking if queue is empty...");
            if (IsQueueEmpty(xmlData)) return Result<SalesOrderDto>.NotFound();

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

            return Result<SalesOrderDto>.Success(new SalesOrderDto
            {
                XmlStream = xmlData,
                AcknowledgementUri = ackUri,
                ConfirmationUri = confirmationUri,
                OrderNumber = orderNumber
            });

        }

        private async Task<HttpResponseMessage> TryGetSalesOrderAsync()
        {
            try
            {
                return await HttpClient.GetAsync("/api/order/response");
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "An exception was thrown while retrieving the sales order");
                throw;
            }
        }

        /// <summary>
        /// Check if the queue is empty.
        ///
        /// Jakamo returns a status-element with the content
        /// "No more messages available." when the queue is empty
        /// </summary>
        /// <param name="xmlData">The raw XML stream from GetSalesOrder</param>
        /// <returns>true if the queue is empty, false if there are more sales orders</returns>
        private bool IsQueueEmpty(Stream xmlData)
        {
            _logger.LogDebug("Loading response XML");
            var xml = XElement.Load(xmlData);
            
            var result = xml.ToString().Contains("No more messages available");
            if (result)
            {
                _logger.LogDebug("No more messages in queue");
            }

            return result;
        }
        
        /// <summary>
        /// Remove a sales order from the queue, indicating that the consumer
        /// has processed the order successfully and it is safe for Jakamo to
        /// remove the sales order. If this is not called, the same sales order
        /// will be returned on the next call to <see cref="GetSalesOrderAsync"/>
        /// </summary>
        /// <param name="ackUri">The acknowledgement URI to post to, can be retrieved from <see cref="SalesOrderDto"/></param>
        /// <returns>A Success-result on success, and an Error result if there was a failure</returns>
        public async Task<Result<bool>> RemoveSalesOrderFromQueueAsync(string ackUri)
        {
            _logger.LogInformation("Attempting to remove sales order from queue");

            var result = await TryRemoveSalesOrderFromQueueAsync(ackUri);
            
            return !result.IsSuccessStatusCode 
                ? Result<bool>.Error(result.ReasonPhrase) 
                : Result<bool>.Success(true);
        }

        private async Task<HttpResponseMessage> TryRemoveSalesOrderFromQueueAsync(string ackUri)
        {
            try
            {
                return await HttpClient.PostAsync(ackUri, null);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "An exception was thrown while removing a sales order from the queue");
                throw;
            }
        }

        /// <summary>
        /// Post an order response to Jakamo
        /// </summary>
        /// <param name="responseXml">The XML stream containing the order response</param>
        /// <returns>A Success-result on success, and an Error result if there was a failure</returns>
        public async Task<Result<bool>> SendOrderResponseAsync(Stream responseXml)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/orderresponse"))
            {
                request.Content = new StreamContent(responseXml);
                request.Content.Headers.Add("Content-Type", "application/xml");

                var result = await TrySendOrderResponseAsync(request);

                // It was successful, return the success code
                if (result.IsSuccessStatusCode) return Result<bool>.Success(true);

                // It was not successful, try to find the error messages
                var content = await result.Content.ReadAsStreamAsync();
                var xmlDoc = XElement.Load(content);
                var errors = xmlDoc.DescendantsAndSelf("error").Select(x => x.Value).ToArray();
                return Result<bool>.Error(errors);
            }
        }

        private async Task<HttpResponseMessage> TrySendOrderResponseAsync(HttpRequestMessage request)
        {
            try
            {
                return await HttpClient.SendAsync(request);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "An exception was thrown when sending order response");
                throw;
            }
        }

        /// <summary>
        /// Confirm all proposed changes to a Sales Order without an explicit OrderConfirmation message
        /// </summary>
        /// <see cref="SalesOrderDto.ConfirmationUri"/>
        /// <param name="confirmationUri">The URI endpoint to post to.</param>
        /// <returns>A Success-result on success, and an Error result if there was a failure</returns>
        public async Task<Result<bool>> ConfirmAllChangesForSalesOrderAsync(string confirmationUri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, confirmationUri))
            {
                request.Content = new StringContent("");
                request.Content.Headers.Remove("Content-Type");
                request.Content.Headers.Add("Content-Type", "application/xml");

                var result = await TryConfirmAllChangesForSalesOrderAsync(request);

                if (result == null || !result.IsSuccessStatusCode)
                {
                    return Result<bool>.Error(
                        result != null 
                            ? new [] { result.ReasonPhrase }
                            : new [] { "An unknown error occured" }
                    );
                }
                return Result<bool>.Success(true);
            }
        }
        
        private async Task<HttpResponseMessage> TryConfirmAllChangesForSalesOrderAsync(HttpRequestMessage request)
        {
            try
            {
                return await HttpClient.SendAsync(request);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "An exception was thrown when confirming changes for sales order");
                throw;
            }
        }
    }
}
