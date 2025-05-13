# 🧪 Ejercicio 02 — Reconstrucción del estado inmutable desde eventos

## 🎯 Objetivo

Aplicar el patrón de Event Sourcing para reconstruir el estado actual de un carrito de compras a partir de una secuencia de eventos, utilizando un enfoque completamente inmutable y funcional en C#.

---

## 🔁 Flujo de reconstrucción

1. El cliente abre el carrito.
2. Agrega productos con una cantidad determinada.
3. Puede remover productos previamente agregados.
4. Confirma el carrito, iniciando el proceso de pedido.
5. También puede cancelarlo, descartando todos los productos.
6. El estado final del carrito refleja únicamente lo que los eventos han producido.

---

## 🧩 Soluciones exploradas

### 🔷 Versión 1 — **Entidad inmutable con `foreach` + `switch` externo**

**Características:**
- La entidad `ShoppingCart` es un `record` puro sin métodos.
- La lógica para aplicar eventos vive fuera, en `GetShoppingCart(...)`.
- Se usa un `switch` sobre el tipo de evento para construir el nuevo estado con `with`.

**Ventajas:**
- Muy funcional, fácil de seguir.
- Útil para sistemas donde la lógica de negocio se mantiene fuera del modelo.

**Limitaciones:**
- Modelo anémico: la entidad no tiene comportamiento.
- Se pierde encapsulamiento del dominio.

---

### 🔷 Versión 2 — **Entidad inmutable con métodos de evolución internos**

**Características:**
- La lógica de evolución (`AddProductItem`, `Confirm`, `Canceled`, etc.) vive dentro del `record`.
- El método `Evolve` aplica cada evento usando `switch` expression.
- Usa `Aggregate(...)` para aplicar los eventos secuencialmente.

**Ventajas:**
- Encapsula comportamiento en la entidad (enfoque DDD).
- Código más expresivo, testable y alineado con el dominio.

**Limitaciones:**
- Requiere más código, pero mejora la mantenibilidad.

---

## 🧠 Técnicas y patrones aplicados

### 🔹 Uso de `record` posicionales
- Reducen ruido sintáctico.
- Facilitan igualdad por valor y desestructuración.

### 🔹 Transformación inmutable con `with` + LINQ
- Las colecciones son tratadas como inmutables.
- Se combinan `Concat`, `GroupBy`, `Select`, `ToArray` para componer nuevo estado.

### 🔹 `switch` expression por tipo de evento
- Permite manejar todos los casos explícitamente.
- Mejora la seguridad del modelo y la legibilidad.

### 🔹 Función `Empty()` para estado inicial
- Representa claramente el punto de partida del fold.
- Mejora la intención del código.

### 🔹 `Aggregate(events, initial, evolve)`
- Patrón funcional de reducción para aplicar eventos.

---

## 🧾 Conclusiones generales del ejercicio

| Concepto | Aprendizaje clave |
|----------|-------------------|
| **Event Sourcing inmutable** | El estado se reconstruye aplicando eventos sin modificar el original. |
| **Diseño funcional** | La transformación de estado se hace a través de funciones puras. |
| **Encapsulamiento del dominio** | Es preferible que la lógica de negocio esté dentro del modelo. |
| **Uso de LINQ inmutable** | Permite construir nuevas colecciones sin mutar las anteriores. |
| **Patrones DDD + funcionales** | Se pueden combinar para lograr claridad, inmutabilidad y expresividad. |