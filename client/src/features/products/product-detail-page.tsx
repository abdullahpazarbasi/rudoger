import { useMutation, useQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate, useParams } from "react-router";
import { toast } from "sonner";
import {
  addPackaging,
  deletePackaging,
  deleteProduct,
  getProduct,
  patchPackaging,
  patchProduct,
} from "../../api/client/rudoger-api";
import { changedFields } from "../../api/client/patch";
import type { PackagingInput, Product, ProductPackaging } from "../../api/contracts";
import { queryKeys } from "../../api/query-keys";
import { queryClient } from "../../app/providers/query-client";
import { Button } from "../../shared/components/button";
import { Dialog } from "../../shared/components/dialog";
import { ErrorPanel } from "../../shared/components/error-panel";
import { TextField } from "../../shared/components/form-field";
import { formatMoney, formatNumber } from "../../shared/formatting/formatters";
import { applyServerFieldErrors } from "../../shared/forms/server-field-errors";
import { useAuth } from "../authn/use-auth";
import {
  emptyPackaging,
  standalonePackagingSchema,
  type PackagingDraft,
} from "./product-validation";

interface ProductEditDraft {
  sku: string;
  name: string;
  basePriceAmount: string;
  basePriceCurrencyCode: string;
}

function productDraft(product: Product): ProductEditDraft {
  return {
    sku: product.sku,
    name: product.name,
    basePriceAmount: String(product.basePriceAmount),
    basePriceCurrencyCode: product.basePriceCurrencyCode,
  };
}

function packagingDraft(packaging: ProductPackaging | null, nextLevel: number): PackagingDraft {
  if (packaging === null) return emptyPackaging(nextLevel);
  return {
    level: String(packaging.level),
    uomCode: packaging.uomCode,
    conversionFactor: String(packaging.conversionFactor),
    barcode: packaging.barcode ?? "",
    weightInKg: packaging.weightInKg === null ? "" : String(packaging.weightInKg),
    lengthInMm: packaging.lengthInMm === null ? "" : String(packaging.lengthInMm),
    widthInMm: packaging.widthInMm === null ? "" : String(packaging.widthInMm),
    heightInMm: packaging.heightInMm === null ? "" : String(packaging.heightInMm),
  };
}

function PackagingDialog({
  product,
  selected,
  open,
  onOpenChange,
}: {
  product: Product;
  selected: ProductPackaging | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const auth = useAuth();
  const nextLevel = Math.max(...product.packagings.map((item) => item.level), -1) + 1;
  const form = useForm<PackagingDraft>({ defaultValues: packagingDraft(selected, nextLevel) });
  useEffect(() => form.reset(packagingDraft(selected, nextLevel)), [form, nextLevel, selected]);
  const mutation = useMutation({
    mutationFn: async (input: PackagingInput) => {
      if (selected === null) return addPackaging(product.id, input);
      const before = { ...selected };
      const after = { ...input, id: selected.id };
      const operations = changedFields(before, after, {
        level: "/level",
        uomCode: "/uomCode",
        conversionFactor: "/conversionFactor",
        barcode: "/barcode",
        weightInKg: "/weightInKg",
        lengthInMm: "/lengthInMm",
        widthInMm: "/widthInMm",
        heightInMm: "/heightInMm",
      });
      if (operations.length === 0) return selected;
      return patchPackaging(product.id, selected.id, operations);
    },
    onError(error) {
      applyServerFieldErrors(error, form.setError);
    },
    async onSuccess() {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.product(product.id) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.products() }),
      ]);
      toast.success(selected === null ? "Packaging eklendi." : "Packaging güncellendi.");
      onOpenChange(false);
    },
  });
  const submit = (draft: PackagingDraft): void => {
    form.clearErrors();
    const parsed = standalonePackagingSchema.safeParse(draft);
    if (!parsed.success) {
      parsed.error.issues.forEach((issue) =>
        form.setError(issue.path.join(".") as keyof PackagingDraft, { message: issue.message }),
      );
      return;
    }
    mutation.mutate(parsed.data);
  };
  const base = selected?.level === 0;
  return (
    <Dialog
      onOpenChange={onOpenChange}
      open={open}
      title={selected === null ? "Packaging ekle" : "Packaging düzenle"}
    >
      {mutation.isError && (
        <div className="mb-4">
          <ErrorPanel compact error={mutation.error} />
        </div>
      )}
      <form
        className="grid gap-4 sm:grid-cols-2"
        noValidate
        onSubmit={(event) => void form.handleSubmit(submit)(event)}
      >
        <TextField
          disabled={base}
          error={form.formState.errors.level?.message}
          label="Level"
          min="0"
          step="1"
          type="number"
          {...form.register("level")}
        />
        <TextField
          disabled={base}
          error={form.formState.errors.uomCode?.message}
          label="UoM kodu"
          maxLength={16}
          {...form.register("uomCode")}
        />
        <TextField
          disabled={base}
          error={form.formState.errors.conversionFactor?.message}
          label="Dönüşüm katsayısı"
          min="0"
          step="any"
          type="number"
          {...form.register("conversionFactor")}
        />
        <TextField
          error={form.formState.errors.barcode?.message}
          label="Barkod"
          maxLength={64}
          {...form.register("barcode")}
        />
        <TextField
          error={form.formState.errors.weightInKg?.message}
          label="Ağırlık (kg)"
          min="0"
          step="any"
          type="number"
          {...form.register("weightInKg")}
        />
        <TextField
          error={form.formState.errors.lengthInMm?.message}
          label="Uzunluk (mm)"
          min="0"
          step="any"
          type="number"
          {...form.register("lengthInMm")}
        />
        <TextField
          error={form.formState.errors.widthInMm?.message}
          label="Genişlik (mm)"
          min="0"
          step="any"
          type="number"
          {...form.register("widthInMm")}
        />
        <TextField
          error={form.formState.errors.heightInMm?.message}
          label="Yükseklik (mm)"
          min="0"
          step="any"
          type="number"
          {...form.register("heightInMm")}
        />
        <div className="flex justify-end sm:col-span-2">
          <Button disabled={auth.status === "expired" || mutation.isPending} type="submit">
            {mutation.isPending ? "Kaydediliyor…" : "Kaydet"}
          </Button>
        </div>
      </form>
    </Dialog>
  );
}

