import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { authStore, startAuthLifecycle } from "../features/authn/auth-store";
import { ThemeToggle } from "../shared/theme/theme-toggle";
import { startThemeLifecycle, themeStore } from "../shared/theme/theme-store";

function token(exp: number, claims: Record<string, unknown> = {}): string {
  return `header.${btoa(JSON.stringify({ exp, sub: "01990101-0000-7000-8000-000000000001", preferred_username: "abdullah", ...claims }))}.signature`;
}

describe("reactive auth state", () => {
  beforeEach(() => {
    authStore.clear();
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-01-01T00:00:00Z"));
  });
  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
    authStore.clear();
  });

  it("moves to expired at exp without navigating", () => {
    const listener = vi.fn();
    const unsubscribe = authStore.subscribe(listener);
    authStore.setSession({
      accessToken: token(Math.floor(Date.now() / 1000) + 5),
      tokenType: "Bearer",
      expiresIn: 5,
    });
    expect(authStore.getSnapshot()).toMatchObject({
      status: "authenticated",
      session: { username: "abdullah" },
    });
    act(() => {
      vi.advanceTimersByTime(5_000);
    });
    expect(authStore.getSnapshot().status).toBe("expired");
    expect(location.pathname).not.toBe("/giris");
    expect(authStore.getAccessToken()).toBeNull();
    expect(listener).toHaveBeenCalled();
    unsubscribe();
  });

  it("restores expired/invalid sessions and responds to lifecycle events", () => {
    authStore.setSession({ accessToken: "not-a-jwt", tokenType: "Bearer", expiresIn: 1 });
    expect(authStore.getAccessToken()).toBe("not-a-jwt");
    const stop = startAuthLifecycle();
    act(() => {
      vi.advanceTimersByTime(1_001);
      window.dispatchEvent(new Event("focus"));
      document.dispatchEvent(new Event("visibilitychange"));
    });
    expect(authStore.getSnapshot().status).toBe("expired");
    authStore.markExpired();
    stop();
    authStore.clear();
    expect(authStore.getSnapshot()).toEqual({ status: "anonymous" });
  });

  it("uses expiresIn and empty claims when a token cannot provide JWT claims", () => {
    authStore.setSession({ accessToken: "header.{.signature", tokenType: "Bearer", expiresIn: 10 });
    expect(authStore.getSnapshot()).toMatchObject({
      status: "authenticated",
      session: { username: null, userId: null },
    });
    expect(authStore.getAccessToken()).toBe("header.{.signature");
  });

  it("initializes safely without storage and clears a corrupted stored session", async () => {
    const storage = globalThis.sessionStorage;
    vi.stubGlobal("sessionStorage", undefined);
    vi.resetModules();
    const withoutStorage = await import("../features/authn/auth-store");
    expect(withoutStorage.authStore.getSnapshot()).toEqual({ status: "anonymous" });

    vi.stubGlobal("sessionStorage", storage);
    storage.setItem("rudoger.session", "{");
    vi.resetModules();
    const withCorruptStorage = await import("../features/authn/auth-store");
    expect(withCorruptStorage.authStore.getSnapshot()).toEqual({ status: "anonymous" });
    expect(storage.getItem("rudoger.session")).toBeNull();

    storage.setItem(
      "rudoger.session",
      JSON.stringify({
        accessToken: "stored-token",
        tokenType: "Bearer",
        expiresAt: Date.now() + 60_000,
        username: "stored-user",
        userId: null,
      }),
    );
    vi.resetModules();
    const withValidStorage = await import("../features/authn/auth-store");
    expect(withValidStorage.authStore.getSnapshot()).toMatchObject({ status: "authenticated" });
    withValidStorage.authStore.clear();
  });
});

describe("reactive theme state", () => {
  it("renders System, Light and Dark as a controlled radio group", () => {
    themeStore.set("system");
    render(<ThemeToggle />);
    fireEvent.click(screen.getByRole("radio", { name: "Koyu" }));
    expect(themeStore.getSnapshot()).toBe("dark");
    expect(document.documentElement).toHaveClass("dark");
    fireEvent.click(screen.getByRole("radio", { name: "Açık" }));
    expect(document.documentElement).not.toHaveClass("dark");
    fireEvent.click(screen.getByRole("radio", { name: "Sistem" }));
    expect(localStorage.getItem("rudoger.theme")).toBe("system");
  });

  it("starts and stops system and storage listeners", () => {
    const media = window.matchMedia("(prefers-color-scheme: dark)");
    vi.spyOn(window, "matchMedia").mockReturnValue(media);
    const add = vi.spyOn(media, "addEventListener");
    const remove = vi.spyOn(media, "removeEventListener");
    const stop = startThemeLifecycle();
    expect(add).toHaveBeenCalledWith("change", expect.any(Function));
    const onMediaChange = add.mock.calls[0]?.[1] as EventListener;
    themeStore.set("system");
    onMediaChange(new Event("change"));
    themeStore.set("dark");
    onMediaChange(new Event("change"));
    themeStore.set("dark");
    window.dispatchEvent(new StorageEvent("storage", { key: "rudoger.theme", newValue: "dark" }));
    expect(themeStore.getSnapshot()).toBe("dark");
    window.dispatchEvent(new StorageEvent("storage", { key: "other", newValue: "light" }));
    window.dispatchEvent(new StorageEvent("storage", { key: "rudoger.theme", newValue: "sepia" }));
    stop();
    expect(remove).toHaveBeenCalledWith("change", expect.any(Function));
  });
});
