using Lightr;
using Newtonsoft.Json.Linq;
using WireMock.Handlers;
using WireMock.Server;
using WireMock.Settings;

namespace SampleLightrApp.Tests;

public sealed class LightrApiFixture : IDisposable
{
    public LightrApiFixture()
    {
        MappingRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        Server = WireMockServer.Start(new WireMockServerSettings
        {
            FileSystemHandler = new LocalFileSystemHandler(MappingRoot),
            ReadStaticMappings = true,
            Urls = ["http://localhost:55330"]
        });
    }

    public WireMockServer Server { get; }
    public string MappingRoot { get; }

    public void Dispose()
    {
        Server.Stop();
        Server.Dispose();
    }
}

public sealed class LightrClientTests : IClassFixture<LightrApiFixture>
{
    private readonly LightrApiFixture _fixture;

    public LightrClientTests(LightrApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MeAsync_returns_a_sanitized_profile_from_the_recorded_route()
    {
        var response = await CreateClient().MeAsync(TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(response.Data.First_name));
        Assert.False(string.IsNullOrWhiteSpace(response.Data.Last_name));
        Assert.False(string.IsNullOrWhiteSpace(response.Data.Email));
    }

    [Fact]
    public async Task Catalog_methods_return_data_from_static_mappings()
    {
        var client = CreateClient();

        var cancellationToken = TestContext.Current.CancellationToken;
        var fonts = await client.FontsAsync(cancellationToken);
        var presets = await client.PresetsGETAsync(cancellationToken: cancellationToken);
        var (presetId, fontId) = GetRecordedOrderInput();
        var preset = await client.PresetsGET2Async(presetId, cancellationToken);
        var template = await client.TemplatesGET2Async(preset.Data.Base_template_id, cancellationToken);

        Assert.Contains(fonts.Data, font => font.Id == fontId);
        Assert.Contains(presets.Data, item => item.Id == presetId);
        Assert.False(string.IsNullOrWhiteSpace(preset.Data.Label));
        Assert.False(string.IsNullOrWhiteSpace(template.Data.Label));
    }

    [Fact]
    public async Task Order_and_receiver_methods_replay_the_recorded_workflow()
    {
        var client = CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var (presetId, fontId) = GetRecordedOrderInput();

        var createdOrder = await client.OrdersPOSTAsync(new Body15
        {
            Preset_id = presetId,
            Quantity = 1,
            Font_id = fontId,
            Type = Body15Type.Send_multiple
        }, cancellationToken);
        var order = await client.OrdersGET2Async(createdOrder.Data.Id.ToString(), cancellationToken);
        var createdReceiver = await client.ReceiversPOSTAsync(
            createdOrder.Data.Id,
            GetRecordedReceiverInput(createdOrder.Data.Id),
            cancellationToken);
        var orders = await client.OrdersGETAsync(1, false, cancellationToken: cancellationToken);
        var receivers = await client.ReceiversGETAsync(createdOrder.Data.Id, cancellationToken: cancellationToken);
        await client.OrdersDELETEAsync(createdOrder.Data.Id, cancellationToken);

        Assert.Equal(createdOrder.Data.Id, order.Data.Id);
        Assert.NotEqual(Guid.Empty, createdReceiver.Data.Id);
        Assert.Contains(orders.Data, item => item.Id == createdOrder.Data.Id);
        Assert.Contains(receivers.Data, item => item.Id == createdReceiver.Data.Id);
    }

    private LightrClient CreateClient()
    {
        var client = new LightrClient(new HttpClient());
        client.BaseUrl = _fixture.Server.Url!;
        return client;
    }

    private (Guid PresetId, Guid FontId) GetRecordedOrderInput()
    {
        var pattern = GetRawRequestPattern("Proxy Mapping for _POST_api_v1_orders.json");
        return (GetGuid(pattern, "preset_id"), GetGuid(pattern, "font_id"));
    }

    private Body14 GetRecordedReceiverInput(Guid orderId)
    {
        var pattern = GetRawRequestPattern(
            $"Proxy Mapping for _POST_api_v1_orders_{orderId}_receivers.json");

        return new Body14
        {
            Id = GetGuid(pattern, "id"),
            Order_id = GetGuid(pattern, "order_id"),
            Name = pattern.Value<string>("name")!,
            Country_id = pattern["country_id"]?.Type == JTokenType.Null
                ? null
                : GetGuid(pattern, "country_id"),
            Text_variables = pattern["text_variables"]!,
            Qr_variables = pattern["qr_variables"]!.ToObject<List<Qr_variables>>()!,
            Received_by_extractor_at = pattern.Value<DateTimeOffset?>("received_by_extractor_at"),
            Address = pattern.Value<string>("address"),
            Postal_code = pattern.Value<string>("postal_code"),
            City = pattern.Value<string>("city"),
            Allow_empty_text_variables = pattern.Value<bool?>("allow_empty_text_variables")
        };
    }

    private JObject GetRawRequestPattern(string fileName)
    {
        var path = Path.Combine(_fixture.MappingRoot, "__admin", "mappings", fileName);
        var mapping = JObject.Parse(File.ReadAllText(path));
        return (JObject)mapping["Request"]!["Body"]!["Matcher"]!["Pattern"]!;
    }

    private static Guid GetGuid(JObject values, string propertyName)
    {
        return Guid.Parse(values.Value<string>(propertyName)!);
    }
}
