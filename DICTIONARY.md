# Ubiquitous Language

This dictionary is the canonical vocabulary shared by code, API contracts, tests, and documentation.

## Shared Terms

### Bounded Context (BC)

An autonomous business boundary that owns its model, event store or operational data, projection, API contracts, and database schema. Rudoger has Authn, Product, Inventory, Order, and Logging bounded contexts.

### Internal API

An in-process request from a consumer bounded context's Infrastructure layer to a provider bounded context's Presentation contract. It does not use HTTP, but it is correlated and audited like an external request.

### Correlation ID

The operation-wide identifier carried by `X-Correlation-Id`, event metadata, stock movements, internal calls, logs, and problem responses.

### Idempotency Key

A client-provided identifier for a retryable command. The same key and semantic input return the original result; the same key with different input is a conflict.

### Source Event ID

The stable identifier of the command/event that caused a stock movement. Inventory uses it to make internal workflow retries idempotent.

### Event Envelope

The generic stored representation of a domain event: EventId, StreamId, AggregateType, Version, EventType, SchemaVersion, Payload, Metadata, and OccurredAtUtc.

### Projection

The query-oriented relational state derived from events in the same local transaction as the append.

### Outbox Message

A durable local workflow instruction claimed and retried by a background worker after its originating transaction commits.

## Authn Terms

### User

An authenticated identity with a unique normalized username and a one-way password hash. Its GUID v7 becomes the JWT `sub` claim.

### Token Exchange

The username/password login operation that returns a short-lived JWT. Refresh tokens are outside the product boundary.

## Product Terms

### Product

The sellable item identified by SKU. It owns a name, immutable base UoM, base price/currency, and packaging options.

### Product Packaging

A product-owned representation with a level, UoM code, base-UoM conversion factor, optional barcode, and optional physical measurements.

### Level-zero Packaging

The mandatory packaging created atomically with Product. It uses the Product base UoM and a conversion factor of exactly one. Its level, UoM, and factor are immutable.

### Conversion Factor

The number of base-UoM units represented by one unit of a packaging. It is used for price snapshots and inventory reservations.

### Usage Claim

A short-lived, idempotent Product guard held by an Inventory or Order operation while it validates or snapshots Product data.

### Logical Deletion

The terminal Product state hidden from normal reads while retaining its event history and projections. A Product with stock or any Order reference cannot be deleted.

## Inventory Terms

### Stock Item

The single-warehouse balance for one Product in its base UoM.

### On-hand Quantity

Physical quantity currently held.

### Reserved Quantity

On-hand quantity assigned to placed Orders but not yet shipped or cancelled.

### Available Quantity

Calculated as `OnHandQuantity - ReservedQuantity`.

### Stock Movement

The immutable inventory ledger entry caused by a receipt, adjustment, deduction, reservation, commit, or release.

### Receipt

Increases on-hand quantity.

### Adjustment

Increases or decreases on-hand quantity while preserving `on-hand >= reserved >= 0`.

### Deduction

Decreases unreserved on-hand quantity independently of an Order.

### Reservation

Increases reserved quantity without changing on-hand quantity when an Order is placed.

### Commit

Decreases both on-hand and reserved quantity when an Order ships.

### Release

Decreases reserved quantity without changing on-hand quantity when an Order is cancelled or placement is compensated.

## Order Terms

### Order Placement

The asynchronous process resource created before an Order exists. It is Pending, Succeeded, or Failed and owns stable reservation/compensation identifiers.

### Order

The successful commercial result of a placement. It belongs to the JWT subject and contains immutable line price snapshots.

### Order Line

A Product/UoM quantity with server-calculated unit price, currency, and technical base quantity.

### Placed

The initial Order status. Payment is assumed to have been collected and stock remains reserved.

### Shipped

The terminal status reached after every reserved Product quantity is committed.

### Cancelled

The terminal status reached after every reserved Product quantity is released.

### Order Transition

The asynchronous process that moves a Placed Order to Shipped or Cancelled.

## Logging Terms

### Request Log

The mandatory operational record of an HTTP request or internal API call, including correlation, timing, outcome, and masked request data.

### Sensitive Data Masking

Replacement of password, authorization, cookie, JWT, and token values before audit persistence.
