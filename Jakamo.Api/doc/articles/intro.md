# Jakamo API 0.0.1-dev

This is a library that can be used to simplify access to the Jakamo APIs. 
It is currently under development and has not been battle tested yet.

**Currently supported:**
- Sales orders

**Work in progress:**
- Purchase Orders
- Claims

## Quickstart

1) Construct a HTTP Client and set the default ```Accept``` and ```Authorization``` headers
2) Construct a logger or inject one using Dependency Injection
3) Construct the Sales Order client, passing the HTTP Client and the Logger instance to it. This would be done with 
Dependency Injection in eg. an ASP.NET Core web app. 
4) Fetch sales orders
5) Process them in some way (read and validate XML, generate internal representation and store in ERP, write to disk, etc)
6) Tell Jakamo you have processed the Sales Order using the ```RemoveSalesOrderFromQueueAsync``` method
7) Repeat from 4 until there are no more sales orders in the queue

```c#
using System.Xml.Linq;
using Ardalis.Result;
using Jakamo.Api.Client;
using Serilog;
using Serilog.Extensions.Logging;

namespace Example;

// 1. Construct the HTTP Client
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://demo.thejakamo.com");
httpClient.DefaultRequestHeaders.Add("Accept", "application/xml");
httpClient.DefaultRequestHeaders.Add("Authorization", "Basic <your token here>");

// 2. Construct the logger. This is using Serilog as an example
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var logger = new SerilogLoggerProvider(Log.Logger).CreateLogger(nameof(SalesOrderClient));

// 3. Construct the sales order client
var client = new SalesOrderClient(httpClient, logger);

while (true) {

    // 4. Fetch sales order
    var result = await client.GetSalesOrderAsync();
        
    if (result.Status == ResultStatus.NotFound) break; // No more orders, break out of loop
        
    var so = result.Value; // Returns a SalesOrderDto -object
    Console.WriteLine($"Order number: {so.OrderNumber}");
    Console.WriteLine($"Confirmation URI: {so.ConfirmationUri}");
    Console.WriteLine($"Acknowledgement URI: {so.AcknowledgementUri}");
    
    // 5. Process the sales order
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
    
    // 6. Remove the processed sales order from the queue
    var removeSalesOrderFromQueueResult = await client.RemoveSalesOrderFromQueueAsync(so.AcknowledgementUri);
    Console.WriteLine(removeSalesOrderFromQueueResult.Value
        ? $"Successfully removed {so.OrderNumber} from queue"
        : $"Error removing the order: ${string.Join(" ", removeSalesOrderFromQueueResult.Errors)}"); 
}
```