import { useMutation, useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate, useSearchParams } from "react-router";
import { toast } from "sonner";
import { createStockItem, listProducts, listStockItems } from "../../api/client/rudoger-api";
import { IdempotencyIntent } from "../../api/client/idempotency";
import { queryKeys } from "../../api/query-keys";
import { queryClient } from "../../app/providers/query-client";
import { Button } from "../../shared/components/button";
import { Dialog } from "../../shared/components/dialog";
import { ErrorPanel } from "../../shared/components/error-panel";
import { SelectField, TextField } from "../../shared/components/form-field";
import { applyServerFieldErrors } from "../../shared/forms/server-field-errors";
import { Pagination } from "../../shared/components/pagination";
import { WorkPage } from "../../shared/components/work-page";
import { formatNumber } from "../../shared/formatting/formatters";
import { useAuth } from "../authn/use-auth";

interface StockDraft {
  productId: string;
  openingQuantity: string;
}

function CreateStockDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const auth = useAuth();
  const navigate = useNavigate();
  const form = useForm<StockDraft>({ defaultValues: { productId: "", openingQuantity: "0" } });
  const intent = useMemo(() => new IdempotencyIntent("stock-open"), []);
  const products = useQuery({
    queryKey: queryKeys.products({ pageNumber: 1, pageSize: 100 }),
    queryFn: ({ signal }) => listProducts({ ids: [], pageNumber: 1, pageSize: 100 }, signal),
    enabled: open && auth.status === "authenticated",
  });
  const mutation = useMutation({
    mutationFn: ({ productId, openingQuantity }: { productId: string; openingQuantity: number }) =>
      createStockItem(productId, openingQuantity, intent.keyFor({ productId, openingQuantity })),
    onError(error) {
      applyServerFieldErrors(error, form.setError);
    },
    async onSuccess(stockItem) {
      intent.reset();
      await queryClient.invalidateQueries({ queryKey: queryKeys.stockItems() });
      toast.success("Stok kaydı açıldı.");
      onOpenChange(false);
      void navigate(`/stok/${stockItem.id}`);
    },
  });
  const submit = (draft: StockDraft): void => {
    form.clearErrors();
    const quantity = Number(draft.openingQuantity);
    if (draft.productId === "") form.setError("productId", { message: "Ürün seçin." });
    if (!Number.isFinite(quantity) || quantity < 0)
      form.setError("openingQuantity", { message: "Açılış miktarı sıfır veya pozitif olmalıdır." });
    if (draft.productId !== "" && Number.isFinite(quantity) && quantity >= 0)
      mutation.mutate({ productId: draft.productId, openingQuantity: quantity });
  };
  return (
    <Dialog
      description="Her ürün için tek stok kaydı açılabilir."
      onOpenChange={onOpenChange}
      open={open}
      title="Stok kaydı aç"
    >
      {mutation.isError && (
        <div className="mb-4">
          <ErrorPanel compact error={mutation.error} />
        </div>
      )}
      {products.isError && <ErrorPanel compact error={products.error} />}
      <form
        className="grid gap-4"
        noValidate
        onSubmit={(event) => void form.handleSubmit(submit)(event)}
      >
        <SelectField
          error={form.formState.errors.productId?.message}
          label="Ürün"
          {...form.register("productId")}
        >
          <option value="">Ürün seçin</option>
          {products.data?.items.map((product) => (
            <option key={product.id} value={product.id}>
              {product.sku} — {product.name}
            </option>
          ))}
        </SelectField>
        <TextField
          error={form.formState.errors.openingQuantity?.message}
          label="Açılış miktarı"
          min="0"
          step="any"
          type="number"
          {...form.register("openingQuantity")}
        />
        <div className="flex justify-end">
          <Button
            disabled={auth.status === "expired" || mutation.isPending || products.isPending}
            type="submit"
          >
            {mutation.isPending ? "Açılıyor…" : "Stok Kaydını Aç"}
          </Button>
        </div>
      </form>
    </Dialog>
  );
}

export function InventoryPage() {
  const auth = useAuth();
  const [params, setParams] = useSearchParams();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [productInput, setProductInput] = useState(params.get("urun") ?? "");
  const pageNumber = Math.max(1, Number(params.get("sayfa")) || 1);
  const productId = params.get("urun") ?? undefined;
  const filters = productId === undefined ? { pageNumber } : { productId, pageNumber };
  const query = useQuery({
    queryKey: queryKeys.stockItems(filters),
    queryFn: ({ signal }) => listStockItems(filters, signal),
    enabled: auth.status === "authenticated",
  });
  const setPage = (page: number): void => {
    const next = new URLSearchParams(params);
    next.set("sayfa", String(page));
    setParams(next);
  };
  return (
    <WorkPage
      action={
        <Button disabled={auth.status === "expired"} onClick={() => setDialogOpen(true)}>
          Stok kaydı aç
        </Button>
      }
      description="Stok bakiyelerini ve hareket defterini yönetin."
      title="Stok"
    >
      <form
        className="panel mb-5 flex flex-col gap-3 p-4 sm:flex-row sm:items-end"
        onSubmit={(event) => {
          event.preventDefault();
          const next = new URLSearchParams();
          if (productInput.trim() !== "") next.set("urun", productInput.trim());
          setParams(next);
        }}
      >
        <div className="min-w-0 flex-1">
          <TextField
            label="Ürün ID’si"
            onChange={(event) => setProductInput(event.target.value)}
            placeholder="GUID"
            value={productInput}
          />
        </div>
        <Button type="submit" variant="secondary">
          Filtrele
        </Button>
        <Button
          onClick={() => {
            setProductInput("");
            setParams({});
          }}
          type="button"
          variant="secondary"
        >
          Temizle
        </Button>
      </form>
      {query.isPending && auth.status === "authenticated" && <p>Stok kayıtları yükleniyor…</p>}
      {query.isError && <ErrorPanel error={query.error} />}
      {query.data !== undefined && (
        <>
          <div className="panel overflow-x-auto">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Ürün ID</th>
                  <th>UoM</th>
                  <th>Eldeki</th>
                  <th>Rezerve</th>
                  <th>Kullanılabilir</th>
                  <th>
                    <span className="sr-only">İşlem</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {query.data.items.map((item) => (
                  <tr key={item.id}>
                    <td className="font-mono text-sm">{item.productId}</td>
                    <td>{item.baseUomCode}</td>
                    <td>{formatNumber(item.onHandQuantity)}</td>
                    <td>{formatNumber(item.reservedQuantity)}</td>
                    <td className="font-semibold">{formatNumber(item.availableQuantity)}</td>
                    <td>
                      <Link className="font-semibold text-[var(--accent)]" to={`/stok/${item.id}`}>
                        Görüntüle
                      </Link>
                    </td>
                  </tr>
                ))}
                {query.data.items.length === 0 && (
                  <tr>
                    <td className="text-center text-[var(--muted)]" colSpan={6}>
                      Stok kaydı bulunamadı.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination
            onPageChange={setPage}
            pageNumber={query.data.pageNumber}
            pageSize={query.data.pageSize}
            totalCount={query.data.totalCount}
          />
        </>
      )}
      <CreateStockDialog onOpenChange={setDialogOpen} open={dialogOpen} />
    </WorkPage>
  );
}
