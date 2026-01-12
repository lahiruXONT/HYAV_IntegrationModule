namespace Integration.Application.DTOs;

public sealed class SapODataResponse<T>
{
    public SapODataResults<T> D { get; set; } = new();
}

public sealed class SapODataResults<T>
{
    public List<T> Results { get; set; } = new();
}
