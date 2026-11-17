namespace SpaceHub.Application.Catalog;

public sealed record CreatePackageRequest(string Name, string Description, decimal Price, List<PackageItemRequest> Items, byte[] SpaceRowVersion);
