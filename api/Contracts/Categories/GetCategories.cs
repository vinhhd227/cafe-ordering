namespace api.Contracts.Categories;

public static class GetCategories
{
    public record Response(List<CategoryDto> Categories);
    
    public record CategoryDto(int Id, string Name);
}