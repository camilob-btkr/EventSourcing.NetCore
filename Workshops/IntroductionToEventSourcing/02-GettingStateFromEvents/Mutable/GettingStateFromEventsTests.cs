using System.Drawing.Printing;
using FluentAssertions;
using Xunit;

namespace IntroductionToEventSourcing.GettingStateFromEvents.Mutable;

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

// ENTITY
public class ShoppingCart
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public ShoppingCartStatus Status { get; set; }
    public IList<PricedProductItem> ProductItems { get; set; } = new List<PricedProductItem>();
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CanceledAt { get; set; }

    public void Apply(ShoppingCartEvent @event)
    {
        switch (@event)
        {
            case ShoppingCartOpened opened:
                Open(opened);
                break;
            case ProductItemAddedToShoppingCart productItemAdded:
                AddProductItem(productItemAdded);
                break;
            case ProductItemRemovedFromShoppingCart productItemRemoved:
                RemoveProductItem(productItemRemoved);
                break;
            case ShoppingCartConfirmed confirmed:
                Confirm(confirmed);
                break;
            case ShoppingCartCanceled canceled:
                Cancel(canceled);
                break;
        }
    }

    private void Cancel(ShoppingCartCanceled canceled)
    {
        CanceledAt = canceled.CanceledAt;
        Status = ShoppingCartStatus.Canceled;
    }

    private void Confirm(ShoppingCartConfirmed confirmed)
    {
        ConfirmedAt = confirmed.ConfirmedAt;
        Status = ShoppingCartStatus.Confirmed;
    }

    private void RemoveProductItem(ProductItemRemovedFromShoppingCart productItemRemoved)
    {
        var product = ProductItems.First(p => p.ProductId == productItemRemoved.ProductItem.ProductId);

        product.Quantity -= productItemRemoved.ProductItem.Quantity;

        if (product.Quantity <= 0)
            ProductItems.Remove(product);
    }

    private void AddProductItem(ProductItemAddedToShoppingCart productItemAdded)
    {
        var product = ProductItems.FirstOrDefault(p => p.ProductId == productItemAdded.ProductItem.ProductId);


        if (product == null)
            ProductItems.Add(productItemAdded.ProductItem);
        else
        {
            product.Quantity += productItemAdded.ProductItem.Quantity;
        }
    }

    private void Open(ShoppingCartOpened opened)
    {
        Id = opened.ShoppingCartId;
        ClientId = opened.ClientId;
        Status = ShoppingCartStatus.Pending;
    }
}

public enum ShoppingCartStatus
{
    Pending = 1,
    Confirmed = 2,
    Canceled = 4
}

public class GettingStateFromEventsTests
{
    // 1. Add logic here
    private static ShoppingCart GetShoppingCart(IEnumerable<ShoppingCartEvent> events)
    {
        var shoppingCart = new ShoppingCart();

        foreach (var @event in events)
        {
            shoppingCart.Apply(@event);
        }

        return shoppingCart;
    }

    [Fact]
    [Trait("Category", "SkipCI")]
    public void GettingState_ForSequenceOfEvents_ShouldSucceed()
    {
        var shoppingCartId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var shoesId = Guid.NewGuid();
        var tShirtId = Guid.NewGuid();
        var twoPairsOfShoes =
            new PricedProductItem { ProductId = shoesId, Quantity = 2, UnitPrice = 100 };
        var pairOfShoes =
            new PricedProductItem { ProductId = shoesId, Quantity = 1, UnitPrice = 100 };
        var tShirt =
            new PricedProductItem { ProductId = tShirtId, Quantity = 1, UnitPrice = 50 };

        var events = new ShoppingCartEvent[]
        {
            new ShoppingCartOpened(shoppingCartId, clientId),
            new ProductItemAddedToShoppingCart(shoppingCartId, twoPairsOfShoes),
            new ProductItemAddedToShoppingCart(shoppingCartId, tShirt),
            new ProductItemRemovedFromShoppingCart(shoppingCartId, pairOfShoes),
            new ShoppingCartConfirmed(shoppingCartId, DateTime.UtcNow),
            new ShoppingCartCanceled(shoppingCartId, DateTime.UtcNow)
        };

        var shoppingCart = GetShoppingCart(events);

        shoppingCart.Id.Should().Be(shoppingCartId);
        shoppingCart.ClientId.Should().Be(clientId);
        shoppingCart.ProductItems.Should().HaveCount(2);

        shoppingCart.ProductItems[0].ProductId.Should().Be(shoesId);
        shoppingCart.ProductItems[0].Quantity.Should().Be(pairOfShoes.Quantity);
        shoppingCart.ProductItems[0].UnitPrice.Should().Be(pairOfShoes.UnitPrice);

        shoppingCart.ProductItems[1].ProductId.Should().Be(tShirtId);
        shoppingCart.ProductItems[1].Quantity.Should().Be(tShirt.Quantity);
        shoppingCart.ProductItems[1].UnitPrice.Should().Be(tShirt.UnitPrice);
    }
}
