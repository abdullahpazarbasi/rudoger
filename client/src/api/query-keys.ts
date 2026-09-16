export const queryKeys = {
  health: ["health"] as const,
  products: (filters?: unknown) => ["products", filters ?? {}] as const,
  product: (id: string) => ["product", id] as const,
  stockItems: (filters?: unknown) => ["stock-items", filters ?? {}] as const,
  stockItem: (id: string) => ["stock-item", id] as const,
  stockMovements: (id: string, page: number) => ["stock-movements", id, page] as const,
  orders: (page: number) => ["orders", page] as const,
  order: (id: string) => ["order", id] as const,
  orderPlacement: (id: string) => ["order-placement", id] as const,
  orderTransition: (orderId: string, transitionId: string) =>
    ["order-transition", orderId, transitionId] as const,
};
