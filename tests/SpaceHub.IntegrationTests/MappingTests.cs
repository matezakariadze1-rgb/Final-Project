using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using SpaceHub.Application;
using Xunit;
namespace SpaceHub.IntegrationTests;
public sealed class MappingTests
{
    [Fact]
    public void All_profiles_have_valid_destination_mappings()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IConfigurationProvider>().AssertConfigurationIsValid();
    }
}
