import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { RouterProvider } from "react-router";
import { AppProviders } from "./app/providers/app-providers";
import { ErrorBoundary } from "./app/error-boundary";
import { router } from "./app/router/router";
import "./index.css";

const root = document.getElementById("root");
if (root === null) {
  throw new Error("Uygulama kök elementi bulunamadı.");
}

createRoot(root).render(
  <StrictMode>
    <ErrorBoundary>
      <AppProviders>
        <RouterProvider router={router} />
      </AppProviders>
    </ErrorBoundary>
  </StrictMode>,
);
