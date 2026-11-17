using SpaceHub.Application.Bookings;
using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Entities;
using SpaceHub.Domain.Entities.Enums;
using Xunit;

namespace SpaceHub.IntegrationTests;

public sealed class BookingRulesTests
{
    private static readonly DateTimeOffset Start = new(DateTime.UtcNow.Date.AddDays(10).AddHours(6), TimeSpan.Zero);

    private static Space CreateSpace()
    {
        var space = new Space { Id = 1, Name = "Test space", Capacity = 30, HourlyRate = 100, IsActive = true };
        space.WorkingHours.Add(new SpaceWorkingHours
        {
            DayOfWeek = Start.DayOfWeek, OpensAt = new TimeOnly(9, 0), ClosesAt = new TimeOnly(20, 0)
        });
        space.Services.Add(new SpaceService { Id = 1, Name = "Meal", UnitPrice = 20, PricingType = ServicePricingType.PerPerson, IsActive = true });
        space.Services.Add(new SpaceService { Id = 2, Name = "Projector", UnitPrice = 15, PricingType = ServicePricingType.PerUnit, IsActive = true });
        space.Packages.Add(new Package
        {
            Id = 1, Name = "Standard", Price = 300, IsActive = true,
            Items = new List<PackageItem> { new() { SpaceServiceId = 1, Quantity = 10 } }
        });
        return space;
    }

    [Theory]
    [InlineData(8, 500)]
    [InlineData(10, 500)]
    [InlineData(12, 540)]
    public void Package_charges_only_uncovered_guests_without_double_counting(int guests, int expected)
    {
        var result = BookingRules.Price(CreateSpace(), new BookingQuoteRequest(1, Start, Start.AddHours(2), guests, 1,
            new List<RequestedService> { new(1, null) }));
        Assert.Equal((decimal)expected, result.TotalAmount);
        Assert.Equal(0m, Assert.Single(result.Services, x => x.IsIncludedInPackage).Subtotal);
    }

    [Fact]
    public void Additional_units_and_fractional_hours_have_independent_prices_and_snapshots()
    {
        var space = CreateSpace();
        var result = BookingRules.Price(space, new BookingQuoteRequest(1, Start, Start.AddHours(1.5), 12, null,
            new List<RequestedService> { new(2, 3) }));
        space.HourlyRate = 999;
        space.Services.Single(x => x.Id == 2).UnitPrice = 999;
        Assert.Equal(150m, result.SpaceSubtotal);
        Assert.Equal(45m, result.ServicesSubtotal);
        Assert.Equal(195m, result.TotalAmount);
        Assert.Equal(15m, Assert.Single(result.Services).UnitPriceSnapshot);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, null)]
    public void Rejects_manual_person_count_or_missing_unit_count(int serviceId, int? quantity)
    {
        Assert.Throws<BusinessException>(() => BookingRules.Price(CreateSpace(),
            new BookingQuoteRequest(1, Start, Start.AddHours(2), 12, null,
                new List<RequestedService> { new(serviceId, quantity) })));
    }

    [Fact]
    public void Rejects_inactive_package_service()
    {
        var space = CreateSpace();
        space.Services.Single(x => x.Id == 1).IsActive = false;
        Assert.Throws<BusinessException>(() => BookingRules.Price(space,
            new BookingQuoteRequest(1, Start, Start.AddHours(2), 12, 1, new List<RequestedService>())));
    }

    [Fact]
    public void Rejects_capacity_overflow_and_time_outside_working_hours()
    {
        var space = CreateSpace();
        Assert.Throws<BusinessException>(() => BookingRules.ValidatePeriod(space, Start.UtcDateTime, Start.AddHours(2).UtcDateTime, 31));
        Assert.Throws<BusinessException>(() => BookingRules.ValidatePeriod(space, Start.AddHours(-2).UtcDateTime, Start.UtcDateTime, 12));
        BookingRules.ValidatePeriod(space, Start.AddHours(-1).UtcDateTime, Start.AddHours(10).UtcDateTime, 30);
    }
}
