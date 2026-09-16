import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { getHealth } from "../api/client/rudoger-api";
import type { HealthStatus } from "../api/client/rudoger-api";
import { HealthGate } from "../app/providers/health-gate";

vi.mock("../api/client/rudoger-api", () => ({ getHealth: vi.fn() }));
const getHealthMock = vi.mocked(getHealth);

function renderGate() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0, throwOnError: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <HealthGate>
        <button type="button">Uygulama işlemi</button>
      </HealthGate>
    </QueryClientProvider>,
  );
}

describe("health gate", () => {
  beforeEach(() => getHealthMock.mockReset());

  it("cannot be dismissed until a manual retry reports Healthy", async () => {
    let resolveRetry: (value: HealthStatus) => void = () => undefined;
    const retry = new Promise<HealthStatus>((resolve) => {
      resolveRetry = resolve;
    });
    getHealthMock
      .mockResolvedValueOnce({ healthy: false, status: "Unhealthy", checks: {} })
      .mockReturnValueOnce(retry);
    renderGate();
    expect(await screen.findByText(/Bildirilen durum:/)).toHaveTextContent("Unhealthy");
    expect(
      screen.getByRole("dialog", { name: "API bağlantısı kullanılamıyor" }),
    ).toBeInTheDocument();
    fireEvent.keyDown(screen.getByRole("dialog"), { key: "Escape" });
    fireEvent.pointerDown(document.body);
    expect(screen.getByRole("dialog")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Yeniden Erişmeyi Dene" }));
    expect(await screen.findByRole("button", { name: "Erişim deneniyor…" })).toBeDisabled();
    resolveRetry({ healthy: true, status: "Healthy", checks: {} });
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(getHealthMock).toHaveBeenCalledTimes(2);
  });

  it("does not block the application when the initial response is healthy", async () => {
    getHealthMock.mockResolvedValue({ healthy: true, status: "Healthy", checks: {} });
    renderGate();
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(screen.getByRole("button", { name: "Uygulama işlemi" })).toBeEnabled();
    expect(getHealthMock).toHaveBeenCalledTimes(1);
  });

  it("keeps the gate closed over the application after a network failure", async () => {
    getHealthMock.mockRejectedValueOnce(new TypeError("offline"));
    renderGate();
    expect(await screen.findByText("API’ye erişilemiyor.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Yeniden Erişmeyi Dene" })).toBeEnabled();
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });
});
