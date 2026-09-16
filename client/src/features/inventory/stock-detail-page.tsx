import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useParams, useSearchParams } from "react-router";
import { toast } from "sonner";
import {
  createStockMovement,
  getStockItem,
  listStockMovements,
} from "../../api/client/rudoger-api";
import { IdempotencyIntent } from "../../api/client/idempotency";
import type { StockMovementType } from "../../api/contracts";
import { queryKeys } from "../../api/query-keys";
import { queryClient } from "../../app/providers/query-client";
import { Button } from "../../shared/components/button";
import { ErrorPanel } from "../../shared/components/error-panel";
import { SelectField, TextField } from "../../shared/components/form-field";
import { Pagination } from "../../shared/components/pagination";
import { applyServerFieldErrors } from "../../shared/forms/server-field-errors";
import {
  formatDateTime,
  formatNumber,
  movementTypeLabels,
} from "../../shared/formatting/formatters";
import { useAuth } from "../authn/use-auth";

type ManualMovementType = Extract<StockMovementType, "Receipt" | "Adjustment" | "Deduction">;
interface MovementDraft {
  type: ManualMovementType;
  quantity: string;
}

export function StockDetailPage() {
  const auth = useAuth();
  const { stockItemId = "" } = useParams();
  const [params, setParams] = useSearchParams();
  const pageNumber = Math.max(1, Number(params.get("sayfa")) || 1);
  const [intent] = useState(() => new IdempotencyIntent("stock-movement"));
  const form = useForm<MovementDraft>({ defaultValues: { type: "Receipt", quantity: "" } });
  const itemQuery = useQuery({
    queryKey: queryKeys.stockItem(stockItemId),
    queryFn: ({ signal }) => getStockItem(stockItemId, signal),
    enabled: auth.status === "authenticated" && stockItemId !== "",
  });
  const movementsQuery = useQuery({
    queryKey: queryKeys.stockMovements(stockItemId, pageNumber),
    queryFn: ({ signal }) => listStockMovements(stockItemId, pageNumber, signal),
    enabled: auth.status === "authenticated" && stockItemId !== "",
  });
  const mutation = useMutation({
    mutationFn: ({ type, quantity }: { type: ManualMovementType; quantity: number }) =>
      createStockMovement(
        stockItemId,
        type,
        quantity,
        intent.keyFor({ stockItemId, type, quantity }),
      ),
    onError(error) {
      applyServerFieldErrors(error, form.setError);
    },
    async onSuccess() {
      intent.reset();
      form.reset();
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.stockItem(stockItemId) }),
        queryClient.invalidateQueries({ queryKey: ["stock-movements", stockItemId] }),
        queryClient.invalidateQueries({ queryKey: queryKeys.stockItems() }),
      ]);
      toast.success("Stok hareketi işlendi.");
    },
  });
  const submit = (draft: MovementDraft): void => {
    const quantity = Number(draft.quantity);
    const valid =
      Number.isFinite(quantity) && (draft.type === "Adjustment" ? quantity !== 0 : quantity > 0);
    if (!valid) {
      form.setError("quantity", {
        message:
          draft.type === "Adjustment"
            ? "Düzeltme miktarı sıfır olamaz."
            : "Miktar sıfırdan büyük olmalıdır.",
      });
      return;
    }
    mutation.mutate({ type: draft.type, quantity });
  };
  if (itemQuery.isPending && auth.status === "authenticated") return <p>Stok kaydı yükleniyor…</p>;
  if (itemQuery.isError) return <ErrorPanel error={itemQuery.error} />;
  if (itemQuery.data === undefined)
    return <p>Oturum yenilenene kadar stok verisi gösterilemiyor.</p>;
  const item = itemQuery.data;
  return (
    <section>
      <Link className="text-sm font-semibold text-[var(--accent)]" to="/stok">
        ← Stok listesine dön
      </Link>
      <div className="mt-4">
        <h1 className="text-2xl font-bold">Stok kaydı</h1>
        <p className="mt-1 font-mono text-sm text-[var(--muted)]">{item.id}</p>
      </div>
      <div className="mt-6 grid gap-6 lg:grid-cols-[1fr_1.3fr]">
        <div className="grid gap-6">
          <section className="panel p-5">
            <h2 className="text-lg font-bold">Bakiyeler</h2>
            <dl className="mt-4 grid grid-cols-3 gap-3">
              <div>
                <dt className="text-sm text-[var(--muted)]">Eldeki</dt>
                <dd className="text-xl font-bold">{formatNumber(item.onHandQuantity)}</dd>
              </div>
              <div>
                <dt className="text-sm text-[var(--muted)]">Rezerve</dt>
                <dd className="text-xl font-bold">{formatNumber(item.reservedQuantity)}</dd>
              </div>
              <div>
                <dt className="text-sm text-[var(--muted)]">Kullanılabilir</dt>
                <dd className="text-xl font-bold">{formatNumber(item.availableQuantity)}</dd>
              </div>
            </dl>
            <p className="mt-4 text-sm text-[var(--muted)]">
              {item.baseUomCode} · Ürün{" "}
              <Link className="text-[var(--accent)]" to={`/urunler/${item.productId}`}>
                {item.productId}
              </Link>
            </p>
          </section>
          <section className="panel p-5">
            <h2 className="text-lg font-bold">Manuel hareket</h2>
            {mutation.isError && (
              <div className="mt-4">
                <ErrorPanel compact error={mutation.error} />
              </div>
            )}
            <form
              className="mt-4 grid gap-4"
              onSubmit={(event) => void form.handleSubmit(submit)(event)}
            >
              <SelectField label="Hareket türü" {...form.register("type")}>
                <option value="Receipt">Giriş</option>
                <option value="Adjustment">Düzeltme</option>
                <option value="Deduction">Düşüm</option>
              </SelectField>
              <TextField
                error={form.formState.errors.quantity?.message}
                label="Miktar"
                step="any"
                type="number"
                {...form.register("quantity")}
              />
              <Button disabled={auth.status === "expired" || mutation.isPending} type="submit">
                {mutation.isPending ? "İşleniyor…" : "Hareketi İşle"}
              </Button>
            </form>
          </section>
        </div>
        <section>
          <h2 className="text-lg font-bold">Hareket defteri</h2>
          {movementsQuery.isError && (
            <div className="mt-3">
              <ErrorPanel error={movementsQuery.error} />
            </div>
          )}
          {movementsQuery.isPending && <p className="mt-3">Hareketler yükleniyor…</p>}
          {movementsQuery.data !== undefined && (
            <>
              <div className="panel mt-3 overflow-x-auto">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Zaman</th>
                      <th>Tür</th>
                      <th>Eldeki Δ</th>
                      <th>Rezerve Δ</th>
                      <th>Kaynak</th>
                    </tr>
                  </thead>
                  <tbody>
                    {movementsQuery.data.items.map((movement) => (
                      <tr key={movement.id}>
                        <td className="text-sm whitespace-nowrap">
                          {formatDateTime(movement.occurredAtUtc)}
                        </td>
                        <td>{movementTypeLabels[movement.type]}</td>
                        <td>{formatNumber(movement.onHandQuantityDelta)}</td>
                        <td>{formatNumber(movement.reservedQuantityDelta)}</td>
                        <td className="text-sm">
                          {movement.referenceType}
                          {movement.referenceId !== null ? (
                            <>
                              <br />
                              <span className="font-mono text-xs">{movement.referenceId}</span>
                            </>
                          ) : null}
                        </td>
                      </tr>
                    ))}
                    {movementsQuery.data.items.length === 0 && (
                      <tr>
                        <td className="text-center text-[var(--muted)]" colSpan={5}>
                          Hareket bulunamadı.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
              <Pagination
                onPageChange={(page) => {
                  const next = new URLSearchParams(params);
                  next.set("sayfa", String(page));
                  setParams(next);
                }}
                pageNumber={movementsQuery.data.pageNumber}
                pageSize={movementsQuery.data.pageSize}
                totalCount={movementsQuery.data.totalCount}
              />
            </>
          )}
        </section>
      </div>
    </section>
  );
}
