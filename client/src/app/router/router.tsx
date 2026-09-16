import { createBrowserRouter } from "react-router";
import { AppLayout } from "../layout/app-layout";
import { NotFoundPage } from "../../shared/components/not-found-page";
import { HomeRoute } from "./home-route";
import { ProtectedRoute } from "./protected-route";

export const router = createBrowserRouter([
  { path: "/", element: <HomeRoute /> },
  {
    path: "/giris",
    lazy: async () => ({
      Component: (await import("../../features/authn/login-page")).LoginPage,
    }),
  },
  {
    element: (
      <ProtectedRoute>
        <AppLayout />
      </ProtectedRoute>
    ),
    children: [
      {
        path: "/urunler",
        lazy: async () => ({
          Component: (await import("../../features/products/products-page")).ProductsPage,
        }),
      },
      {
        path: "/urunler/:productId",
        lazy: async () => ({
          Component: (await import("../../features/products/product-detail-page"))
            .ProductDetailPage,
        }),
      },
      {
        path: "/stok",
        lazy: async () => ({
          Component: (await import("../../features/inventory/inventory-page")).InventoryPage,
        }),
      },
      {
        path: "/stok/:stockItemId",
        lazy: async () => ({
          Component: (await import("../../features/inventory/stock-detail-page")).StockDetailPage,
        }),
      },
      {
        path: "/siparisler",
        lazy: async () => ({
          Component: (await import("../../features/orders/orders-page")).OrdersPage,
        }),
      },
      {
        path: "/siparisler/:orderId",
        lazy: async () => ({
          Component: (await import("../../features/orders/order-detail-page")).OrderDetailPage,
        }),
      },
    ],
  },
  { path: "*", element: <NotFoundPage /> },
]);
