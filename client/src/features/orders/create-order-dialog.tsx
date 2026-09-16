import { useMutation, useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState, type ChangeEvent } from "react";
import { useFieldArray, useForm, useWatch } from "react-hook-form";
import { useNavigate } from "react-router";
import { toast } from "sonner";
import { createOrder, getOrderPlacement, listProducts } from "../../api/client/rudoger-api";
import { IdempotencyIntent } from "../../api/client/idempotency";
import { translateFailure } from "../../api/errors/problem-translations";
import { queryKeys } from "../../api/query-keys";
import { queryClient } from "../../app/providers/query-client";
import { Button } from "../../shared/components/button";
import { Dialog } from "../../shared/components/dialog";
import { ErrorPanel } from "../../shared/components/error-panel";
import { SelectField, TextField } from "../../shared/components/form-field";
import { applyServerFieldErrors } from "../../shared/forms/server-field-errors";
import { useAuth } from "../authn/use-auth";

interface OrderLineDraft {
  productId: string;
  uomCode: string;
  quantity: string;
}
interface OrderDraft {
  lines: OrderLineDraft[];
}
const emptyLine = (): OrderLineDraft => ({ productId: "", uomCode: "", quantity: "" });

export function CreateOrderDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const auth = useAuth();
  const navigate = useNavigate();
  const [placementId, setPlacementId] = useState<string | null>(null);
  const form = useForm<OrderDraft>({ defaultValues: { lines: [emptyLine()] } });
  const fields = useFieldArray({ control: form.control, name: "lines" });
  const lines = useWatch({ control: form.control, name: "lines" });
  const intent = useMemo(() => new IdempotencyIntent("order-place"), []);
  const products = useQuery({
    queryKey: queryKeys.products({ pageNumber: 1, pageSize: 100 }),
    queryFn: ({ signal }) => listProducts({ ids: [], pageNumber: 1, pageSize: 100 }, signal),
    enabled: open && auth.status === "authenticated",
  });
  const mutation = useMutation({
    mutationFn: (input: {
      lines: Array<{ productId: string; uomCode: string; quantity: number }>;
    }) => createOrder(input, intent.keyFor(input)),
    onError(error) {
      applyServerFieldErrors(error, form.setError);
    },
    onSuccess(placement) {
      setPlacementId(placement.id);
    },
  });
  const placement = useQuery({
    queryKey: queryKeys.orderPlacement(placementId ?? ""),
    queryFn: ({ signal }) =>
      placementId === null
        ? Promise.reject(new Error("Placement id is required."))
        : getOrderPlacement(placementId, signal),
    enabled: placementId !== null && auth.status === "authenticated",
    refetchInterval: (query) => (query.state.data?.status === "Pending" ? 750 : false),
  });
  useEffect(() => {
    if (placement.data?.status !== "Succeeded") return;
    intent.reset();
    void queryClient.invalidateQueries({ queryKey: queryKeys.orders(1) });
    toast.success("Sipariş oluşturuldu.");
    void navigate(`/siparisler/${placement.data.orderId}`);
  }, [intent, navigate, placement.data]);

  const selectedCurrencies = new Set(
    lines
      .map(
        (line) =>
          products.data?.items.find((product) => product.id === line.productId)
            ?.basePriceCurrencyCode,
      )
      .filter((value): value is string => value !== undefined),
  );
  const submit = (draft: OrderDraft): void => {
    form.clearErrors();
    if (draft.lines.length < 1 || draft.lines.length > 100) {
      form.setError("lines", { message: "Sipariş 1 ile 100 arasında satır içermelidir." });
      return;
    }
    const pairs = new Set<string>();
    let valid = true;
    draft.lines.forEach((line, index) => {
      if (line.productId === "") {
        form.setError(`lines.${index}.productId`, { message: "Ürün seçin." });
        valid = false;
      }
      if (line.uomCode === "") {
        form.setError(`lines.${index}.uomCode`, { message: "UoM seçin." });
        valid = false;
      }
      const quantity = Number(line.quantity);
      if (!Number.isFinite(quantity) || quantity <= 0) {
        form.setError(`lines.${index}.quantity`, { message: "Miktar sıfırdan büyük olmalıdır." });
        valid = false;
      }
      const pair = `${line.productId}/${line.uomCode}`;
      if (pairs.has(pair)) {
        form.setError(`lines.${index}.uomCode`, {
          message: "Ürün/UoM ikilisi benzersiz olmalıdır.",
        });
        valid = false;
      }
      pairs.add(pair);
    });
    if (selectedCurrencies.size > 1) {
      form.setError("lines", { message: "Bir siparişte farklı para birimleri kullanılamaz." });
      valid = false;
    }
    if (valid)
      mutation.mutate({
        lines: draft.lines.map((line) => ({
          productId: line.productId,
          uomCode: line.uomCode,
          quantity: Number(line.quantity),
        })),
      });
  };
  const pending = mutation.isPending || placement.data?.status === "Pending";
  return (
    <Dialog
      description="Stok uygunluğu arka plandaki placement sürecinde doğrulanır."
      onOpenChange={onOpenChange}
      open={open}
      title="Yeni sipariş"
    >
      {(mutation.isError || placement.isError) && (
        <div className="mb-4">
          <ErrorPanel compact error={mutation.error ?? placement.error} />
        </div>
      )}
      {placement.data?.status === "Failed" && (
        <section
          className="mb-4 rounded-lg border border-[var(--danger)] bg-[var(--danger-soft)] p-4 text-[var(--danger)]"
          role="alert"
        >
          <h3 className="font-bold">Sipariş oluşturulamadı</h3>
          <p className="mt-1 text-sm">
            {translateFailure(placement.data.failureCode, placement.data.failureDetail)}
          </p>
          <details className="mt-2 text-sm">
            <summary>Teknik ayrıntılar</summary>
            <p>Kod: {placement.data.failureCode ?? "—"}</p>
            <p>{placement.data.failureDetail ?? "Ayrıntı bildirilmedi."}</p>
          </details>
        </section>
      )}
      {form.formState.errors.lines?.message !== undefined && (
        <p className="mb-3 text-sm text-[var(--danger)]" role="alert">
          {form.formState.errors.lines.message}
        </p>
      )}
      <form
        className="grid gap-4"
        noValidate
        onSubmit={(event) => void form.handleSubmit(submit)(event)}
      >
        {fields.fields.map((field, index) => {
          const product = products.data?.items.find((item) => item.id === lines[index]?.productId);
          return (
            <fieldset className="rounded-lg border p-4" disabled={pending} key={field.id}>
              <div className="mb-3 flex items-center justify-between">
                <legend className="font-semibold">Satır {index + 1}</legend>
                <Button
                  disabled={fields.fields.length === 1}
                  onClick={() => fields.remove(index)}
                  type="button"
                  variant="secondary"
                >
                  Kaldır
                </Button>
              </div>
              <div className="grid gap-4 sm:grid-cols-3">
                <SelectField
                  error={form.formState.errors.lines?.[index]?.productId?.message}
                  label="Ürün"
                  {...form.register(`lines.${index}.productId`, {
                    onChange(event: ChangeEvent<HTMLSelectElement>) {
                      const selected = products.data?.items.find(
                        (item) => item.id === event.target.value,
                      );
                      form.setValue(
                        `lines.${index}.uomCode`,
                        selected?.packagings[0]?.uomCode ?? "",
                      );
                    },
                  })}
                >
                  <option value="">Ürün seçin</option>
                  {products.data?.items.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.sku} — {item.name}
                    </option>
                  ))}
                </SelectField>
                <SelectField
                  error={form.formState.errors.lines?.[index]?.uomCode?.message}
                  label="UoM"
                  {...form.register(`lines.${index}.uomCode`)}
                >
                  <option value="">UoM seçin</option>
                  {product?.packagings.map((packaging) => (
                    <option key={packaging.id} value={packaging.uomCode}>
                      {packaging.uomCode} (×{packaging.conversionFactor})
                    </option>
                  ))}
                </SelectField>
                <TextField
                  error={form.formState.errors.lines?.[index]?.quantity?.message}
                  label="Miktar"
                  min="0"
                  step="any"
                  type="number"
                  {...form.register(`lines.${index}.quantity`)}
                />
              </div>
            </fieldset>
          );
        })}
        <div className="flex flex-wrap items-center justify-between gap-3">
          <Button
            disabled={pending || fields.fields.length >= 100}
            onClick={() => fields.append(emptyLine())}
            type="button"
            variant="secondary"
          >
            Satır ekle
          </Button>
          <div className="flex items-center gap-3">
            {selectedCurrencies.size === 1 && (
              <span className="text-sm text-[var(--muted)]">
                Para birimi: {[...selectedCurrencies][0]}
              </span>
            )}
            <Button
              disabled={auth.status === "expired" || pending || products.isPending}
              type="submit"
            >
              {pending
                ? "Sipariş işleniyor…"
                : placement.data?.status === "Failed"
                  ? "Yeniden Dene"
                  : "Siparişi Oluştur"}
            </Button>
          </div>
        </div>
      </form>
    </Dialog>
  );
}
