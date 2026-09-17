namespace Shared.Models.Dtos;

public class CompanyDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string OwnerId { get; init; }
    public int Treasury { get; init; }
}