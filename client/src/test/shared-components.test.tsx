import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import { ClientError } from "../api/errors/client-error";
import { ErrorPanel } from "../shared/components/error-panel";
import { SelectField, TextField } from "../shared/components/form-field";
import { Pagination } from "../shared/components/pagination";
import { StatusBadge } from "../shared/components/status-badge";
import { WorkPage } from "../shared/components/work-page";

describe("shared accessible components", () => {
  it("renders every available problem detail and compact mode", () => {
    const error = new ClientError({
      kind: "problem",
      title: "Başlık",
      detail: "Ayrıntı",
      status: 422,
      type: "https://x/problem",
      instance: "/resource",
      correlationId: "correlation",
      fieldErrors: { name: ["Zorunlu"] },
      originalFieldErrors: { name: ["Required"] },
      originalTitle: "Original title",
      originalDetail: "Original",
    });
    render(<ErrorPanel compact error={error} />);
    fireEvent.click(screen.getByText("Teknik ayrıntılar"));
    expect(screen.getByText(/HTTP durumu:/).parentElement).toHaveTextContent("422");
    expect(screen.getByText(/Problem türü:/).parentElement).toHaveTextContent("https://x/problem");
    expect(screen.getByText(/İstek yolu:/).parentElement).toHaveTextContent("/resource");
    expect(screen.getByText(/Correlation ID:/).parentElement).toHaveTextContent("correlation");
    expect(screen.getByText(/Özgün başlık:/).parentElement).toHaveTextContent("Original title");
    expect(screen.getByText(/Özgün ayrıntı:/).parentElement).toHaveTextContent("Original");
    expect(screen.getByText(/Ürün adı:/).parentElement).toHaveTextContent("Zorunlu");
    expect(screen.getByText(/Özgün alan hatası · name:/).parentElement).toHaveTextContent(
      "Required",
    );
  });

  it("links labels, hints and errors to text and select fields", () => {
    render(
      <>
        <TextField hint="Yardım" label="Ad" name="name" />
        <TextField error="Hatalı" label="Kod" name="code" />
        <SelectField error="Seçin" label="Tür" name="type">
          <option>Bir</option>
        </SelectField>
      </>,
    );
    expect(screen.getByLabelText("Ad")).toHaveAccessibleDescription("Yardım");
    expect(screen.getByLabelText("Kod")).toBeInvalid();
    expect(screen.getByLabelText("Tür")).toBeInvalid();
  });

  it("hides single-page pagination and drives both directions", () => {
    const change = vi.fn();
    const view = render(
      <Pagination onPageChange={change} pageNumber={1} pageSize={20} totalCount={1} />,
    );
    expect(screen.queryByRole("navigation")).not.toBeInTheDocument();
    view.rerender(
      <Pagination onPageChange={change} pageNumber={2} pageSize={20} totalCount={60} />,
    );
    fireEvent.click(screen.getByRole("button", { name: "Önceki" }));
    fireEvent.click(screen.getByRole("button", { name: "Sonraki" }));
    expect(change).toHaveBeenNthCalledWith(1, 1);
    expect(change).toHaveBeenNthCalledWith(2, 3);
  });

  it("renders page actions and status tones", () => {
    render(
      <MemoryRouter>
        <WorkPage
          action={<button type="button">Ekle</button>}
          description="Açıklama"
          title="Başlık"
        >
          <StatusBadge tone="success">Tamam</StatusBadge>
          <StatusBadge>Normal</StatusBadge>
        </WorkPage>
      </MemoryRouter>,
    );
    expect(screen.getByRole("heading", { name: "Başlık" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Ekle" })).toBeInTheDocument();
    expect(screen.getByText("Tamam")).toHaveClass("status-success");
  });
});
