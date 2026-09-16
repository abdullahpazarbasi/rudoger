import { QueryClient } from "@tanstack/react-query";
import { ClientError } from "../../api/errors/client-error";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry(failureCount, error) {
        if (
          error instanceof ClientError &&
          error.details.status !== null &&
          error.details.status < 500
        ) {
          return false;
        }
        return failureCount < 1;
      },
      staleTime: 15_000,
      refetchOnWindowFocus: true,
    },
    mutations: {
      retry: false,
    },
  },
});
