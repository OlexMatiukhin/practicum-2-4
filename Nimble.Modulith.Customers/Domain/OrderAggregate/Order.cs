using Nimble.Modulith.Customers.Domain.Common;

namespace Nimble.Modulith.Customers.Domain.OrderAggregate;

public class Order : EntityBase
{
    private readonly List<OrderItem> _items = [];

    public int CustomerId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public decimal TotalAmount => _items.Sum(i => i.TotalPrice);

    public void AddItem(OrderItem item)
    {
        _items.Add(item);
    }

    public void RemoveItem(OrderItem item)
    {
        _items.Remove(item);
    }
}
