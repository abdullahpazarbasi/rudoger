import { QueryClientProvider } from "@tanstack/react-query";
import { useEffect, type ReactNode } from "react";
import { Toaster } from "sonner";
import { startAuthLifecycle } from "../../features/authn/auth-store";
import { startThemeLifecycle } from "../../shared/theme/theme-store";
import { HealthGate } from "./health-gate";
import { queryClient } from "./query-client";

export function AppProviders({ children }: { children: ReactNode }) {
  useEffect(() => {
    const stopAuth = startAuthLifecycle();
    const stopTheme = startThemeLifecycle();
    return () => {
      stopAuth();
      stopTheme();
    };
  }, []);

  return (
    <QueryClientProvider client={queryClient}>
      <HealthGate>{children}</HealthGate>
      <Toaster closeButton={false} position="bottom-right" richColors />
    </QueryClientProvider>
  );
}
