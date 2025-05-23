# 🧪 Ejercicio 07 — Comparativa de enfoques en lógica de negocio con Event Sourcing

## 🎯 Objetivo

Implementar y evaluar distintas formas de modelar la lógica de negocio de un carrito de compras usando Event Sourcing. El enfoque debe producir eventos como resultado de operaciones de negocio, capturando los cambios de estado de forma explícita.

---

## 🧩 Versiones analizadas

### 🔷 Camilo - Versión inmutable (funcional)

**Características:**
- El `ShoppingCart` es un `record` inmutable.
- Cada comando genera un evento desde un `ShoppingCartService` externo.
- La evolución del estado (`Evolve`) aplica los eventos usando `switch expression` y `with { ... }`.

**Ventajas:**
- Patrón funcional puro (`fold`).
- Fácil de testear y mantener.
- Eventos y estado completamente separados.

**Cuándo usarla:**
- Ideal para sistemas altamente trazables.
- Cuando se busca claridad, inmutabilidad y separación de efectos.

---

### 🔷 Camilo - Versión mutable (OO + Event Sourcing)

**Características:**
- `ShoppingCart` es un `class` con estado interno mutable.
- Usa `UncommittedEvents` y métodos `AddProduct(...)`, `Confirm()`, etc.
- La evolución aplica los eventos mediante `Apply(...)`.

**Ventajas:**
- Encapsula reglas de negocio dentro del modelo (DDD táctico).
- Muy expresivo para equipos acostumbrados a OOP.
- Control centralizado del estado y eventos.

**Cuándo usarla:**
- Cuando se desea un dominio rico y encapsulado.
- Útil en entornos donde se prioriza comportamiento sobre pureza.

---

### 🔷 Autor - Versión inmutable (anémica + funcional)

**Características:**
- `ShoppingCart` es un `record` inmutable sin comportamiento.
- Toda la lógica vive en `ShoppingCartService` (modelo anémico).
- Retorna eventos desde el servicio y usa `Evolve(...)` para aplicar.

**Ventajas:**
- Muy funcional y explícito.
- Bajo acoplamiento, buen para CQRS.

**Cuándo usarla:**
- En contextos CQRS/ES donde dominio e infraestructura están separados.
- Ideal para equipos funcionales o FP-first.

---

### 🔷 Autor - Versión mutable (agregado clásico)

**Características:**
- Agregado extiende `Aggregate<T>`, encapsula estado y comportamiento.
- Genera eventos y los aplica internamente (`Enqueue`, `Apply`).

**Ventajas:**
- Modelo rico, alineado con DDD.
- Flujo claro: comando → evento → evolución.

**Cuándo usarla:**
- En aplicaciones complejas orientadas a dominio.
- Cuando se quiere centralizar reglas y estado en la misma clase.

---

### 🔷 Autor - Versión mixta (agregado + eventos explícitos)

**Características:**
- Clase `ShoppingCart` mutable, pero cada método retorna el evento generado.
- Estado se muta con `Apply(...)`, pero el evento se expone.

**Ventajas:**
- Claridad del dominio + trazabilidad de efectos.
- Facilita logging, testing y persistencia inmediata.

**Cuándo usarla:**
- Cuando se desea mantener el control de reglas y estado dentro del agregado.
- Y al mismo tiempo se busca claridad en qué eventos fueron causados.

---

## 🧾 Conclusiones comparativas

| Enfoque | Beneficios clave | Cuándo elegirlo |
|--------|------------------|------------------|
| 🧠 Funcional puro (Camilo inmutable) | Simplicidad, testabilidad, separación de efectos | Sistemas CQRS, pipelines funcionales |
| 🧱 OO clásico (Camilo mutable) | Comportamiento rico, encapsulamiento | DDD táctico, sistemas OO tradicionales |
| 📦 Inmutable anémico (Autor) | Claridad funcional, sin lógica en entidades | Infraestructuras funcionales, CQRS |
| 🔄 Mutable con agregados (Autor) | DDD alineado, evolución interna de eventos | Dominios complejos con mutabilidad controlada |
| 🌀 Mixto (Autor) | Rico en reglas + retorno de eventos | Balance entre claridad de dominio y trazabilidad |

---

## 📘 Recomendación final

Cada enfoque responde a una necesidad distinta:

- Si buscas claridad, pruebas fáciles y separación estricta: **funcional puro**.
- Si prefieres encapsular todo en un modelo que “hable el lenguaje del negocio”: **mutabilidad controlada con eventos internos**.
- Si deseas lo mejor de ambos mundos: **modelo mixto con retorno explícito de eventos**.