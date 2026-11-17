using SpaceHub.Application.Exceptions;
using SpaceHub.Domain.Entities;
using SpaceHub.Domain.Entities.Enums;

namespace SpaceHub.Application.Bookings;

public static class BookingRules
{
    public static void ValidatePeriod(Space space, DateTime start, DateTime end, int attendees)
    {
        if (!space.IsActive)
        {
            throw Invalid("Space is not active.");
        }

        if (start <= DateTime.UtcNow || end <= start)
        {
            throw Invalid("Booking must be in the future.");
        }

        if (attendees > space.Capacity)
        {
            throw Invalid("Attendees exceed the space capacity.");
        }

     
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tbilisi");
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(start, DateTimeKind.Utc), zone);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(end, DateTimeKind.Utc), zone);
        var hours = space.WorkingHours.SingleOrDefault(day => day.DayOfWeek == localStart.DayOfWeek);

        if (localStart.Date != localEnd.Date || hours == null)
        {
            throw Invalid("Booking must fit within one local day's working hours.");
        }

        bool startsTooEarly = TimeOnly.FromDateTime(localStart) < hours.OpensAt;
        bool endsTooLate = TimeOnly.FromDateTime(localEnd) > hours.ClosesAt;

        if (startsTooEarly || endsTooLate)
        {
            throw Invalid("Booking must fit within one local day's working hours.");
        }
    }

    public static Booking Price(Space space, BookingQuoteRequest request)
    {
        ValidatePeriod(space, request.StartsAt.UtcDateTime, request.EndsAt.UtcDateTime, request.AttendeeCount);

        // 1. სივრცის ფასი = საათების რაოდენობა × საათობრივი ფასი.
        decimal durationHours = (decimal)(request.EndsAt - request.StartsAt).Ticks / TimeSpan.TicksPerHour;
        var booking = new Booking
        {
            Space = space,
            SpaceId = space.Id,
            StartsAt = request.StartsAt.UtcDateTime,
            EndsAt = request.EndsAt.UtcDateTime,
            AttendeeCount = request.AttendeeCount,
            HourlyRateSnapshot = space.HourlyRate,
            SpaceSubtotal = Math.Round(space.HourlyRate * durationHours, 2, MidpointRounding.AwayFromZero)
        };

        // ეს სია თავიდან გვარიდებს სტუმარზე მომსახურების ორჯერ დათვლას.
        var countedPerPersonServices = new HashSet<int>();

        // 2. პაკეტი და პაკეტით დაუფარავი სტუმრების მომსახურება.
        if (request.PackageId.HasValue)
        {
            AddPackage(space, request, booking, countedPerPersonServices);
        }

        // 3. მომხმარებლის მიერ დამატებით არჩეული მომსახურებები.
        foreach (var requested in request.AdditionalServices)
        {
            var service = GetActiveService(space, requested.SpaceServiceId);

            if (service.PricingType == ServicePricingType.PerPerson)
            {
                if (requested.Quantity.HasValue)
                {
                    throw Invalid("Per-person quantities are calculated from attendeeCount; omit quantity.");
                }

                if (!countedPerPersonServices.Contains(service.Id))
                {
                    AddServiceLine(booking, service, request.AttendeeCount, false);
                    countedPerPersonServices.Add(service.Id);
                }
            }
            else
            {
                if (!requested.Quantity.HasValue)
                {
                    throw Invalid("Per-unit services require a quantity.");
                }

                AddServiceLine(booking, service, requested.Quantity.Value, false);
            }
        }

        // 4. პაკეტში შესულ ხაზებზე Subtotal არის 0: მათი ფასი უკვე პაკეტშია.
        booking.ServicesSubtotal = booking.Services.Sum(line => line.Subtotal);
        booking.TotalAmount = booking.SpaceSubtotal + booking.PackagePriceSnapshot + booking.ServicesSubtotal;
        return booking;
    }

    private static void AddPackage(Space space, BookingQuoteRequest request, Booking booking, HashSet<int> countedServices)
    {
        var package = space.Packages.SingleOrDefault(item => item.Id == request.PackageId && item.IsActive);
        if (package == null)
        {
            throw Invalid("Selected package is inactive or does not belong to this space.");
        }

        // სახელიც და ფასიც ინახება, რათა კატალოგის მომავალმა ცვლილებამ ჯავშანი არ შეცვალოს.
        booking.PackageId = package.Id;
        booking.PackageNameSnapshot = package.Name;
        booking.PackagePriceSnapshot = package.Price;

        foreach (var item in package.Items.OrderBy(item => item.SpaceServiceId))
        {
            var service = GetActiveService(space, item.SpaceServiceId);
            AddServiceLine(booking, service, item.Quantity, true);

            if (service.PricingType == ServicePricingType.PerPerson)
            {
                int extraGuests = Math.Max(0, request.AttendeeCount - item.Quantity);
                AddServiceLine(booking, service, extraGuests, false);
                countedServices.Add(service.Id);
            }
        }
    }

    private static SpaceService GetActiveService(Space space, int serviceId)
    {
        var service = space.Services.SingleOrDefault(item => item.Id == serviceId);
        if (service == null || !service.IsActive)
        {
            throw Invalid("Selected service is inactive or does not belong to this space.");
        }

        return service;
    }

    private static void AddServiceLine(Booking booking, SpaceService service, int quantity, bool includedInPackage)
    {
        if (quantity <= 0)
        {
            return;
        }

        decimal subtotal = 0;
        if (!includedInPackage)
        {
            subtotal = quantity * service.UnitPrice;
        }

        var line = new BookingService
        {
            SpaceServiceId = service.Id,
            NameSnapshot = service.Name,
            PricingTypeSnapshot = service.PricingType,
            Quantity = quantity,
            UnitPriceSnapshot = service.UnitPrice,
            Subtotal = subtotal,
            IsIncludedInPackage = includedInPackage
        };
        booking.Services.Add(line);
    }

    public static BusinessException Invalid(string message)
    {
        return new BusinessException(FailureKind.Validation, message);
    }
}
