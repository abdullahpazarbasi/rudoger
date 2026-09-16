import { useMutation, useQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router";
import { toast } from "sonner";
import { createOrderTransition, getOrder, getOrderTransition } from "../../api/client/rudoger-api";
import type { OrderTransitionTarget } from "../../api/contracts";
import { queryKeys } from "../../api/query-keys";
import { queryClient } from "../../app/providers/query-client";
import { Button } from "../../shared/components/button";
import { Dialog } from "../../shared/components/dialog";
import { ErrorPanel } from "../../shared/components/error-panel";
import { StatusBadge } from "../../shared/components/status-badge";
import {
  formatMoney,
  formatNumber,
  orderStatusLabels,
  orderStatusTones,
} from "../../shared/formatting/formatters";
import { useAuth } from "../authn/use-auth";

export function OrderDetailPage() {
  const auth = useAuth();
  const { orderId = "" } = useParams();
  const [target, setTarget] = useState<OrderTransitionTarget | null>(null);
  const [transitionId, setTransitionId] = useState<string | null>(null);
  const orderQuery = useQuery({
    queryKey: queryKeys.order(orderId),
    queryFn: ({ signal }) => getOrder(orderId, signal),
    enabled: auth.status === "authenticated" && orderId !== "",
  });
  const activeTransitionId = transitionId ?? orderQuery.data?.pendingTransitionId ?? null;
  const transitionQuery = useQuery({
    queryKey: queryKeys.orderTransition(orderId, activeTransitionId ?? ""),
    queryFn: ({ signal }) =>
      activeTransitionId === null
        ? Promise.reject(new Error("Transition id is required."))
        : getOrderTransition(orderId, activeTransitionId, signal),
    enabled: activeTransitionId !== null && auth.status === "authenticated",
    refetchInterval: (query) => (query.state.data?.status === "Pending" ? 750 : false),
  });
  const mutation = useMutation({
    mutationFn: (next: OrderTransitionTarget) => createOrderTransition(orderId, next),
    onSuccess(transition) {
      setTransitionId(transition.id);
      setTarget(null);
    },
  });
  useEffect(() => {
    if (transitionQuery.data?.status !== "Succeeded") return;
    void Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.order(orderId) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.orders(1) }),
      queryClient.invalidateQueries({ queryKey: ["stock-items"] }),
    ]);
    toast.success("Sipariş durumu güncellendi.");
  }, [orderId, transitionQuery.data]);
  if (orderQuery.isPending && auth.status === "authenticated") return <p>Sipariş yükleniyor…</p>;
  if (orderQuery.isError) return <ErrorPanel error={orderQuery.error} />;
  if (orderQuery.data === undefined)
    return <p>Oturum yenilenene kadar sipariş verisi gösterilemiyor.</p>;
  const order = orderQuery.data;
  const pending = activeTransitionId !== null && transitionQuery.data?.status !== "Succeeded";
  return (
    <section>
      <Link className="text-sm font-semibold text-[var(--accent)]" to="/siparisler">
        ← Siparişlere dön
      </Link>
      <div className="mt-4 flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold">{order.orderNumber}</h1>
            <StatusBadge tone={orderStatusTones[order.status]}>
              {orderStatusLabels[order.status]}
            </StatusBadge>
          </div>
          <p className="mt-1 font-mono text-sm text-[var(--muted)]">{order.id}</p>
        </div>
        {order.status === "Placed" && (
          <div className="flex gap-3">
            <Button
              disabled={auth.status === "expired" || pending}
              onClick={() => setTarget("Shipped")}
            >
              Gönderildi Yap
            </Button>
            <Button
              disabled={auth.status === "expired" || pending}
              onClick={() => setTarget("Cancelled")}
              variant="danger"
            >
              İptal Et
            </Button>
          </div>
        )}
      </div>
      {(mutation.isError || transitionQuery.isError) && (
        <div className="mt-5">
          <ErrorPanel error={mutation.error ?? transitionQuery.error} />
        </div>
      )}
      {pending && (
        <p className="panel mt-5 p-4 text-[var(--muted)]" role="status">
          {order.pendingTransitionTarget === "Cancelled" ? "İptal" : "Gönderim"} geçişi stok
          işlemleriyle birlikte tamamlanıyor…
        </p>
      )}
      <section className="mt-6">
        <h2 className="text-lg font-bold">Sipariş satırları</h2>
        <div className="panel mt-3 overflow-x-auto">
          <table className="data-table">
            <thead>
              <tr>
                <th>No.</th>
                <th>Ürün ID</th>
                <th>UoM</th>
                <th>Miktar</th>
                <th>Birim fiyat</th>
                <th>Toplam</th>
              </tr>
            </thead>
            <tbody>
              {order.lines.map((line) => (
                <tr key={line.id}>
                  <td>{line.num}</td>
                  <td>
                    <Link
                      className="font-mono text-sm text-[var(--accent)]"
                      to={`/urunler/${line.productId}`}
                    >
                      {line.productId}
                    </Link>
                  </td>
                  <td>{line.uomCode}</td>
                  <td>{formatNumber(line.quantity)}</td>
                  <td>{formatMoney(line.unitPriceAmount, line.unitPriceCurrencyCode)}</td>
                  <td className="font-semibold">
                    {formatMoney(line.quantity * line.unitPriceAmount, line.unitPriceCurrencyCode)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
      <Dialog
        footer={
          <>
            <Button onClick={() => setTarget(null)} variant="secondary">
              Vazgeç
            </Button>
            <Button
              disabled={auth.status === "expired" || mutation.isPending}
              onClick={() => {
                if (target !== null) mutation.mutate(target);
              }}
              variant={target === "Cancelled" ? "danger" : "primary"}
            >
              {mutation.isPending ? "Başlatılıyor…" : "Onayla"}
            </Button>
          </>
        }
        onOpenChange={(open) => {
          if (!open) setTarget(null);
        }}
        open={target !== null}
        title={
          target === "Cancelled" ? "Sipariş iptal edilsin mi?" : "Sipariş gönderildi yapılsın mı?"
        }
      >
        <p className="text-[var(--muted)]">
          {target === "Cancelled"
            ? "Ayrılmış stok serbest bırakılacak."
            : "Ayrılmış stok eldeki stoktan düşülerek kesinleştirilecek."}
        </p>
      </Dialog>
    </section>
  );
}
