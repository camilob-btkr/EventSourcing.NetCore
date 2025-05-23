namespace IntroductionToEventSourcing.BusinessLogic.Immutable;

using static ShoppingCartEvent;
using static ShoppingCartCommand;

public abstract record ShoppingCartCommand
{
    public record OpenShoppingCart(
        Guid ShoppingCartId,
        Guid ClientId
    ): ShoppingCartCommand;

    public record AddProductItemToShoppingCart(
        Guid ShoppingCartId,
        ProductItem ProductItem
    );

    public record RemoveProductItemFromShoppingCart(
        Guid ShoppingCartId,
        PricedProductItem ProductItem
    );

    public record ConfirmShoppingCart(
        Guid ShoppingCartId
    );

    public record CancelShoppingCart(
        Guid ShoppingCartId
    ): ShoppingCartCommand;

    private ShoppingCartCommand() { }
}

public static class ShoppingCartService
{
    public static ShoppingCartOpened Handle(OpenShoppingCart command) => new(command.ShoppingCartId, command.ClientId);

    public static ProductItemAddedToShoppingCart Handle(
        IProductPriceCalculator priceCalculator,
        AddProductItemToShoppingCart command,
        ShoppingCart shoppingCart
    )
    {
        if (ShoppingCartStatus.Closed.HasFlag(shoppingCart.Status))
            throw new InvalidOperationException(
                $"Adding product item for cart in '{shoppingCart.Status}' status is not allowed.");
        if (command.ProductItem.Quantity <= 0)
            throw new InvalidOperationException(
                $"Adding product item with quantity '{command.ProductItem.Quantity}' is not allowed.");

        var pricedProductItem = priceCalculator.Calculate(command.ProductItem);

        return new ProductItemAddedToShoppingCart(command.ShoppingCartId, pricedProductItem);
    }

    public static ProductItemRemovedFromShoppingCart Handle(
        RemoveProductItemFromShoppingCart command,
        ShoppingCart shoppingCart
    )
    {
        if (ShoppingCartStatus.Closed.HasFlag(shoppingCart.Status))
            throw new InvalidOperationException(
                $"Removing product item for cart in '{shoppingCart.Status}' status is not allowed.");

        var currentQuntity = shoppingCart.ProductItems.Where(pi => pi.ProductId == command.ProductItem.ProductId)
            .Select(pi => pi.Quantity).FirstOrDefault();

        if (currentQuntity == 0)
            throw new InvalidOperationException(
                "Not enough product items to remove");

        return new ProductItemRemovedFromShoppingCart(command.ShoppingCartId, command.ProductItem);
    }

    public static ShoppingCartConfirmed Handle(ConfirmShoppingCart command, ShoppingCart shoppingCart)
    {
        if (ShoppingCartStatus.Closed.HasFlag(shoppingCart.Status) )
            throw new InvalidOperationException(
                $"Confirming cart in '{shoppingCart.Status}' status is not allowed.");

        if(shoppingCart.ProductItems.Length == 0)
            throw new InvalidOperationException(
                "Cannot confirm empty shopping cart");

        return new ShoppingCartConfirmed(command.ShoppingCartId, DateTime.UtcNow);

    }

    public static ShoppingCartCanceled Handle(CancelShoppingCart command, ShoppingCart shoppingCart)
    {
        if (ShoppingCartStatus.Closed.HasFlag(shoppingCart.Status))
            throw new InvalidOperationException(
                $"Canceling cart in '{shoppingCart.Status}' status is not allowed.");

        return new ShoppingCartCanceled(command.ShoppingCartId, DateTime.UtcNow);
    }
}
