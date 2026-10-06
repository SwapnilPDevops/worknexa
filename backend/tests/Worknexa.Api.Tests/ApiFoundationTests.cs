using System.Net;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Worknexa.Api.Errors;
using Worknexa.Application.Common;
using Worknexa.Application.Configuration;
using Worknexa.Application.Validation;

namespace Worknexa.Api.Tests;

public sealed class ApiFoundationTests : IClassFixture<Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>>
{
    private readonly Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> _factory;

    public ApiFoundationTests(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SwaggerDocument_IsAvailable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/health", json);
    }

    [Fact]
    public async Task ValidationException_UsesStandardErrorShape()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new ValidationException([
            new ValidationFailure("page", "Page must be at least 1.")
        ]);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("VALIDATION_ERROR", document.RootElement.GetProperty("code").GetString());
        Assert.Equal("Validation failed", document.RootElement.GetProperty("message").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("errors").GetArrayLength());
        Assert.Equal("page", document.RootElement.GetProperty("errors")[0].GetProperty("field").GetString());
    }

    [Fact]
    public void PageRequestValidator_RejectsPageZero()
    {
        var validator = new PageRequestValidator(Options.Create(new PaginationSettings()));

        var result = validator.Validate(new PageRequest { Page = 0, PageSize = 20 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(PageRequest.Page));
    }

    [Fact]
    public void ApplySort_OrdersAllowedProperties()
    {
        var query = new[]
        {
            new SortItem { Name = "b" },
            new SortItem { Name = "a" }
        }.AsQueryable();

        var sorted = query.ApplySort(
            new SortRequest { SortBy = nameof(SortItem.Name), Direction = SortDirection.Asc },
            new HashSet<string> { nameof(SortItem.Name) });

        Assert.Equal(["a", "b"], sorted.Select(item => item.Name).ToArray());
    }

    [Fact]
    public void ApplySort_RejectsUnknownProperties()
    {
        var query = new[] { new SortItem { Name = "a" } }.AsQueryable();

        var exception = Assert.Throws<InvalidSortException>(() =>
            query.ApplySort(
                new SortRequest { SortBy = "Missing" },
                new HashSet<string> { nameof(SortItem.Name) }));

        Assert.Equal("Missing", exception.PropertyName);
    }

    private sealed class SortItem
    {
        public string Name { get; init; } = string.Empty;
    }
}
