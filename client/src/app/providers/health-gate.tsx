import * as Dialog from "@radix-ui/react-dialog";
import { useQuery } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { getHealth } from "../../api/client/rudoger-api";
import { normalizeError } from "../../api/errors/client-error";
import { queryKeys } from "../../api/query-keys";
import { Button } from "../../shared/components/button";
import { ErrorPanel } from "../../shared/components/error-panel";

export function HealthGate({ children }: { children: ReactNode }) {
  const query = useQuery({
    queryKey: queryKeys.health,
    queryFn: async ({ signal }) => {
      try {
        return { health: await getHealth(signal), error: null };
      } catch (error) {
        return { health: null, error: normalizeError(error) };
      }
    },
    retry: false,
    refetchOnWindowFocus: false,
    staleTime: 30_000,
  });
  const health = query.data?.health ?? null;
  const error = query.data?.error ?? null;
  const open = query.isPending || error !== null || health?.healthy !== true;

  return (
    <>
      {children}
      <Dialog.Root open={open}>
        <Dialog.Portal>
          <Dialog.Overlay className="fixed inset-0 z-50 bg-slate-950/60 backdrop-blur-[2px]" />
          <Dialog.Content
            aria-describedby="health-dialog-description"
            className="fixed top-1/2 left-1/2 z-50 w-[min(32rem,calc(100%-2rem))] -translate-x-1/2 -translate-y-1/2 rounded-xl border bg-[var(--surface)] p-6 shadow-2xl"
            onEscapeKeyDown={(event) => event.preventDefault()}
            onPointerDownOutside={(event) => event.preventDefault()}
          >
            <Dialog.Title className="text-xl font-bold">API bağlantısı kullanılamıyor</Dialog.Title>
            <Dialog.Description className="mt-2 text-[var(--muted)]" id="health-dialog-description">
              {query.isPending
                ? "API sağlık durumu denetleniyor."
                : "Rudoger API sağlıklı yanıt verene kadar işlemlere devam edilemez."}
            </Dialog.Description>
            {error !== null && (
              <div className="mt-4">
                <ErrorPanel compact error={error} />
              </div>
            )}
            {health?.healthy === false && (
              <div className="mt-4 rounded-lg bg-[var(--surface-subtle)] p-3 text-sm">
                Bildirilen durum: <strong>{health.status}</strong>
              </div>
            )}
            {!query.isPending && (
              <Button
                className="mt-5 w-full"
                disabled={query.isFetching}
                onClick={() => void query.refetch()}
                type="button"
              >
                {query.isFetching ? "Erişim deneniyor…" : "Yeniden Erişmeyi Dene"}
              </Button>
            )}
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>
    </>
  );
}
