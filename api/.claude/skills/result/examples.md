# Result Pattern Advanced Examples

Real-world examples and advanced patterns for Ardalis.Result.

## Complete E-Commerce Order Processing

```csharp
public class OrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IPaymentService _paymentService;
    private readonly IInventoryService _inventoryService;
    private readonly IValidator<CreateOrderDto> _validator;

    public async Task<Result<OrderConfirmationDto>> CreateOrderAsync(CreateOrderDto dto)
    {
        // Railway Oriented Programming - chain of operations
        return await ValidateOrderAsync(dto)
            .BindAsync(_ => GetCustomerAsync(dto.CustomerId))
            .BindAsync(customer => CheckCustomerStatusAsync(customer))
            .BindAsync(customer => ValidateInventoryAsync(dto.Items))
            .BindAsync(_ => ReserveInventoryAsync(dto.Items))
            .BindAsync(_ => CreateOrderEntityAsync(dto))
            .BindAsync(order => ProcessPaymentAsync(order, dto.PaymentInfo))
            .BindAsync(order => FinalizeOrderAsync(order))
            .MapAsync(order => GenerateConfirmationAsync(order));
    }

    private async Task<Result> ValidateOrderAsync(CreateOrderDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Result.Invalid(validation.AsErrors());
        }
        return Result.Success();
    }

    private async Task<Result<Customer>> GetCustomerAsync(int customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null)
        {
            return Result<Customer>.NotFound($"Customer {customerId} not found");
        }
        return Result<Customer>.Success(customer);
    }

    private async Task<Result<Customer>> CheckCustomerStatusAsync(Customer customer)
    {
        if (!customer.IsActive)
        {
            return Result<Customer>.Invalid(new ValidationError
            {
                Identifier = nameof(Customer),
                ErrorMessage = "Customer account is not active",
                ErrorCode = "CUSTOMER_INACTIVE"
            });
        }

        if (customer.IsBlocked)
        {
            return Result<Customer>.Forbidden("Customer account is blocked");
        }

        return Result<Customer>.Success(customer);
    }

    private async Task<Result> ValidateInventoryAsync(List<OrderItemDto> items)
    {
        var validationErrors = new List<ValidationError>();

        foreach (var item in items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null)
            {
                validationErrors.Add(new ValidationError
                {
                    Identifier = $"Items[{item.ProductId}]",
                    ErrorMessage = $"Product {item.ProductId} not found",
                    ErrorCode = "PRODUCT_NOT_FOUND"
                });
                continue;
            }

            if (product.Stock < item.Quantity)
            {
                validationErrors.Add(new ValidationError
                {
                    Identifier = $"Items[{item.ProductId}].Quantity",
                    ErrorMessage = $"Insufficient stock for {product.Name}. Available: {product.Stock}",
                    ErrorCode = "INSUFFICIENT_STOCK"
                });
            }
        }

        if (validationErrors.Any())
        {
            return Result.Invalid(validationErrors);
        }

        return Result.Success();
    }

    private async Task<Result> ReserveInventoryAsync(List<OrderItemDto> items)
    {
        try
        {
            await _inventoryService.ReserveAsync(items);
            return Result.Success();
        }
        catch (InventoryException ex)
        {
            return Result.Error($"Failed to reserve inventory: {ex.Message}");
        }
    }

    private async Task<Result<Order>> CreateOrderEntityAsync(CreateOrderDto dto)
    {
        var order = new Order(dto.CustomerId, dto.Items);
        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();
        return Result<Order>.Success(order);
    }

    private async Task<Result<Order>> ProcessPaymentAsync(Order order, PaymentInfoDto paymentInfo)
    {
        var paymentResult = await _paymentService.ProcessPaymentAsync(
            order.TotalAmount,
            paymentInfo
        );

        if (!paymentResult.IsSuccess)
        {
            // Rollback inventory reservation
            await _inventoryService.ReleaseAsync(order.Items);
            
            return paymentResult.Status switch
            {
                ResultStatus.Invalid => Result<Order>.Invalid(paymentResult.ValidationErrors),
                ResultStatus.Forbidden => Result<Order>.Forbidden("Payment declined"),
                _ => Result<Order>.Error("Payment processing failed")
            };
        }

        order.MarkAsPaid(paymentResult.Value.TransactionId);
        await _orderRepository.SaveChangesAsync();
        return Result<Order>.Success(order);
    }

    private async Task<Result<Order>> FinalizeOrderAsync(Order order)
    {
        order.Finalize();
        await _orderRepository.SaveChangesAsync();
        return Result<Order>.Success(order);
    }

    private async Task<OrderConfirmationDto> GenerateConfirmationAsync(Order order)
    {
        return new OrderConfirmationDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            EstimatedDeliveryDate = DateTime.Now.AddDays(7)
        };
    }
}
```

