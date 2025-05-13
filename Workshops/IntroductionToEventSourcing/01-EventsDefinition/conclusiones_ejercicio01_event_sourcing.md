# 🧪 Ejercicio 01 — Modelado de Eventos en un Carrito de Compras

## 🎯 Objetivo

Modelar los **eventos del dominio** para un carrito de compras utilizando Event Sourcing. El objetivo es capturar el comportamiento del negocio como una secuencia de hechos que pueden reconstruir el estado actual de la entidad `Carrito`.

---

## 🛒 Flujo de negocio

1. El cliente abre el carrito.
2. Agrega productos con una cantidad determinada.
3. Puede remover productos previamente agregados.
4. Confirma el carrito, iniciando el proceso de pedido.
5. También puede cancelarlo, descartando todos los productos.
6. Una vez confirmado o cancelado, no se permiten más cambios.

---

## 🧩 Soluciones propuestas

### 🔷 Versión 1 — **Modelado directo de eventos**

**Características:**
- Eventos definidos como `record`s independientes (`ShoppingCartOpened`, `ProductItemAddedToShoppingCart`, etc.).
- Se modela el carrito como entidad mutable (`ShoppingCart`) y su versión inmutable (`ImmutableShoppingCart`).
- Se utiliza un `enum` llamado `ShoppingCartStatus` para representar el estado (`Pending`, `Confirmed`, `Canceled`).

**Ventajas:**
- Fácil de entender y mantener.
- Compatible con patrones de diseño tradicionales.
- Útil en fases iniciales de modelado o cuando el equipo aún no domina técnicas avanzadas.

**Limitaciones:**
- Los eventos están sueltos, lo que puede dificultar la validación exhaustiva con `switch`.
- No hay agrupación ni encapsulamiento que indique que todos esos eventos pertenecen a un mismo agregado.

---

### 🔷 Versión 2 — **Modelado en español con validaciones**

**Características:**
- Eventos y entidad escritos en español (`CarritoDeComprasAbierto`, `ProductoEnCarrito`).
- `ProductoEnCarrito` incluye validaciones internas (precio no negativo, cantidad mayor que cero).
- Se usan propiedades booleanas (`Confirmado`, `Cancelado`) para reflejar el estado.

**Ventajas:**
- Modelado más expresivo y alineado con el negocio.
- Las reglas de validación están cerca de donde ocurren los datos.
- Ideal para equipos que buscan aplicar DDD en su lenguaje natural.

**Limitaciones:**
- El uso de booleanos puede generar estados ambiguos (¿qué pasa si ambos son `false`?).
- Puede necesitar refinamiento para escalar en complejidad o expresividad del dominio.

---

### 🔷 Versión 3 — **Uso de Union Types en C# (modelo avanzado)**

**Características clave:**
- Todos los eventos heredan de una clase base abstracta `ShoppingCartEvent`.
- Cada tipo de evento (abrir, agregar, remover, confirmar, cancelar) es una subclase `record` inmutable.
- El constructor base es `private`, lo que **impide la herencia externa** y garantiza un conjunto cerrado de eventos.

```csharp
public abstract record ShoppingCartEvent
{
    public record ShoppingCartOpened(...) : ShoppingCartEvent;
    public record ProductItemAddedToShoppingCart(...) : ShoppingCartEvent;
    ...
    private ShoppingCartEvent() {}
}
```

### 🧠 ¿Qué es un Union Type?

Un **Union Type** (tipo unión cerrada) representa un valor que puede ser uno de varios tipos definidos. En lenguajes como F#, Rust o Kotlin, esto se conoce como *discriminated union*. C# no lo tiene nativamente, pero puede simularse así:

- Se crea una clase base `abstract` o `record abstract`.
- Se definen subtipos como `record`s que heredan de ella.
- Se hace el constructor `private` para impedir que alguien cree tipos externos no controlados.

### 🧠 ¿Por qué usar Union Types aquí?

| Beneficio | Descripción |
|----------|-------------|
| ✅ **Seguridad en tiempo de compilación** | Puedes usar `switch` o `pattern matching` sobre `ShoppingCartEvent`, y el compilador te avisa si olvidas un caso. |
| ✅ **Organización** | Todos los eventos relacionados con `ShoppingCart` están contenidos en un solo bloque, lo que mejora la claridad y mantenibilidad. |
| ✅ **Encapsulamiento** | No se pueden definir nuevos tipos de eventos fuera del dominio. Esto mantiene el modelo consistente. |
| ✅ **Ideal para Event Sourcing** | Representa claramente el conjunto de eventos que pueden reconstruir el estado de un agregado.

### Ejemplo de uso con `switch`:

```csharp
void ApplyEvent(ShoppingCartEvent evt)
{
    switch (evt)
    {
        case ShoppingCartOpened e:
            // Lógica de apertura
            break;
        case ProductItemAddedToShoppingCart e:
            // Lógica de agregado
            break;
        // ...
        default:
            throw new InvalidOperationException("Evento no reconocido");
    }
}
```

---

## 🧾 Conclusiones generales del ejercicio

| Concepto | Aprendizaje clave |
|----------|-------------------|
| **Eventos** | Representan hechos pasados. Deben ser inmutables y nombrados en pasado. |
| **Value Objects** | Son componentes del dominio que encapsulan lógica y validaciones. |
| **Entidad agregada** | Puede reconstruirse aplicando eventos. Su estado puede ser mutable (para aplicar eventos) o inmutable (para proyecciones). |
| **Union Types** | Mejoran la seguridad y claridad del modelo. Ideal para conjuntos finitos y bien definidos de eventos. |
| **Diseño guiado por el dominio** | Lo importante no es solo el código, sino reflejar claramente la lógica del negocio. |