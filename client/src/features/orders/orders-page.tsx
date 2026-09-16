import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Link, useSearchParams } from "react-router";
import { listOrders } from "../../api/client/rudoger-api";
import { queryKeys } from "../../api/query-keys";
import { Button } from "../../shared/components/button";
import { ErrorPanel } from "../../shared/components/error-panel";
import { Pagination } from "../../shared/components/pagination";
import { StatusBadge } from "../../shared/components/status-badge";
import { WorkPage } from "../../shared/components/work-page";
import { orderStatusLabels, orderStatusTones } from "../../shared/formatting/formatters";
import { useAuth } from "../authn/use-auth";
import { CreateOrderDialog } from "./create-order-dialog";

export function OrdersPage() {
  const auth = useAuth();
  const [params, setParams] = useSearchParams();
  const [dialogOpen, setDialogOpen] = useState(false);
  const pageNumber = Math.max(1, Number(params.get("sayfa")) || 1);
  const query = useQuery({
    queryKey: queryKeys.orders(pageNumber),
    queryFn: ({ signal }) => listOrders(pageNumber, signal),
    enabled: auth.status === "authenticated",
  });
  return (
    <WorkPage
      action={
        <Button disabled={auth.status === "expired"} onClick={() => setDialogOpen(true)}>
          Yeni sipariş
        </Button>
      }
      description="Siparişleri oluşturun ve durumlarını ilerletin."
      title="Siparişler"
    >
      {query.isPending && auth.status === "authenticated" && <p>Siparişler yükleniyor…</p>}
      {query.isError && <ErrorPanel error={query.error} />}
      {query.data !== undefined && (
        <>
          <div className="panel overflow-x-auto">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Sipariş no.</th>
                  <th>Durum</th>
                  <th>Satır</th>
                  <th>Kullanıcı ID</th>
                  <th>
                    <span className="sr-only">İşlem</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {query.data.items.map((order) => (
                  <tr key={order.id}>
                    <td className="font-semibold">{order.orderNumber}</td>
                    <td>
                      <StatusBadge tone={orderStatusTones[order.status]}>
                        {orderStatusLabels[order.status]}
                      </StatusBadge>
                    </td>
                    <td>{order.lines.length}</td>
                    <td className="font-mono text-sm">{order.userId}</td>
                    <td>
                      <Link
                        className="font-semibold text-[var(--accent)]"
                        to={`/siparisler/${order.id}`}
                      >
                        Görüntüle
                      </Link>
                    </td>
                  </tr>
                ))}
                {query.data.items.length === 0 && (
                  <tr>
                    <td className="text-center text-[var(--muted)]" colSpan={5}>
                      Sipariş bulunamadı.
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
            pageNumber={query.data.pageNumber}
            pageSize={query.data.pageSize}
            totalCount={query.data.totalCount}
          />
        </>
      )}
      <CreateOrderDialog onOpenChange={setDialogOpen} open={dialogOpen} />
    </WorkPage>
  );
}