## Multi-Step User Registration

```csharp
public class UserRegistrationService
{
    private readonly IRepository<User> _userRepository;
    private readonly IEmailService _emailService;
    private readonly IValidator<RegisterUserDto> _validator;
    private readonly IPasswordHasher _passwordHasher;

    public async Task<Result<UserDto>> RegisterUserAsync(RegisterUserDto dto)
    {
        return await ValidateRegistrationAsync(dto)
            .BindAsync(_ => CheckEmailAvailabilityAsync(dto.Email))
            .BindAsync(_ => CreateUserAccountAsync(dto))
            .BindAsync(user => SendVerificationEmailAsync(user))
            .MapAsync(user => MapToUserDtoAsync(user));
    }

    private async Task<Result> ValidateRegistrationAsync(RegisterUserDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Result.Invalid(validation.AsErrors());
        }

        // Additional business rules
        if (dto.Age < 18)
        {
            return Result.Invalid(new ValidationError
            {
                Identifier = nameof(dto.Age),
                ErrorMessage = "You must be at least 18 years old to register",
                ErrorCode = "AGE_REQUIREMENT"
            });
        }

        return Result.Success();
    }

    private async Task<Result> CheckEmailAvailabilityAsync(string email)
    {
        var existingUser = await _userRepository.FirstOrDefaultAsync(
            new UserByEmailSpec(email)
        );

        if (existingUser != null)
        {
            return Result.Conflict("Email address is already in use");
        }

        return Result.Success();
    }

    private async Task<Result<User>> CreateUserAccountAsync(RegisterUserDto dto)
    {
        var passwordHash = _passwordHasher.Hash(dto.Password);
        
        var user = new User(
            email: dto.Email,
            firstName: dto.FirstName,
            lastName: dto.LastName,
            passwordHash: passwordHash
        );

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return Result<User>.Success(user);
    }

    private async Task<Result<User>> SendVerificationEmailAsync(User user)
    {
        try
        {
            await _emailService.SendVerificationEmailAsync(user.Email, user.VerificationToken);
            return Result<User>.Success(user);
        }
        catch (EmailException ex)
        {
            // Log error but don't fail registration
            // User can request resend later
            return Result<User>.Success(user);
        }
    }

    private Task<UserDto> MapToUserDtoAsync(User user)
    {
        return Task.FromResult(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            IsVerified = user.IsVerified
        });
    }
}
```

## File Upload with Validation

```csharp
public class DocumentService
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private static readonly string[] AllowedExtensions = { ".pdf", ".docx", ".xlsx" };

    private readonly IRepository<Document> _documentRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IVirusScanner _virusScanner;

    public async Task<Result<DocumentDto>> UploadDocumentAsync(
        int userId,
        IFormFile file,
        string category)
    {
        return await ValidateFileAsync(file)
            .BindAsync(_ => ScanForVirusesAsync(file))
            .BindAsync(_ => SaveFileAsync(file))
            .BindAsync(filePath => CreateDocumentRecordAsync(userId, file, filePath, category))
            .MapAsync(document => MapToDtoAsync(document));
    }

    private async Task<Result> ValidateFileAsync(IFormFile file)
    {
        var errors = new List<ValidationError>();

        if (file == null || file.Length == 0)
        {
            errors.Add(new ValidationError
            {
                Identifier = nameof(file),
                ErrorMessage = "File is required",
                ErrorCode = "FILE_REQUIRED"
            });
        }

        if (file != null && file.Length > MaxFileSize)
        {
            errors.Add(new ValidationError
            {
                Identifier = nameof(file),
                ErrorMessage = $"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024}MB",
                ErrorCode = "FILE_TOO_LARGE"
            });
        }

        var extension = Path.GetExtension(file?.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            errors.Add(new ValidationError
            {
                Identifier = nameof(file),
                ErrorMessage = $"File type {extension} is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}",
                ErrorCode = "INVALID_FILE_TYPE"
            });
        }

        if (errors.Any())
        {
            return Result.Invalid(errors);
        }

        return Result.Success();
    }

    private async Task<Result> ScanForVirusesAsync(IFormFile file)
    {
        var scanResult = await _virusScanner.ScanAsync(file);
        
        if (scanResult.IsInfected)
        {
            return Result.Invalid(new ValidationError
            {
                ErrorMessage = "File contains malicious content and cannot be uploaded",
                ErrorCode = "VIRUS_DETECTED"
            });
        }

        return Result.Success();
    }

    private async Task<Result<string>> SaveFileAsync(IFormFile file)
    {
        try
        {
            var filePath = await _fileStorage.SaveAsync(file);
            return Result<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            return Result<string>.Error($"Failed to save file: {ex.Message}");
        }
    }

    private async Task<Result<Document>> CreateDocumentRecordAsync(
        int userId,
        IFormFile file,
        string filePath,
        string category)
    {
        var document = new Document(
            userId: userId,
            fileName: file.FileName,
            filePath: filePath,
            fileSize: file.Length,
            contentType: file.ContentType,
            category: category
        );

        await _documentRepository.AddAsync(document);
        await _documentRepository.SaveChangesAsync();

        return Result<Document>.Success(document);
    }

    private Task<DocumentDto> MapToDtoAsync(Document document)
    {
        return Task.FromResult(new DocumentDto
        {
            Id = document.Id,
            FileName = document.FileName,
            FileSize = document.FileSize,
            ContentType = document.ContentType,
            Category = document.Category,
            UploadedAt = document.CreatedAt
        });
    }
}
```

