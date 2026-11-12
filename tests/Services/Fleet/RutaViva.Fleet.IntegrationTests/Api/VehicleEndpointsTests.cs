using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RutaViva.Fleet.Api.Contracts;
using RutaViva.Fleet.Domain.Common;
using RutaViva.Fleet.IntegrationTests.Persistence;
using RutaViva.Testing.Fixtures;

namespace RutaViva.Fleet.IntegrationTests.Api;

[Trait("Category", "Integration")]
public sealed class VehicleEndpointsTests(SqlServerTestDatabase database) : IClassFixture<SqlServerTestDatabase>
{
    private const string VehiclesRoute = "/api/v1/fleet/vehicles";
    private const string LocationFormat = VehiclesRoute + "/{0}";
    private const string StatusRouteFormat = "/api/v1/fleet/vehicles/{0}/status";
    private const string ByPlateRouteFormat = "/api/v1/fleet/vehicles/by-plate/{0}";
    private const string ListByCustomerRouteFormat = "/api/v1/fleet/vehicles?customerId={0}";
    private const string CreatedPlate = "API101";
    private const string InvalidPlate = "12-ABC";
    private const string DuplicatedPlate = "API102";
    private const string OwnerlessPlate = "API103";
    private const string StatusPlate = "API104";
    private const string LookupPlate = "API105";
    private const string ListPlate = "API106";
    private const string StatusProperty = "status";
    private const string ActiveStatus = "Active";
    private const string InactiveStatus = "Inactive";
    private const string IdProperty = "id";
    private const string CodeProperty = "code";
    private const string PlateProperty = "plate";
    private const string ItemsProperty = "items";
    private const string OwnerNotFoundCode = "Vehicle.OwnerNotFound";
    private const string PlateAlreadyRegisteredCode = "Vehicle.PlateAlreadyRegistered";
    private const string ValidationCode = "Validation.Failed";
    private const int ExpectedSingleItem = 1;

    [Fact]
    public async Task PostVehicle_Should_ReturnCreatedWithLocation_When_RequestIsValid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (CustomerId customer, WorkshopId workshop) = await FleetTestData.SeedPartiesAsync(database, cancellationToken);
        await using FleetApiFactory factory = await FleetApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(VehiclesRoute, BuildRequest(CreatedPlate, customer.Value, workshop.Value), cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        string id = body.GetProperty(IdProperty).GetString()!;
        response.Headers.Location!.ToString().Should().Be(string.Format(LocationFormat, id));
    }

    [Fact]
    public async Task PostVehicle_Should_ReturnBadRequest_When_PlateIsInvalid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (CustomerId customer, WorkshopId workshop) = await FleetTestData.SeedPartiesAsync(database, cancellationToken);
        await using FleetApiFactory factory = await FleetApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(VehiclesRoute, BuildRequest(InvalidPlate, customer.Value, workshop.Value), cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.GetProperty(CodeProperty).GetString().Should().Be(ValidationCode);
    }

    [Fact]
    public async Task PostVehicle_Should_ReturnConflict_When_PlateIsAlreadyRegistered()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (CustomerId customer, WorkshopId workshop) = await FleetTestData.SeedPartiesAsync(database, cancellationToken);
        await using FleetApiFactory factory = await FleetApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage first = await client.PostAsJsonAsync(VehiclesRoute, BuildRequest(DuplicatedPlate, customer.Value, workshop.Value), cancellationToken);

        using HttpResponseMessage second = await client.PostAsJsonAsync(VehiclesRoute, BuildRequest(DuplicatedPlate, customer.Value, workshop.Value), cancellationToken);
        JsonElement body = await second.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        body.GetProperty(CodeProperty).GetString().Should().Be(PlateAlreadyRegisteredCode);
    }

    [Fact]
    public async Task PostVehicle_Should_ReturnUnprocessableEntity_When_OwnerDoesNotExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (CustomerId _, WorkshopId workshop) = await FleetTestData.SeedPartiesAsync(database, cancellationToken);
        await using FleetApiFactory factory = await FleetApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(VehiclesRoute, BuildRequest(OwnerlessPlate, Guid.NewGuid(), workshop.Value), cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        body.GetProperty(CodeProperty).GetString().Should().Be(OwnerNotFoundCode);
    }

    [Fact]
    public async Task PatchStatus_Should_ReturnNoContent_When_RequestedStatusIsAlreadyApplied()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (CustomerId customer, WorkshopId workshop) = await FleetTestData.SeedPartiesAsync(database, cancellationToken);
        await using FleetApiFactory factory = await FleetApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        Guid vehicleId = await RegisterAsync(client, StatusPlate, customer.Value, workshop.Value, cancellationToken);
        string statusRoute = string.Format(StatusRouteFormat, vehicleId);

        using HttpResponseMessage unchanged = await client.PatchAsJsonAsync(statusRoute, BuildStatusRequest(ActiveStatus), cancellationToken);
        using HttpResponseMessage deactivated = await client.PatchAsJsonAsync(statusRoute, BuildStatusRequest(InactiveStatus), cancellationToken);
        using HttpResponseMessage repeated = await client.PatchAsJsonAsync(statusRoute, BuildStatusRequest(InactiveStatus), cancellationToken);

        unchanged.StatusCode.Should().Be(HttpStatusCode.NoContent);
        deactivated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        repeated.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetByPlate_Should_ReturnVehicle_When_PlateIsRequestedInLowerCase()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (CustomerId customer, WorkshopId workshop) = await FleetTestData.SeedPartiesAsync(database, cancellationToken);
        await using FleetApiFactory factory = await FleetApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        await RegisterAsync(client, LookupPlate, customer.Value, workshop.Value, cancellationToken);

        using HttpResponseMessage response = await client.GetAsync(string.Format(ByPlateRouteFormat, LookupPlate.ToLowerInvariant()), cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty(PlateProperty).GetString().Should().Be(LookupPlate);
        body.GetProperty(StatusProperty).GetString().Should().Be(ActiveStatus);
    }

    [Fact]
    public async Task GetVehicles_Should_ReturnOnlyCustomerVehicles_When_CustomerFilterIsGiven()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (CustomerId customer, WorkshopId workshop) = await FleetTestData.SeedPartiesAsync(database, cancellationToken);
        await using FleetApiFactory factory = await FleetApiFactory.CreateAsync(database, cancellationToken);
        using HttpClient client = factory.CreateClient();
        await RegisterAsync(client, ListPlate, customer.Value, workshop.Value, cancellationToken);

        using HttpResponseMessage response = await client.GetAsync(string.Format(ListByCustomerRouteFormat, customer.Value), cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty(ItemsProperty).GetArrayLength().Should().Be(ExpectedSingleItem);
    }

    private static RegisterVehicleRequest BuildRequest(string plate, Guid customerId, Guid workshopId) =>
        new(plate, null, FleetTestData.BrandText, FleetTestData.ModelText, FleetTestData.ModelYear, FleetTestData.OdometerKm, customerId, workshopId, null);

    private static Dictionary<string, string> BuildStatusRequest(string status) =>
        new() { [StatusProperty] = status };

    private static async Task<Guid> RegisterAsync(HttpClient client, string plate, Guid customerId, Guid workshopId, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(VehiclesRoute, BuildRequest(plate, customerId, workshopId), cancellationToken);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return body.GetProperty(IdProperty).GetGuid();
    }
}
