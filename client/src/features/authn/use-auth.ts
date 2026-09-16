import { useSyncExternalStore } from "react";
import { authStore, type AuthState } from "./auth-store";

export function useAuth(): AuthState {
  return useSyncExternalStore(authStore.subscribe, authStore.getSnapshot, authStore.getSnapshot);
}
