using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Worknexa.Api.Hosting;

namespace Worknexa.Api.Tests;

public sealed class ProductionHostingTests
{
    [Fact]
    public void ForwardedHeaders_RefuseToTrustEveryProxy()
    {
        var configuration = Configuration(("ForwardedHeaders:Enabled", "true"));

        var error = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddWorknexaHosting(configuration, Environment()));

        Assert.Contains("KnownNetworks", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ForwardedHeaders_TrustOnlyTheConfiguredNetwork()
    {
        var configuration = Configuration(
            ("ForwardedHeaders:Enabled", "true"),
            ("ForwardedHeaders:KnownNetworks:0", "172.30.0.0/24"));

        var services = new ServiceCollection().AddWorknexaHosting(configuration, Environment());
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        var network = Assert.Single(options.KnownIPNetworks);
        Assert.Equal("172.30.0.0/24", network.ToString());
        Assert.Empty(options.KnownProxies);
        Assert.Equal(1, options.ForwardLimit);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("false", false)]
    [InlineData("true", true)]
    public void ApiDocs_AreOffUnlessEnabled(string? value, bool expected)
    {
        var configuration = value is null ? Configuration() : Configuration(("ApiDocs:Enabled", value));

        Assert.Equal(expected, configuration.ApiDocsEnabled());
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();
    }

    private static IWebHostEnvironment Environment()
    {
        return new TestEnvironment();
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ApplicationName { get; set; } = "Worknexa.Api";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ContentRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = Environments.Production;
    }
}
