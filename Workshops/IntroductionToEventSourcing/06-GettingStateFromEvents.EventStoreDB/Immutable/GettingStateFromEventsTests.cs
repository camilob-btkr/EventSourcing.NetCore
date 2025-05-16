using System.Text.Json;
using EventStore.Client;
using FluentAssertions;
using IntroductionToEventSourcing.GettingStateFromEvents.Tools;
using Xunit;

namespace IntroductionToEventSourcing.GettingStateFromEvents.Immutable;

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
public record PricedProductItem(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
);

// ENTITY
public record ShoppingCart(
    Guid Id,
    Guid ClientId,
    ShoppingCartStatus Status,
    PricedProductItem[] ProductItems,
    DateTime? ConfirmedAt = null,
    DateTime? CanceledAt = null
)
{
    public static ShoppingCart Apply(ShoppingCart shoppingCart, object @event)
    {
        return @event switch
        {
            ShoppingCartOpened opened => Open(opened),
            ProductItemAddedToShoppingCart productItemAdded => AddProductItem(productItemAdded, shoppingCart),
            ProductItemRemovedFromShoppingCart productItemRemoved =>
                RemoveProductItem(productItemRemoved, shoppingCart),
            ShoppingCartConfirmed confirmed => Confirm(confirmed, shoppingCart),
            ShoppingCartCanceled canceled => Canceled(canceled, shoppingCart),
            _ => shoppingCart
        };
    }

    private static ShoppingCart Canceled(ShoppingCartCanceled canceled, ShoppingCart shoppingCart)
    {
        return shoppingCart with { CanceledAt = canceled.CanceledAt, Status = ShoppingCartStatus.Canceled };
    }

    private static ShoppingCart Confirm(ShoppingCartConfirmed confirmed, ShoppingCart shoppingCart)
    {
        return shoppingCart with { Status = ShoppingCartStatus.Confirmed, ConfirmedAt = confirmed.ConfirmedAt };
    }

    private static ShoppingCart RemoveProductItem(ProductItemRemovedFromShoppingCart productItemRemoved,
        ShoppingCart shoppingCart)
    {
        var currentProductItems = shoppingCart.ProductItems.ToList();
        var product = currentProductItems.Single(p => p.ProductId == productItemRemoved.ProductItem.ProductId);
        var index = currentProductItems.FindIndex(p => p.ProductId == product.ProductId);

        var quantity = product.Quantity - productItemRemoved.ProductItem.Quantity;


        if (quantity > 0)
            currentProductItems[index] = product with { Quantity = quantity };
        else
            currentProductItems.Remove(product);

        return shoppingCart with { ProductItems = currentProductItems.ToArray() };
    }

    private static ShoppingCart Open(ShoppingCartOpened opened)
    {
        return new ShoppingCart(opened.ShoppingCartId,
            opened.ClientId,
            ShoppingCartStatus.Pending,
            []);
    }

    private static ShoppingCart AddProductItem(ProductItemAddedToShoppingCart productItemAdded,
        ShoppingCart shoppingCart)
    {
        var productItemsUpdated = shoppingCart.ProductItems.Concat(new[] { productItemAdded.ProductItem })
            .GroupBy(p => p.ProductId).Select(group =>
                group.Count() == 1
                    ? group.First()
                    : new PricedProductItem(
                        group.Key,
                        group.Sum(p => p.Quantity),
                        group.First().UnitPrice)).ToArray();
        return shoppingCart with { ProductItems = productItemsUpdated };
    }

    public static ShoppingCart Empty()
    {
        return new ShoppingCart(Guid.Empty, Guid.Empty, ShoppingCartStatus.Pending, []);
    }
}

public enum ShoppingCartStatus
{
    Pending = 1,
    Confirmed = 2,
    Canceled = 4
}

public class GettingStateFromEventsTests: EventStoreDBTest
{
    /// <summary>
    /// Solution - Mutable entity with When method
    /// </summary>
    /// <returns></returns>
    private static async Task<ShoppingCart> GetShoppingCart(EventStoreClient eventStore, string streamName,
        CancellationToken ct)
    {
        var events =
            eventStore.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start, cancellationToken: ct);
        return await events
            .Select(@event =>
                JsonSerializer.Deserialize(@event.Event.Data.Span,Type.GetType(@event.Event.EventType, true)!))
            .AggregateAsync(ShoppingCart.Empty(), ShoppingCart.Apply!, ct);
    }

    [Fact]
    [Trait("Category", "SkipCI")]
    public async Task GettingState_FromEventStoreDB_ShouldSucceed()
    {
        var shoppingCartId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var shoesId = Guid.NewGuid();
        var tShirtId = Guid.NewGuid();
        var twoPairsOfShoes = new PricedProductItem(shoesId, 2, 100);
        var pairOfShoes = new PricedProductItem(shoesId, 1, 100);
        var tShirt = new PricedProductItem(tShirtId, 1, 50);

        var events = new ShoppingCartEvent[]
        {
            new ShoppingCartOpened(shoppingCartId, clientId),
            new ProductItemAddedToShoppingCart(shoppingCartId, twoPairsOfShoes),
            new ProductItemAddedToShoppingCart(shoppingCartId, tShirt),
            new ProductItemRemovedFromShoppingCart(shoppingCartId, pairOfShoes),
            new ShoppingCartConfirmed(shoppingCartId, DateTime.UtcNow),
            new ShoppingCartCanceled(shoppingCartId, DateTime.UtcNow)
        };

        var streamName = $"shopping_cart-{shoppingCartId}";

        await AppendEvents(streamName, events, CancellationToken.None);

        var shoppingCart = await GetShoppingCart(EventStore, streamName, CancellationToken.None);

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