export function ProductDetailPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const { productId = "" } = useParams();
  const [packagingOpen, setPackagingOpen] = useState(false);
  const [selectedPackaging, setSelectedPackaging] = useState<ProductPackaging | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ProductPackaging | "product" | null>(null);
  const query = useQuery({
    queryKey: queryKeys.product(productId),
    queryFn: ({ signal }) => getProduct(productId, signal),
    enabled: auth.status === "authenticated" && productId !== "",
  });
  const form = useForm<ProductEditDraft>();
  useEffect(() => {
    if (query.data !== undefined) form.reset(productDraft(query.data));
  }, [form, query.data]);

  const editMutation = useMutation({
    mutationFn: (draft: ProductEditDraft) => {
      const product = query.data;
      if (product === undefined) {
        throw new Error("Product data is required before editing.");
      }
      const after = {
        sku: draft.sku.trim().toUpperCase(),
        name: draft.name.trim(),
        basePriceAmount: Number(draft.basePriceAmount),
        basePriceCurrencyCode: draft.basePriceCurrencyCode.trim().toUpperCase(),
      };
      const before = {
        sku: product.sku,
        name: product.name,
        basePriceAmount: product.basePriceAmount,
        basePriceCurrencyCode: product.basePriceCurrencyCode,
      };
      return patchProduct(
        product.id,
        changedFields(before, after, {
          sku: "/sku",
          name: "/name",
          basePriceAmount: "/basePriceAmount",
          basePriceCurrencyCode: "/basePriceCurrencyCode",
        }),
      );
    },
    onError(error) {
      applyServerFieldErrors(error, form.setError);
    },
    async onSuccess(product) {
      queryClient.setQueryData(queryKeys.product(product.id), product);
      await queryClient.invalidateQueries({ queryKey: queryKeys.products() });
      toast.success("Ürün güncellendi.");
    },
  });
  const deleteMutation = useMutation({
    mutationFn: async () => {
      if (deleteTarget === "product") return deleteProduct(productId);
      if (deleteTarget !== null) return deletePackaging(productId, deleteTarget.id);
    },
    async onSuccess() {
      if (deleteTarget === "product") {
        await queryClient.invalidateQueries({ queryKey: queryKeys.products() });
        toast.success("Ürün silindi.");
        void navigate("/urunler", { replace: true });
      } else {
        await Promise.all([
          queryClient.invalidateQueries({ queryKey: queryKeys.product(productId) }),
          queryClient.invalidateQueries({ queryKey: queryKeys.products() }),
        ]);
        toast.success("Packaging silindi.");
      }
      setDeleteTarget(null);
    },
  });

  if (query.isPending && auth.status === "authenticated") return <p>Ürün yükleniyor…</p>;
  if (query.isError) return <ErrorPanel error={query.error} />;
  if (query.data === undefined) return <p>Oturum yenilenene kadar ürün verisi gösterilemiyor.</p>;
  const product = query.data;
  return (
    <section>
      <Link className="text-sm font-semibold text-[var(--accent)]" to="/urunler">
        ← Ürünlere dön
      </Link>
      <div className="mt-4 flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold">{product.name}</h1>
          <p className="mt-1 font-mono text-sm text-[var(--muted)]">{product.id}</p>
        </div>
        <Button
          disabled={auth.status === "expired"}
          onClick={() => setDeleteTarget("product")}
          variant="danger"
        >
          Ürünü sil
        </Button>
      </div>
      <div className="mt-6 grid gap-6">
        <section className="panel p-5">
          <h2 className="text-lg font-bold">Ürün bilgileri</h2>
          {editMutation.isError && (
            <div className="mt-4">
              <ErrorPanel compact error={editMutation.error} />
            </div>
          )}
          <form
            className="mt-4 grid gap-4 sm:grid-cols-2"
            onSubmit={(event) =>
              void form.handleSubmit((draft) => editMutation.mutate(draft))(event)
            }
          >
            <TextField
              error={form.formState.errors.sku?.message}
              label="SKU"
              maxLength={64}
              required
              {...form.register("sku")}
            />
            <TextField
              error={form.formState.errors.name?.message}
              label="Ürün adı"
              maxLength={200}
              required
              {...form.register("name")}
            />
            <TextField disabled label="Temel UoM" value={product.baseUomCode} />
            <TextField
              error={form.formState.errors.basePriceAmount?.message}
              label="Temel fiyat"
              min="0"
              step="any"
              type="number"
              required
              {...form.register("basePriceAmount")}
            />
            <TextField
              error={form.formState.errors.basePriceCurrencyCode?.message}
              label="Para birimi"
              maxLength={3}
              required
              {...form.register("basePriceCurrencyCode")}
            />
            <div className="flex items-end justify-end">
              <Button disabled={auth.status === "expired" || editMutation.isPending} type="submit">
                {editMutation.isPending ? "Kaydediliyor…" : "Değişiklikleri Kaydet"}
              </Button>
            </div>
          </form>
        </section>
        <section>
          <div className="flex items-center justify-between gap-4">
            <div>
              <h2 className="text-lg font-bold">Packaging</h2>
              <p className="text-sm text-[var(--muted)]">
                Temel fiyat: {formatMoney(product.basePriceAmount, product.basePriceCurrencyCode)}
              </p>
            </div>
            <Button
              disabled={auth.status === "expired"}
              onClick={() => {
                setSelectedPackaging(null);
                setPackagingOpen(true);
              }}
            >
              Packaging ekle
            </Button>
          </div>
          <div className="panel mt-4 overflow-x-auto">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Level</th>
                  <th>UoM</th>
                  <th>Katsayı</th>
                  <th>Barkod</th>
                  <th>Ölçüler</th>
                  <th>
                    <span className="sr-only">İşlemler</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {product.packagings.map((item) => (
                  <tr key={item.id}>
                    <td>{item.level}</td>
                    <td>{item.uomCode}</td>
                    <td>{formatNumber(item.conversionFactor)}</td>
                    <td>{item.barcode ?? "—"}</td>
                    <td className="text-sm">
                      {[
                        item.weightInKg !== null ? `${formatNumber(item.weightInKg)} kg` : null,
                        item.lengthInMm !== null &&
                        item.widthInMm !== null &&
                        item.heightInMm !== null
                          ? `${formatNumber(item.lengthInMm)}×${formatNumber(item.widthInMm)}×${formatNumber(item.heightInMm)} mm`
                          : null,
                      ]
                        .filter(Boolean)
                        .join(" · ") || "—"}
                    </td>
                    <td>
                      <div className="flex gap-2">
                        <Button
                          onClick={() => {
                            setSelectedPackaging(item);
                            setPackagingOpen(true);
                          }}
                          variant="secondary"
                        >
                          Düzenle
                        </Button>
                        <Button
                          disabled={item.level === 0 || auth.status === "expired"}
                          onClick={() => setDeleteTarget(item)}
                          variant="danger"
                        >
                          Sil
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </div>
      <PackagingDialog
        onOpenChange={setPackagingOpen}
        open={packagingOpen}
        product={product}
        selected={selectedPackaging}
      />
      <Dialog
        footer={
          <>
            <Button onClick={() => setDeleteTarget(null)} variant="secondary">
              Vazgeç
            </Button>
            <Button
              disabled={auth.status === "expired" || deleteMutation.isPending}
              onClick={() => deleteMutation.mutate()}
              variant="danger"
            >
              {deleteMutation.isPending ? "Siliniyor…" : "Sil"}
            </Button>
          </>
        }
        onOpenChange={(open) => {
          if (!open) setDeleteTarget(null);
        }}
        open={deleteTarget !== null}
        title={deleteTarget === "product" ? "Ürün silinsin mi?" : "Packaging silinsin mi?"}
      >
        {deleteMutation.isError && <ErrorPanel compact error={deleteMutation.error} />}
        <p className="text-[var(--muted)]">
          Bu işlem geri alınamaz. API iş kuralları kullanımdaki ürünlerin silinmesini engeller.
        </p>
      </Dialog>
    </section>
  );
}
