using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StoreManagement.IntegrationTests.Helpers;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using Xunit;

namespace StoreManagement.IntegrationTests.Controllers;

public class InvoicesControllerTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly HttpClient _client;

    public InvoicesControllerTests(StoreManagementApiFactory factory)
    {
        _client = factory.CreateClient();
        factory.SeedDatabase();
    }

    [Fact]
    public async Task CreateSaleInvoice_Success()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        _client.DefaultRequestHeaders.Add("X-Test-BranchId", "1");

        var dto = new CreateInvoiceDto
        {
            InvoiceType = 1,
            CustomerId = 1,
            Date = System.DateTime.UtcNow,
            BranchId = 1,
            Paid = 100,
            IdempotencyKey = System.Guid.NewGuid().ToString(),
            Items = new List<CreateInvoiceItemDto>
            {
                new CreateInvoiceItemDto
                {
                    ProductId = 1,
                    Quantity = 2,
                    UnitPrice = 100
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/invoices", dto);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<InvoiceReadDto>>();
        
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.InvoiceType.Should().Be("Sale");
        result.Data.TotalValue.Should().Be(200);
    }

    [Fact]
    public async Task CreateSaleInvoice_DuplicateIdempotencyKey_Fails()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        _client.DefaultRequestHeaders.Add("X-Test-BranchId", "1");

        var idempotencyKey = System.Guid.NewGuid().ToString();

        var dto = new CreateInvoiceDto
        {
            InvoiceType = 1,
            CustomerId = 1,
            Date = System.DateTime.UtcNow,
            BranchId = 1,
            Paid = 0,
            IdempotencyKey = idempotencyKey,
            Items = new List<CreateInvoiceItemDto>
            {
                new CreateInvoiceItemDto
                {
                    ProductId = 1,
                    Quantity = 1,
                    UnitPrice = 50
                }
            }
        };

        // First attempt should succeed
        var response1 = await _client.PostAsJsonAsync("/api/v1/invoices", dto);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second attempt with same key should fail with InternalServerError (or BadRequest if mapped)
        var response2 = await _client.PostAsJsonAsync("/api/v1/invoices", dto);
        response2.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task CreateSaleInvoice_NoCustomer_Fails()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        _client.DefaultRequestHeaders.Add("X-Test-BranchId", "1");

        var dto = new CreateInvoiceDto
        {
            // Missing CustomerId
            InvoiceType = 1,
            Date = System.DateTime.UtcNow,
            BranchId = 1,
            IdempotencyKey = System.Guid.NewGuid().ToString(),
            Items = new List<CreateInvoiceItemDto>
            {
                new CreateInvoiceItemDto
                {
                    ProductId = 1,
                    Quantity = 1,
                    UnitPrice = 50
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/invoices", dto);
        response.IsSuccessStatusCode.Should().BeFalse();
    }
}
