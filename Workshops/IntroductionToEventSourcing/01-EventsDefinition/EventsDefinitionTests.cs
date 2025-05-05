using FluentAssertions;
using Xunit;

namespace IntroductionToEventSourcing.EventsDefinition;

// 1. Define your events and entity here

//Eventos

public record CarritoDeComprasAbierto(
    Guid IdCarrito,
    Guid IdCliente,
    DateTime FechaApertura
);

public record ProductoAgregadoAlCarrito(
    Guid IdCarrito,
    ProductoEnCarrito Producto
);

public record ProductoRemovidoDelCarrito(
    Guid IdCarrito,
    ProductoEnCarrito Producto
);

public record CarritoDeComprasConfirmado(
    Guid IdCarrito,
    DateTime FechaConfirmacion
);

public record CarritoDeComprasCancelado(
    Guid IdCarrito,
    DateTime FechaCancelacion
);

//Velue Objects
public record ProductoEnCarrito
{
    public Guid IdProducto { get; }
    public decimal PrecioUnitario { get; }
    public int Cantidad { get; }

    public ProductoEnCarrito(Guid idProducto, decimal precioUnitario, int cantidad)
    {
        if (precioUnitario < 0)
            throw new ArgumentOutOfRangeException(nameof(precioUnitario), "El precio no puede ser negativo.");
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor a cero.");

        IdProducto = idProducto;
        PrecioUnitario = precioUnitario;
        Cantidad = cantidad;
    }

    public decimal PrecioTotal => PrecioUnitario * Cantidad;
}

//Entidades

public class CarritoDeCompras
{
    public Guid Id { get; set; }
    public Guid IdCliente { get; set; }
    public List<ProductoEnCarrito> Productos { get; set; } = [];
    public bool Confirmado { get; set; }
    public bool Cancelado { get; set; }
}

public class EventsDefinitionTests
{
    [Fact]
    [Trait("Category", "SkipCI")]
    public void AllEventTypes_ShouldBeDefined()
    {
        var idCarrito = Guid.NewGuid();
        var idCliente = Guid.NewGuid();
        var fechaApertura = DateTime.UtcNow;

        var cervezaPoker = new ProductoEnCarrito(Guid.NewGuid(), 10.0m, 2);

        var events = new object[]
        {
            new CarritoDeComprasAbierto(idCarrito, idCliente, fechaApertura),
            new ProductoAgregadoAlCarrito(idCarrito, cervezaPoker),
            new ProductoRemovidoDelCarrito(idCarrito, cervezaPoker),
            new CarritoDeComprasConfirmado(idCarrito, DateTime.UtcNow),
            new CarritoDeComprasCancelado(idCarrito, DateTime.UtcNow)
        };

        const int expectedEventTypesCount = 5;
        events.Should().HaveCount(expectedEventTypesCount);
        events.GroupBy(e => e.GetType()).Should().HaveCount(expectedEventTypesCount);
    }
}
