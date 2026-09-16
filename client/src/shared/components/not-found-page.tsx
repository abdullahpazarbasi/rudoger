import { Link } from "react-router";

export function NotFoundPage() {
  return (
    <main className="mx-auto grid min-h-screen max-w-xl place-content-center p-6 text-center">
      <h1 className="text-3xl font-bold">Sayfa bulunamadı</h1>
      <p className="mt-2 text-[var(--muted)]">İstenen adres Rudoger istemcisinde bulunmuyor.</p>
      <Link className="button button-primary mt-5" to="/">
        Ana sayfaya dön
      </Link>
    </main>
  );
}
