import { Button } from "./button";

interface PaginationProps {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}

export function Pagination({ pageNumber, pageSize, totalCount, onPageChange }: PaginationProps) {
  const pageCount = Math.max(1, Math.ceil(totalCount / pageSize));
  if (totalCount <= pageSize && pageNumber === 1) {
    return null;
  }
  return (
    <nav aria-label="Sayfalama" className="mt-4 flex items-center justify-between gap-4">
      <Button
        disabled={pageNumber <= 1}
        onClick={() => onPageChange(pageNumber - 1)}
        type="button"
        variant="secondary"
      >
        Önceki
      </Button>
      <p className="text-sm text-[var(--muted)]">
        Sayfa <strong className="text-[var(--text)]">{pageNumber}</strong> / {pageCount} ·{" "}
        {totalCount} kayıt
      </p>
      <Button
        disabled={pageNumber >= pageCount}
        onClick={() => onPageChange(pageNumber + 1)}
        type="button"
        variant="secondary"
      >
        Sonraki
      </Button>
    </nav>
  );
}
