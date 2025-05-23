namespace IntroductionToEventSourcing.BusinessLogic.Mutable;

using static ShoppingCartEvent;

// EVENTS
public abstract record ShoppingCartEvent
{
    public record ShoppingCartOpened(
        Guid ShoppingCartId,
        Guid ClientId
    ): ShoppingCartEvent;

    public record ProductItemAddedToShoppingCart(
        Guid ShoppingCartId,
        PricedProductItem ProductItem
    ): ShoppingCartEvent;

    public record ProductItemRemovedFromShoppingCart(
        Guid ShoppingCartId,
        PricedProductItem ProductItem
    ): ShoppingCartEvent;

    public record ShoppingCartConfirmed(
        Guid ShoppingCartId,
        DateTime ConfirmedAt
    ): ShoppingCartEvent;

    public record ShoppingCartCanceled(
        Guid ShoppingCartId,
        DateTime CanceledAt
    ): ShoppingCartEvent;

    // This won't allow external inheritance
    private ShoppingCartEvent() { }
}

// VALUE OBJECTS
public class PricedProductItem
{
    public Guid ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}

public class ProductItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

// ENTITY
public class ShoppingCart
{
    private ShoppingCart(Guid cartId, Guid clientId)
    {
        Id = cartId;
        ClientId = clientId;
        UncommittedEvents = UncommittedEvents.Append(new ShoppingCartOpened(cartId, clientId)).ToArray();
    }

    private ShoppingCart()
    {
    }

    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public ShoppingCartStatus Status { get; private set; }
    public IList<PricedProductItem> ProductItems { get; } = new List<PricedProductItem>();
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CanceledAt { get; private set; }

    private ShoppingCartEvent[] UncommittedEvents  = [];

    public void Evolve(object @event)
    {
        switch (@event)
        {
            case ShoppingCartOpened opened:
                Apply(opened);
                break;
            case ProductItemAddedToShoppingCart productItemAdded:
                Apply(productItemAdded);
                break;
            case ProductItemRemovedFromShoppingCart productItemRemoved:
                Apply(productItemRemoved);
                break;
            case ShoppingCartConfirmed confirmed:
                Apply(confirmed);
                break;
            case ShoppingCartCanceled canceled:
                Apply(canceled);
                break;
        }
    }

    public static ShoppingCart Initial() => new();

    public static ShoppingCart Open(
        Guid cartId,
        Guid clientId)
    =>
        new(cartId, clientId);

    private void Apply(ShoppingCartOpened opened)
    {
        Id = opened.ShoppingCartId;
        ClientId = opened.ClientId;
        Status = ShoppingCartStatus.Pending;
    }

    public void AddProduct(
        IProductPriceCalculator priceCalculator,
        ProductItem productItem
    )
    {
        if(ShoppingCartStatus.Closed.HasFlag(Status))
            throw new InvalidOperationException(
                $"Adding product item for cart in '{Status}' status is not allowed.");

        if(productItem.Quantity <= 0)
            throw new InvalidOperationException(
                $"Adding product item with quantity '{productItem.Quantity}' is not allowed.");

        var pricedProductItem = priceCalculator.Calculate(productItem);

        var @event = new ProductItemAddedToShoppingCart(
            Id,
            pricedProductItem
        );
        UncommittedEvents = [@event];
    }

    private void Apply(ProductItemAddedToShoppingCart productItemAdded)
    {
        var (_, pricedProductItem) = productItemAdded;
        var productId = pricedProductItem.ProductId;
        var quantityToAdd = pricedProductItem.Quantity;

        var current = ProductItems.SingleOrDefault(
            pi => pi.ProductId == productId
        );

        if (current == null)
            ProductItems.Add(pricedProductItem);
        else
            current.Quantity += quantityToAdd;
    }

    public void RemoveProduct(PricedProductItem productItemToBeRemoved)
    {
        if (ShoppingCartStatus.Closed.HasFlag(Status))
            throw new InvalidOperationException(
                $"Removing product item for cart in '{Status}' status is not allowed.");

        var currentQuntity = ProductItems.Where(pi => pi.ProductId == productItemToBeRemoved.ProductId).Select(pi => pi.Quantity).FirstOrDefault();

        if (currentQuntity == 0)
            throw new InvalidOperationException(
                "Not enough product items to remove");


        var @event = new ProductItemRemovedFromShoppingCart(
            Id,
            productItemToBeRemoved
        );
        UncommittedEvents = [@event];
    }
    private void Apply(ProductItemRemovedFromShoppingCart productItemRemoved)
    {
        var (_, pricedProductItem) = productItemRemoved;
        var productId = pricedProductItem.ProductId;
        var quantityToRemove = pricedProductItem.Quantity;

        var current = ProductItems.Single(
            pi => pi.ProductId == productId
        );

        if (current.Quantity == quantityToRemove)
            ProductItems.Remove(current);
        else
            current.Quantity -= quantityToRemove;
    }

    public void Confirm()
    {
        if (ShoppingCartStatus.Closed.HasFlag(Status) )
            throw new InvalidOperationException(
                $"Confirming cart in '{Status}' status is not allowed.");

        if(ProductItems.Count == 0)
            throw new InvalidOperationException(
                "Cannot confirm empty shopping cart");

        var @event = new ShoppingCartConfirmed(
            Id,
            DateTime.UtcNow
        );

        UncommittedEvents = [@event];
    }

    private void Apply(ShoppingCartConfirmed confirmed)
    {
        Status = ShoppingCartStatus.Confirmed;
        ConfirmedAt = confirmed.ConfirmedAt;
    }

    public void Cancel()
    {
        if (ShoppingCartStatus.Closed.HasFlag(Status))
            throw new InvalidOperationException(
                $"Canceling cart in '{Status}' status is not allowed.");

        var @event = new ShoppingCartCanceled(
            Id,
            DateTime.UtcNow
        );

        UncommittedEvents = [@event];
    }

    public ShoppingCartEvent[] GetUncommittedEvents()
    {
        var eventsToCommit = UncommittedEvents.ToArray();
        UncommittedEvents = [];
        return eventsToCommit;
    }

    private void Apply(ShoppingCartCanceled canceled)
    {
        Status = ShoppingCartStatus.Canceled;
        CanceledAt = canceled.CanceledAt;
    }
}

public enum ShoppingCartStatus
{
    Pending = 1,
    Confirmed = 2,
    Canceled = 4,
    Closed = Confirmed | Canceled
}
