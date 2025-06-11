# 🧪 Ejercicio 13 — Proyecciones con Idempotencia

## 🎯 Objetivo

Implementar proyecciones en un sistema basado en eventos (Event Sourcing), asegurando que **cada evento se aplique una sola vez**, incluso si llega varias veces. Esto es clave en sistemas distribuidos donde pueden producirse **reintentos o duplicados**.

---

## 🧠 ¿Qué es la idempotencia en este contexto?

La idempotencia garantiza que, **si un mismo evento se procesa varias veces**, **solo afecte el estado una vez**. Es decir, aplicar el mismo evento dos veces no debe cambiar nada después de la primera vez.

---

## 🧩 Estrategia utilizada

Se usó una combinación de:

1. Un campo `Version` en cada proyección (`IVersioned`).
2. El uso del campo `StreamPosition` del evento.
3. Una función auxiliar llamada `GetAndStore(...)` que verifica si el evento ya fue aplicado.

### 📦 Interfaz común para las proyecciones

```csharp
public interface IVersioned
{
    ulong Version { get; set; }
}
```

Esto permite a todas las proyecciones guardar **hasta qué evento han sido actualizadas**.

---

### 🔄 ¿Cómo funciona `GetAndStore(...)`?

```csharp
public static void GetAndStore<T>(this Database database, Guid id, ulong version, Func<T, T> update)
    where T : class, IVersioned, new()
{
    var item = database.Get<T>(id);

    if (item == null)
        throw new Exception($"Item with id: '{id}' and expected version: {version} not found!");

    if (item.Version >= version) return; // ← Aquí se evita aplicar dos veces el mismo evento

    database.Store(id, version, update(item));
}
```

### 🧪 ¿Qué hace paso a paso?

1. **Busca la entidad** en la base de datos.
2. **Compara su versión actual** con la del evento:
   - Si ya aplicamos ese evento o uno posterior → lo ignoramos.
3. Si es un evento nuevo:
   - Aplica los cambios con `update(...)`.
   - **Actualiza la versión** al número del evento (`StreamPosition`).

---

### 📨 ¿De dónde viene el número del evento?

Cada evento se publica con una metadata que incluye su posición única en el stream:

```csharp
public record EventMetadata(Guid StreamId, ulong StreamPosition);
```

Este número se usa como base para comparar:

```csharp
if (item.Version >= @event.Metadata.StreamPosition)
    return; // Evento duplicado o fuera de orden
```

---

## 📌 Ejemplo en una proyección real

```csharp
database.GetAndStore<ShoppingCartDetails>(
    @event.Data.ShoppingCartId,
    @event.Metadata.StreamPosition,
    item =>
    {
        item.ProductItems.Add(...);
        item.TotalAmount += ...;
        item.TotalItemsCount += ...;
        return item;
    });
```

🔁 Si este evento llega 3 veces, solo la primera vez se aplica. Las otras se ignoran automáticamente.

---

## ✅ ¿Qué problemas evita esta estrategia?

- Eventos duplicados (por reintentos)
- Eventos que llegan en desorden
- Errores como precios duplicados, cantidades sumadas varias veces, cambios de estado incorrectos

---

## 📘 Conclusión

La solución es **simple pero poderosa**: guardar la última versión aplicada y comparar.  
Esto permite que la lógica de las proyecciones sea **segura, repetible y confiable**, incluso en entornos reales donde los eventos pueden llegar varias veces o fuera de orden.