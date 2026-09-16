import createClient, { type Client } from "openapi-fetch";
import { queryClient } from "../../app/providers/query-client";
import { authStore } from "../../features/authn/auth-store";
import type { paths } from "../generated/schema";
import { queryKeys } from "../query-keys";

export const apiBaseUrl: string =
  typeof import.meta.env.VITE_API_BASE_URL === "string"
    ? import.meta.env.VITE_API_BASE_URL
    : window.location.origin;

export const apiClient: Client<paths> = createClient<paths>({
  baseUrl: apiBaseUrl,
  fetch: (request) => fetch(request),
});

apiClient.use({
  onRequest({ request }) {
    request.headers.set("X-Correlation-Id", crypto.randomUUID());
    const token = authStore.getAccessToken();
    if (token !== null) {
      request.headers.set("Authorization", `Bearer ${token}`);
    }
    return request;
  },
  onResponse({ request, response }) {
    if (response.status === 401 && !request.url.endsWith("/api/v1/authn/tokens")) {
      authStore.markExpired();
    }
    return response;
  },
  onError({ error }) {
    void queryClient.invalidateQueries({ queryKey: queryKeys.health });
    return error instanceof Error ? error : new Error("The API request failed.", { cause: error });
  },
});
