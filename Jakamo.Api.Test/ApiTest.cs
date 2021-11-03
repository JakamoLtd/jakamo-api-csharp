using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using Jakamo.Api.Client;
using Jakamo.Api.DTO;
using Moq;
using Moq.Protected;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Jakamo.Api.Test
{
 public class SalesOrderClientTest
{

    private readonly ILogger _logger;

    public SalesOrderClientTest()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();
        _logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(SalesOrderClient));

    }
    [Fact]
    public async Task Test_GetSalesOrderAsync_Returns_NotFoundResult_When_StatusIsNoMoreMessagesAvailable()
    {
        const string str = "<status>No more messages available.</status>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(str));
        
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                Content = new StreamContent(stream),
                StatusCode = HttpStatusCode.OK
            });
        
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://dummy.local");
        var client = new SalesOrderClient(httpClient, _logger);
        

        var result = await client.GetSalesOrderAsync();
        Assert.True(result.Status == ResultStatus.NotFound);
    }
    
    [Fact]
    public async Task Test_GetSalesOrderAsync_Returns_OrderResult_When_ResponseContainsOrder()
    {
        const string str = "<Order>lol</Order>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(str));
        
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                Content = new StreamContent(stream),
                StatusCode = HttpStatusCode.OK,
                Headers =
                {
                    {"Jakamo-Order-Number", "1234"},
                    {"Jakamo-Confirm-Url", "http://dummy.local/1234"},
                    {"X-Acknowledge-Uri", "http://dummy.local/1234"}
                    
                }
            });
        
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://dummy.local");
        var client = new SalesOrderClient(httpClient, _logger);
        
        var result = await client.GetSalesOrderAsync();
        Assert.IsType<SalesOrderDto>(result.Value);
        var streamContents = await new StreamReader(result.Value.XmlStream).ReadToEndAsync();
        Assert.Equal("<Order>lol</Order>", streamContents);
        Assert.False(string.IsNullOrEmpty(result.Value.OrderNumber));
        Assert.False(string.IsNullOrEmpty(result.Value.AcknowledgementUri));
        Assert.False(string.IsNullOrEmpty(result.Value.ConfirmationUri));
    }
    
    [Fact]
    public async Task Test_GetSalesOrders_Returns_Error_When_RequestFails()
    {
        const string str = "<Order>lol</Order>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(str));
        
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                Content = new StreamContent(stream),
                StatusCode = HttpStatusCode.BadRequest,
            });
        
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://dummy.local");
        var client = new SalesOrderClient(httpClient, _logger);
        
        var result = await client.GetSalesOrderAsync();
        Assert.IsNotType<SalesOrderDto>(result.Value);
        Assert.True(result.Status == ResultStatus.Error);
        Assert.Equal("Bad Request", string.Join(" ", result.Errors));
    }

    [Fact]
    public async Task Test_RemoveOrderFromQueue_Returns_Success_When_OrderRemoved()
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK
            });
        
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://dummy.local");
        var client = new SalesOrderClient(httpClient, _logger);
        
        var result = await client.RemoveSalesOrderFromQueueAsync("https://dummy.local");
        Assert.IsNotType<SalesOrderDto>(result.Value);
        Assert.True(result.Status == ResultStatus.Ok);
    }
    
    [Fact]
    public async Task Test_RemoveOrderFromQueue_Returns_Error_When_OrderNotRemoved()
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadRequest
            });
        
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://dummy.local");
        var client = new SalesOrderClient(httpClient, _logger);
        
        var result = await client.RemoveSalesOrderFromQueueAsync("https://dummy.local");
        Assert.IsNotType<SalesOrderDto>(result.Value);
        Assert.True(result.Status == ResultStatus.Error);
        Assert.Equal("Bad Request", string.Join(" ", result.Errors));
    }
    
    [Fact]
    public async Task Test_CSendOrderResponse_Returns_Error_When_OrderNotFound()
    {
        const string str = "<errors><error>Order not found</error></errors>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(str));
        
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StreamContent(stream)
            });
        
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://dummy.local");
        var client = new SalesOrderClient(httpClient, _logger);

        var result = await client.SendOrderResponseAsync(stream);
        Assert.True(result.Status == ResultStatus.Error);
        Assert.Equal("Order not found", string.Join(" ", result.Errors));
    }
    
    [Fact]
    public async Task Test_SendOrderResponse_Returns_Error_When_MultipleErrorsFound()
    {
        const string str = "<errors><error>Order not found</error><error>Also this is an error</error></errors>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(str));
        
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StreamContent(stream)
            });
        
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://dummy.local");
        var client = new SalesOrderClient(httpClient, _logger);

        var result = await client.SendOrderResponseAsync(stream);
        Assert.True(result.Status == ResultStatus.Error);
        Assert.Equal("Order not found;Also this is an error", string.Join(";", result.Errors));
    }

    [Fact]
    public async Task Test_SendOrderResponse_Returns_Success_When_OrderIsConfirmed()
    {
        const string str = "<OrderConfirmation>lol</OrderConfirmation>";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(str));
        
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK
            });
        
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://dummy.local");
        var client = new SalesOrderClient(httpClient, _logger);

        var result = await client.SendOrderResponseAsync(stream);
        Assert.True(result.Status == ResultStatus.Ok);
    }
    
    [Fact]
    public async Task Test_ConfirmAllOrderChanges_Returns_Success_When_OrderIsConfirmed()
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK
            });
        
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://dummy.local");
        var client = new SalesOrderClient(httpClient, _logger);

        var result = await client.ConfirmAllChangesForSalesOrderAsync("http://dummy.local");
        Assert.True(result.Status == ResultStatus.Ok);
    }
    
    [Fact]
    public async Task Test_ConfirmAllOrderChanges_Returns_Error_When_OrderIsNotConfirmed()
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.BadRequest
            });
        
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri("https://dummy.local");
        var client = new SalesOrderClient(httpClient, _logger);

        var result = await client.ConfirmAllChangesForSalesOrderAsync("http://dummy.local");
        Assert.True(result.Status == ResultStatus.Error);

    }
}   
}