namespace SpaceHub.Application.Catalog;

public sealed record UpdatePackageRequest(string Name, string Description, decimal Price, List<PackageItemRequest> Items, bool? IsActive, byte[] SpaceRowVersion);
