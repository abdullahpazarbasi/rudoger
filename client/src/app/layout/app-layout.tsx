import { NavLink, Outlet, useLocation, useNavigate } from "react-router";
import { Button } from "../../shared/components/button";
import { ThemeToggle } from "../../shared/theme/theme-toggle";
import { authStore } from "../../features/authn/auth-store";
import { SessionExpiryNotice } from "../../features/authn/session-expiry-notice";
import { useAuth } from "../../features/authn/use-auth";

const navigation = [
  { to: "/urunler", label: "Ürünler" },
  { to: "/stok", label: "Stok" },
  { to: "/siparisler", label: "Siparişler" },
];

export function AppLayout() {
  const auth = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const username = auth.status === "anonymous" ? null : auth.session.username;

  return (
    <div className="min-h-screen">
      <SessionExpiryNotice />
      <header className="sticky top-0 z-20 border-b bg-[color-mix(in_srgb,var(--surface)_94%,transparent)] backdrop-blur">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center gap-x-6 gap-y-3 px-4 py-3 sm:px-6">
          <NavLink className="text-lg font-extrabold tracking-tight" to="/urunler">
            Rudoger
          </NavLink>
          <nav
            aria-label="Ana navigasyon"
            className="order-3 flex w-full gap-1 sm:order-none sm:w-auto"
          >
            {navigation.map((item) => (
              <NavLink
                className={({ isActive }) =>
                  `rounded-md px-3 py-2 text-sm font-semibold ${isActive ? "bg-[var(--accent-soft)] text-[var(--accent)]" : "text-[var(--muted)] hover:bg-[var(--surface-subtle)] hover:text-[var(--text)]"}`
                }
                key={item.to}
                to={item.to}
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
          <div className="ml-auto flex items-center gap-3">
            <div className="hidden text-right text-sm md:block">
              <p className="font-semibold">{username ?? "Kullanıcı"}</p>
              <p
                className={
                  auth.status === "expired" ? "text-[var(--danger)]" : "text-[var(--muted)]"
                }
              >
                {auth.status === "expired" ? "Oturum süresi doldu" : "Oturum açık"}
              </p>
            </div>
            <ThemeToggle />
            <Button
              onClick={() => {
                const returnTo = `${location.pathname}${location.search}`;
                authStore.clear();
                void navigate(`/giris?returnTo=${encodeURIComponent(returnTo)}`);
              }}
              type="button"
              variant="secondary"
            >
              Çıkış
            </Button>
          </div>
        </div>
      </header>
      <main className="mx-auto w-full max-w-7xl px-4 py-6 sm:px-6 sm:py-8">
        <Outlet />
      </main>
    </div>
  );
}
