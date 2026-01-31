using api.Contracts.Categories;
using api.Domain.Entities;
using AutoMapper;

namespace api.Infrastructure.Mapping;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, GetCategories.CategoryDto>();
    }
}
