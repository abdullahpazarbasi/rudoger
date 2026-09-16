import * as DialogPrimitive from "@radix-ui/react-dialog";
import type { ReactNode } from "react";

interface DialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description?: string;
  children: ReactNode;
  footer?: ReactNode;
}

export function Dialog({ open, onOpenChange, title, description, children, footer }: DialogProps) {
  return (
    <DialogPrimitive.Root onOpenChange={onOpenChange} open={open}>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="fixed inset-0 z-40 bg-slate-950/60 backdrop-blur-[2px]" />
        <DialogPrimitive.Content className="fixed top-1/2 left-1/2 z-50 max-h-[min(52rem,calc(100vh-2rem))] w-[min(46rem,calc(100%-2rem))] -translate-x-1/2 -translate-y-1/2 overflow-y-auto rounded-xl border bg-[var(--surface)] p-5 shadow-2xl sm:p-6">
          <div className="flex items-start justify-between gap-4">
            <div>
              <DialogPrimitive.Title className="text-xl font-bold">{title}</DialogPrimitive.Title>
              {description !== undefined && (
                <DialogPrimitive.Description className="mt-1 text-sm text-[var(--muted)]">
                  {description}
                </DialogPrimitive.Description>
              )}
            </div>
            <DialogPrimitive.Close
              aria-label="Pencereyi kapat"
              className="button button-secondary min-h-9 px-3"
              type="button"
            >
              Kapat
            </DialogPrimitive.Close>
          </div>
          <div className="mt-5">{children}</div>
          {footer !== undefined && (
            <div className="mt-6 flex flex-wrap justify-end gap-3">{footer}</div>
          )}
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  );
}