## API Controller Examples

### RESTful API Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IMediator _mediator;

    [HttpGet]
    [TranslateResultToActionResult]
    public async Task<Result<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null)
    {
        var query = new GetProductsQuery(page, pageSize, search, category);
        return await _mediator.Send(query);
    }

    [HttpGet("{id}")]
    [TranslateResultToActionResult]
    [ExpectedFailures(ResultStatus.NotFound)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<Result<ProductDto>> GetProduct(int id)
    {
        return await _productService.GetProductAsync(id);
    }

    [HttpPost]
    [TranslateResultToActionResult]
    [ExpectedFailures(ResultStatus.Invalid)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<Result<ProductDto>> CreateProduct(CreateProductDto dto)
    {
        return await _productService.CreateProductAsync(dto);
    }

    [HttpPut("{id}")]
    [TranslateResultToActionResult]
    [ExpectedFailures(ResultStatus.NotFound, ResultStatus.Invalid)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<Result<ProductDto>> UpdateProduct(int id, UpdateProductDto dto)
    {
        return await _productService.UpdateProductAsync(id, dto);
    }

    [HttpDelete("{id}")]
    [TranslateResultToActionResult]
    [ExpectedFailures(ResultStatus.NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<Result> DeleteProduct(int id)
    {
        return await _productService.DeleteProductAsync(id);
    }
}
```

### Minimal API Examples

```csharp
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products")
            .WithOpenApi();

        group.MapGet("/", GetProducts)
            .Produces<PagedResult<ProductDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id}", GetProduct)
            .Produces<ProductDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateProduct)
            .Produces<ProductDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{id}", UpdateProduct)
            .Produces<ProductDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", DeleteProduct)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetProducts(
        [AsParameters] ProductQueryParams queryParams,
        IProductService productService)
    {
        var result = await productService.GetProductsAsync(
            queryParams.Page,
            queryParams.PageSize,
            queryParams.Search,
            queryParams.Category
        );

        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> GetProduct(
        int id,
        IProductService productService)
    {
        var result = await productService.GetProductAsync(id);
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> CreateProduct(
        CreateProductDto dto,
        IProductService productService)
    {
        var result = await productService.CreateProductAsync(dto);
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> UpdateProduct(
        int id,
        UpdateProductDto dto,
        IProductService productService)
    {
        var result = await productService.UpdateProductAsync(id, dto);
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> DeleteProduct(
        int id,
        IProductService productService)
    {
        var result = await productService.DeleteProductAsync(id);
        return result.ToMinimalApiResult();
    }
}

public record ProductQueryParams(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Category = null
);
```

## Testing Examples

```csharp
public class CustomerServiceTests
{
    private readonly Mock<IRepository<Customer>> _repositoryMock;
    private readonly Mock<IValidator<CreateCustomerDto>> _validatorMock;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Customer>>();
        _validatorMock = new Mock<IValidator<CreateCustomerDto>>();
        _service = new CustomerService(_repositoryMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task GetCustomer_WhenCustomerExists_ReturnsSuccess()
    {
        // Arrange
        var customer = new Customer { Id = 1, Email = "test@example.com" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(customer);

        // Act
        var result = await _service.GetCustomerAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("test@example.com", result.Value.Email);
    }

    [Fact]
    public async Task GetCustomer_WhenCustomerNotFound_ReturnsNotFound()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _service.GetCustomerAsync(999);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task CreateCustomer_WithInvalidData_ReturnsInvalid()
    {
        // Arrange
        var dto = new CreateCustomerDto { Email = "invalid" };
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Email", "Invalid email format")
        });
        
        _validatorMock.Setup(v => v.ValidateAsync(dto, default))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _service.CreateCustomerAsync(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Single(result.ValidationErrors);
        Assert.Equal("Email", result.ValidationErrors.First().Identifier);
    }

    [Fact]
    public async Task CreateCustomer_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var dto = new CreateCustomerDto 
        { 
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };
        
        _validatorMock.Setup(v => v.ValidateAsync(dto, default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await _service.CreateCustomerAsync(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Customer>()), Times.Once);
    }
}
```

See SKILL.md for complete documentation and API reference.
