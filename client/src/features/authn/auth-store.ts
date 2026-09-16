import type { TokenResult } from "../../api/contracts";

const STORAGE_KEY = "rudoger.session";

export type AuthState =
  | { status: "anonymous" }
  | { status: "authenticated"; session: AuthSession }
  | { status: "expired"; session: AuthSession };

export interface AuthSession {
  accessToken: string;
  tokenType: string;
  expiresAt: number;
  username: string | null;
  userId: string | null;
}

interface JwtClaims {
  exp?: number;
  preferred_username?: string;
  sub?: string;
}

type Listener = () => void;

function decodeClaims(token: string): JwtClaims {
  try {
    const payload = token.split(".")[1];
    if (payload === undefined) {
      return {};
    }
    const base64 = payload.replaceAll("-", "+").replaceAll("_", "/");
    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, "=");
    return JSON.parse(atob(padded)) as JwtClaims;
  } catch {
    return {};
  }
}

function readStoredSession(): AuthSession | null {
  if (typeof sessionStorage === "undefined") {
    return null;
  }
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    return raw === null ? null : (JSON.parse(raw) as AuthSession);
  } catch {
    sessionStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

class AuthStore {
  private readonly listeners = new Set<Listener>();
  private state: AuthState;
  private expiryTimer: ReturnType<typeof setTimeout> | null = null;

  public constructor() {
    const session = readStoredSession();
    this.state = session === null ? { status: "anonymous" } : this.stateFor(session);
    this.scheduleExpiry();
  }

  public getSnapshot = (): AuthState => this.state;

  public subscribe = (listener: Listener): (() => void) => {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  };

  public setSession(result: TokenResult): void {
    const claims = decodeClaims(result.accessToken);
    const expiresAt =
      claims.exp === undefined ? Date.now() + result.expiresIn * 1000 : claims.exp * 1000;
    const session: AuthSession = {
      accessToken: result.accessToken,
      tokenType: result.tokenType,
      expiresAt,
      username: claims.preferred_username ?? null,
      userId: claims.sub ?? null,
    };
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    this.update(this.stateFor(session));
  }

  public clear(): void {
    sessionStorage.removeItem(STORAGE_KEY);
    this.update({ status: "anonymous" });
  }

  public markExpired(): void {
    if (this.state.status === "authenticated") {
      this.update({ status: "expired", session: this.state.session });
    }
  }

  public evaluateExpiry = (): void => {
    if (this.state.status === "authenticated" && this.state.session.expiresAt <= Date.now()) {
      this.markExpired();
    }
  };

  public getAccessToken(): string | null {
    this.evaluateExpiry();
    return this.state.status === "authenticated" ? this.state.session.accessToken : null;
  }

  private stateFor(session: AuthSession): AuthState {
    return session.expiresAt <= Date.now()
      ? { status: "expired", session }
      : { status: "authenticated", session };
  }

  private update(state: AuthState): void {
    this.state = state;
    this.scheduleExpiry();
    this.listeners.forEach((listener) => listener());
  }

  private scheduleExpiry(): void {
    if (this.expiryTimer !== null) {
      clearTimeout(this.expiryTimer);
      this.expiryTimer = null;
    }
    if (this.state.status !== "authenticated") {
      return;
    }
    const remaining = Math.max(0, this.state.session.expiresAt - Date.now());
    this.expiryTimer = setTimeout(this.evaluateExpiry, Math.min(remaining, 2_147_000_000));
  }
}

export const authStore = new AuthStore();

export function startAuthLifecycle(): () => void {
  const evaluate = (): void => authStore.evaluateExpiry();
  document.addEventListener("visibilitychange", evaluate);
  window.addEventListener("focus", evaluate);
  return () => {
    document.removeEventListener("visibilitychange", evaluate);
    window.removeEventListener("focus", evaluate);
  };
}
