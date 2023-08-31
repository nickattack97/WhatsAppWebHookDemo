using Newtonsoft.Json;
using RestSharp;
using static WebHookDemo.Models.TextMessage;

using IHost host = Host.CreateDefaultBuilder(args).Build();

IConfiguration? config = host.Services.GetService<IConfiguration>();

string? mytoken = config?.GetSection("AppSettings").GetSection("MyToken").Value;
string? bearerToken = config?.GetSection("AppSettings").GetSection("BearerToken").Value;
string? baseUrl = config?.GetSection("AppSettings").GetSection("BaseUrl").Value;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/webhook", async context => {

    Console.WriteLine("In Get Endpoint");
    var mode = context.Request.Query["hub.mode"].ToString();
    var challenge = context.Request.Query["hub.challenge"].ToString();
    var token = context.Request.Query["hub.verify_token"].ToString();

    if(mode != null && token != null)
    {
        if(mode == "subscribe" && token == mytoken)
        {
            await context.Response.WriteAsync(challenge);
            Console.WriteLine(challenge);
            return;
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
    }

});

app.MapPost("/webhook", async context => {

    Console.WriteLine("In Post Endpoint");
    var reader = new StreamReader(context.Request.Body);
    string body = reader.ReadToEndAsync().Result;

    WhatsAppMessageRoot? message = JsonConvert.DeserializeObject<WhatsAppMessageRoot>(body);

    Console.WriteLine(body);

    if (body.Contains("object"))
    {
        if (body.Contains("entry"))
        {
            var phoneNumId = message?.entry[0].changes[0].value.metadata.phone_number_id;
            var from = message?.entry[0].changes[0].value.messages[0].from;
            var msgBody = message?.entry[0].changes[0].value.messages[0].text.body;

            Console.WriteLine(String.IsNullOrEmpty(phoneNumId) ? "EmptyPhone": phoneNumId);
            Console.WriteLine(String.IsNullOrEmpty(from) ? "FromPhone" : from);
            Console.WriteLine(String.IsNullOrEmpty(msgBody) ? "EmptyMsg" : msgBody);

            var options = new RestClientOptions(baseUrl)
            {
                MaxTimeout = -1,
            };

            var client = new RestClient(options);
            var request = new RestRequest("/v17.0/" + phoneNumId + "/messages", Method.Post);

            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", "Bearer " + bearerToken + "");

            var data = @"{" + "\n" +
                @"    ""messaging_product"": ""whatsapp"",    " + "\n" +
                @"    ""recipient_type"": ""individual""," + "\n" +
                @"    ""to"": """+from+"\"," + "\n" +
                @"    ""type"": ""text""," + "\n" +
                @"    ""text"": {" + "\n" +
                @"        ""preview_url"": false," + "\n" +
                @"        ""body"": ""Nick Dev Jedi Test - " +msgBody+"\"" + "\n" +
                @"    }" + "\n" +
                @"}";

            request.AddStringBody(data, DataFormat.Json);
            await client.ExecuteAsync(request);
            return;
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
    }


});

app.MapGet("/", async context => {
    await context.Response.WriteAsync(".NET 7 Demo Webook Test");
    return;
});


app.Run();

