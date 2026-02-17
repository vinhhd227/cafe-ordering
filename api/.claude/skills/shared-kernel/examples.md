# SharedKernel Advanced Examples

Complete DDD patterns with Ardalis.SharedKernel.

## Complete E-Commerce Aggregate

```csharp
// Order aggregate root
public class Order : HasDomainEventsBase, IAggregateRoot
{
    public int CustomerId { get; private set; }
    public Address ShippingAddress { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }
    
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    
    private Order() { } // EF
    
    public Order(int customerId, Address shippingAddress)
    {
        CustomerId = Guard.Against.NegativeOrZero(customerId);
        ShippingAddress = Guard.Against.Null(shippingAddress);
        Status = OrderStatus.Draft;
        TotalAmount = Money.Zero();
        
        RegisterDomainEvent(new OrderCreatedEvent(this));
    }
    
    public void AddItem(int productId, string productName, int quantity, Money unitPrice)
    {
        Guard.Against.NegativeOrZero(quantity);
        var item = new OrderItem(productId, productName, quantity, unitPrice);
        _items.Add(item);
        RecalculateTotal();
        
        RegisterDomainEvent(new OrderItemAddedEvent(this, item));
    }
    
    public void Submit()
    {
        Guard.Against.InvalidInput(Status, nameof(Status), 
            s => s == OrderStatus.Draft, "Can only submit draft orders");
        Guard.Against.InvalidInput(_items.Count, nameof(Items),
            c => c > 0, "Order must have at least one item");
            
        Status = OrderStatus.Submitted;
        RegisterDomainEvent(new OrderSubmittedEvent(this));
    }
    
    private void RecalculateTotal()
    {
        TotalAmount = _items
            .Select(i => i.Subtotal)
            .Aggregate(Money.Zero(), (sum, price) => sum.Add(price));
    }
}

// Child entity (not aggregate root)
public class OrderItem : EntityBase
{
    public int ProductId { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money Subtotal => new Money(UnitPrice.Amount * Quantity, UnitPrice.Currency);
    
    private OrderItem() { }
    
    internal OrderItem(int productId, string productName, int quantity, Money unitPrice)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}

// Value Objects
public class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string ZipCode { get; private set; }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return ZipCode;
    }
}

public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return new Money(Amount + other.Amount, Currency);
    }
    
    public static Money Zero() => new Money(0, "USD");
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

// Domain Events
public class OrderCreatedEvent : DomainEventBase
{
    public Order Order { get; }
    public OrderCreatedEvent(Order order) => Order = order;
}

public class OrderSubmittedEvent : DomainEventBase  
{
    public Order Order { get; }
    public OrderSubmittedEvent(Order order) => Order = order;
}

// Event Handlers
public class OrderSubmittedEventHandler : INotificationHandler<OrderSubmittedEvent>
{
    private readonly IEmailService _emailService;
    
    public async Task Handle(OrderSubmittedEvent notification, CancellationToken ct)
    {
        await _emailService.SendOrderConfirmationAsync(
            notification.Order.Id,
            notification.Order.CustomerId,
            ct);
    }
}
```

See SKILL.md for complete documentation.
