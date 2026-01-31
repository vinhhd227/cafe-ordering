namespace api.Contracts.Menu;

public static class GetMenu
{
    public record Response(List<CategoryDto> Categories);

    public record CategoryDto(int Id, string Name, List<ProductDto> Products);

    public record ProductDto(
        int Id,
        string Name,
        string? Description,
        decimal Price,
        string? ImageUrl,
        bool HasTemperatureOption,
        bool HasIceLevelOption,
        bool HasSugarLevelOption
    );
}
