﻿using System.Diagnostics;
using HttpRecorder;
using Lightr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

var apiKey = builder.Configuration["ApiKey"];

if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException($"Environment is not {Environments.Development}, and thus secrets will not be loaded!");

if (string.IsNullOrWhiteSpace(apiKey))
    throw new InvalidOperationException("""
                                        ApiKey is null or whitespace, please add an apiKey: dotnet user-secrets set "ApiKey" "..."
                                        """);

builder.Services.AddLightr(apiKey)
    // Below is just to demonstrate configuring the client further, in this case we'll be saving all request data to a .har file to inspect
    .AddHttpRecorder("sample_lightr_client", HttpRecorderMode.Record);

// We build and use a Host so that we can use DI, however we don't run it.
var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var client = host.Services.GetRequiredService<ILightrClient>();

// Quick check to see if we can send/receive requests
var meResponse = await client.MeAsync();
logger.LogInformation("I am: {Email}", meResponse.Data.Email);

// Fetch needed items to create an order

var fontsResponse = await client.FontsAsync();
var font = fontsResponse.Data.First();

var presetsResponse = await client.PresetsGETAsync();
var preset = presetsResponse.Data.Last();

var presetDetailsResponse = await client.PresetsGET2Async(preset.Id);
var presetDetails = presetDetailsResponse.Data;

// Create a new order
logger.LogInformation("Attempting to create an order with font: {FontLabel} and preset {Preset} with text variables: {TextVariables}", font.Label, presetDetails.Label, string.Join(", ", presetDetails));

var addOrderResponse = await client.OrdersPOSTAsync(new()
{
    Quantity = 1, // Needs to be >= 1
    Preset_id = presetDetails.Id,
    Font_id = font.Id,
    Type = Body15Type.Send_multiple
});

// Check that the order exists
var orderResponse = await client.OrdersGET2Async(addOrderResponse.Data.Id.ToString());
var orderId = orderResponse.Data.Id;

// A way to fetch all country id's if you need them:
//var countriesResponse = await client.CountriesAsync();
var countryId = new Guid("99d4d37a-0956-428c-9eb6-67e9b205cc09");

// Add receivers:

var receiverPostResponse1 = await client.ReceiversPOSTAsync(orderId, new()
{
    Order_id = orderId,
    
    Name = "Pepijn smitt",
    Country_id = countryId,
    
    Address = "Voorbeeldstraat 1",
    City = "Fantasiastad",
    Postal_code = "1234AB",
    
    Text_variables = new
    {
        Bedrijf = "Smitt Smithy's",
        Voor_achternaam = "Pepijn smitt",
        Straat_nr = "Voorbeeldstraat 1",
        Postcode_plaats = "123XX Fantasiastad",
        Land = "Nederland",

        Multiline = "Dear {{Naam}} from {{Land}},\n This is a test card which you'll be receiving.\n\nYours truly,\n" + meResponse.Data.First_name
    },

    Qr_variables = [
        new()
        {
            Index = 0,
            Trackable = true,
            Value = "https://example.com/card1"
        }
    ]
});

var receiverPostResponse2 = await client.ReceiversPOSTAsync(orderId, new()
{
    Order_id = orderId,

    // The following fields are used as reference by Lightr and won't show up on the card.
    Name = "Chiel de boer",
    Address = "Voorbeeldstraat 2",
    City = "Fantasiastad",
    Postal_code = "1234AB",
    Country_id = countryId,

    Text_variables = new
    {
        // No 'Bedrijf' - but we need a value for the api.
        Bedrijf = "---",
        Voor_achternaam = "Chiel de boer",
        Straat_nr = "Voorbeeldstraat 2",
        Postcode_plaats = "123XX Fantasiastad",
        Land = "Nederland",
        Multiline = "Beste {{Naam}} van {{Land}},\n Dit is een testkaart die je gaat ontvangen.\n\nGroeten,\n" + meResponse.Data.First_name
    },

    // If your card is setup to use a QR-code:
    Qr_variables = [
        new()
        {
            Index = 0,
            Trackable = true,
            Value = "https://example.com/card2"
        }
    ]
});

// Check if the receivers have been added.

var allOrders = await client.OrdersGETAsync(1, false);
Debug.Assert(allOrders.Data.Select(c => c.Id).Contains(orderId));
var receiversResponse = await client.ReceiversGETAsync(orderId);
Debug.Assert(receiversResponse.Data.Any(c => c.Id == receiverPostResponse1.Data.Id));
Debug.Assert(receiversResponse.Data.Any(c => c.Id == receiverPostResponse2.Data.Id));

logger.LogInformation("Inspect the order online: {Url}", $"https://lightr.app/shoppingcart?orderid={orderResponse.Data.Id}");

// Wait before deleting, so you can inspect the order online.
Debugger.Break();

await client.OrdersDELETEAsync(orderId);
logger.LogInformation("Deleted order: {OrderId}", orderId);

// Wait before exiting.
Debugger.Break();