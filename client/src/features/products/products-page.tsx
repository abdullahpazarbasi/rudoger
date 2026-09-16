import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router";
import { z } from "zod";
import { listProducts } from "../../api/client/rudoger-api";
import { queryKeys } from "../../api/query-keys";
import { Button } from "../../shared/components/button";
import { ErrorPanel } from "../../shared/components/error-panel";
import { TextField } from "../../shared/components/form-field";
import { Pagination } from "../../shared/components/pagination";
import { WorkPage } from "../../shared/components/work-page";
import { formatMoney } from "../../shared/formatting/formatters";
import { useAuth } from "../authn/use-auth";
import { CreateProductDialog } from "./create-product-dialog";

const guidSchema = z.uuid();

function parseIds(value: string): { ids: string[]; error?: string } {
  const candidates = value
    .split(/[\s,;]+/)
    .map((item) => item.trim())
    .filter(Boolean);
  const invalid = candidates.find((item) => !guidSchema.safeParse(item).success);
  return invalid === undefined
    ? { ids: [...new Set(candidates)] }
    : { ids: [], error: `“${invalid}” geçerli bir ürün ID’si değil.` };
}

export function ProductsPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const [dialogOpen, setDialogOpen] = useState(false);
  const [idInput, setIdInput] = useState(searchParams.get("ids") ?? "");
  const [idError, setIdError] = useState<string>();
  const pageNumber = Math.max(1, Number(searchParams.get("sayfa")) || 1);
  const ids = parseIds(searchParams.get("ids") ?? "").ids;
  const filters = { ids, pageNumber };
  const query = useQuery({
    queryKey: queryKeys.products(filters),
    queryFn: ({ signal }) => listProducts(filters, signal),
    enabled: auth.status === "authenticated",
  });
  const applyFilter = (): void => {
    const parsed = parseIds(idInput);
    setIdError(parsed.error);
    if (parsed.error !== undefined) return;
    const next = new URLSearchParams();
    if (parsed.ids.length > 0) next.set("ids", parsed.ids.join(","));
    setSearchParams(next);
  };
  const setPage = (page: number): void => {
    const next = new URLSearchParams(searchParams);
    next.set("sayfa", String(page));
    setSearchParams(next);
  };

  return (
    <WorkPage
      action={
        <Button disabled={auth.status === "expired"} onClick={() => setDialogOpen(true)}>
          Yeni ürün
        </Button>
      }
      description="Ürünleri ve packaging seçeneklerini yönetin."
      title="Ürünler"
    >
      {auth.status === "expired" && (
        <p className="panel mb-4 p-4 text-[var(--muted)]">
          Oturum yenilenene kadar veriler güncellenmeyecek.
        </p>
      )}
      <form
        className="panel mb-5 flex flex-col gap-3 p-4 sm:flex-row sm:items-end"
        onSubmit={(event) => {
          event.preventDefault();
          applyFilter();
        }}
      >
        <div className="min-w-0 flex-1">
          <TextField
            error={idError}
            label="Ürün ID’leri"
            onChange={(event) => setIdInput(event.target.value)}
            placeholder="Virgül veya boşlukla ayırın"
            value={idInput}
          />
        </div>
        <Button type="submit" variant="secondary">
          Filtrele
        </Button>
        <Button
          onClick={() => {
            setIdInput("");
            setIdError(undefined);
            setSearchParams({});
          }}
          type="button"
          variant="secondary"
        >
          Temizle
        </Button>
      </form>
      {query.isPending && auth.status === "authenticated" && <p>Ürünler yükleniyor…</p>}
      {query.isError && <ErrorPanel error={query.error} />}
      {query.data !== undefined && (
        <>
          <div className="panel overflow-x-auto">
            <table className="data-table">
              <thead>
                <tr>
                  <th>SKU</th>
                  <th>Ürün</th>
                  <th>Temel UoM</th>
                  <th>Fiyat</th>
                  <th>
                    <span className="sr-only">İşlem</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {query.data.items.map((product) => (
                  <tr key={product.id}>
                    <td className="font-mono text-sm">{product.sku}</td>
                    <td className="font-semibold">{product.name}</td>
                    <td>{product.baseUomCode}</td>
                    <td>{formatMoney(product.basePriceAmount, product.basePriceCurrencyCode)}</td>
                    <td>
                      <Link
                        className="font-semibold text-[var(--accent)]"
                        to={`/urunler/${product.id}`}
                      >
                        Görüntüle
                      </Link>
                    </td>
                  </tr>
                ))}
                {query.data.items.length === 0 && (
                  <tr>
                    <td className="text-center text-[var(--muted)]" colSpan={5}>
                      Bu ölçütlerde ürün bulunamadı.
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
      <CreateProductDialog
        onCreated={(product) => void navigate(`/urunler/${product.id}`)}
        onOpenChange={setDialogOpen}
        open={dialogOpen}
      />
    </WorkPage>
  );
}
