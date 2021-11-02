using System.Xml.Linq;
using Ardalis.Result;
using Jakamo.Api.Client;
using Serilog;
using Serilog.Extensions.Logging;

var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://demo.thejakamo.com");
httpClient.DefaultRequestHeaders.Add("Accept", "application/xml");
httpClient.DefaultRequestHeaders.Add("Authorization", "Basic CHANGEME");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(SalesOrderClient));
var client = new SalesOrderClient(httpClient, logger);

while(true)
{
    var result = await client.GetSalesOrderAsync();

    // If we have no more results, break out of the loop
    if (result.Status == ResultStatus.NotFound) break;
    
    var so = result.Value;
    Console.WriteLine($"Order number: {so.OrderNumber}");
    Console.WriteLine($"Confirmation URI: {so.ConfirmationUri}");
    Console.WriteLine($"Acknowledgement URI: {so.AcknowledgementUri}");
    
    if (so.XmlStream != null)
    {
        // Write the indented and formatted XML to a file
        // Normally you'd write so.XmlStream directly to a FileStream
        // or however you wish to process it. The formatting is only for
        // example purposes
        Console.WriteLine($"Writing {so.OrderNumber}.xml");
        var doc = XDocument.Load(so.XmlStream);
        await File.WriteAllTextAsync($"{so.OrderNumber}.xml", doc.ToString());
    }

    Console.WriteLine("Confirming all changes to order...");
    var confirmAllResult = await client.ConfirmAllChangesForSalesOrderAsync(so.ConfirmationUri);

    Console.WriteLine(confirmAllResult.Value
        ? $"Successfully confirmed order at {so.ConfirmationUri}"
        : $"Error confirming order: {string.Join("; ", confirmAllResult.Errors)}");
    
    var removeSalesOrderFromQueueAsync = await client.RemoveSalesOrderFromQueueAsync(so.AcknowledgementUri);
    Console.WriteLine(removeSalesOrderFromQueueAsync.Value == true
        ? $"Successfully removed {so.OrderNumber} from queue"
        : $"Error removing the order: ${string.Join(" ", removeSalesOrderFromQueueAsync.Errors)}");    

    Console.WriteLine("------------------------------");
    
}

Console.WriteLine("No more messages in queue.");

Console.WriteLine("Confirming order");
var fileStream = File.OpenRead("Z:\\Jakamo.Api\\SampleApiConsumer\\OrderResponse_mle.xml");
var orderResponseResult = await client.SendOrderResponseAsync(fileStream);
Console.WriteLine(orderResponseResult.Value == true
    ? $"Successfully posted OrderResponse"
    : $"Error sending OrderResponse. Server responded with {string.Join(" ", orderResponseResult.Errors)}");