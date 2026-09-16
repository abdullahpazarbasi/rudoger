import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { Navigate, useNavigate, useSearchParams } from "react-router";
import { z } from "zod";
import { exchangeToken } from "../../api/client/rudoger-api";
import { Button } from "../../shared/components/button";
import { ErrorPanel } from "../../shared/components/error-panel";
import { TextField } from "../../shared/components/form-field";
import { applyServerFieldErrors } from "../../shared/forms/server-field-errors";
import { ThemeToggle } from "../../shared/theme/theme-toggle";
import { authStore } from "./auth-store";
import { useAuth } from "./use-auth";

const schema = z.object({
  username: z
    .string()
    .trim()
    .min(1, "Kullanıcı adı zorunludur.")
    .max(64, "En fazla 64 karakter girin."),
  password: z.string().min(1, "Parola zorunludur."),
});

type LoginForm = z.infer<typeof schema>;

function safeReturnTo(value: string | null): string {
  return value !== null && value.startsWith("/") && !value.startsWith("//") ? value : "/urunler";
}

export function LoginPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const form = useForm<LoginForm>({
    resolver: zodResolver(schema),
    defaultValues: { username: "", password: "" },
  });
  const mutation = useMutation({
    mutationFn: ({ username, password }: LoginForm) => exchangeToken(username, password),
    onError(error) {
      applyServerFieldErrors(error, form.setError);
    },
    onSuccess(result) {
      authStore.setSession(result);
      void navigate(safeReturnTo(searchParams.get("returnTo")), { replace: true });
    },
  });

  if (auth.status === "authenticated") {
    return <Navigate replace to={safeReturnTo(searchParams.get("returnTo"))} />;
  }

  return (
    <main className="grid min-h-screen grid-rows-[auto_1fr]">
      <div className="flex justify-end p-4 sm:p-6">
        <ThemeToggle />
      </div>
      <div className="mx-auto flex w-full max-w-md items-start px-5 pt-[8vh] pb-12 sm:px-6">
        <section className="panel w-full p-6 sm:p-8" aria-labelledby="login-title">
          <p className="text-sm font-semibold tracking-[0.16em] text-[var(--accent)] uppercase">
            Rudoger
          </p>
          <h1 className="mt-2 text-2xl font-bold" id="login-title">
            Giriş yap
          </h1>
          <p className="mt-2 text-[var(--muted)]">Ürün, stok ve sipariş işlemlerine erişin.</p>

          {mutation.isError && (
            <div className="mt-5">
              <ErrorPanel compact error={mutation.error} />
            </div>
          )}

          <form
            className="mt-6 grid gap-5"
            noValidate
            onSubmit={(event) => void form.handleSubmit((values) => mutation.mutate(values))(event)}
          >
            <TextField
              autoComplete="username"
              error={form.formState.errors.username?.message}
              label="Kullanıcı adı"
              {...form.register("username")}
            />
            <TextField
              autoComplete="current-password"
              error={form.formState.errors.password?.message}
              label="Parola"
              type="password"
              {...form.register("password")}
            />
            <Button className="w-full" disabled={mutation.isPending} type="submit">
              {mutation.isPending ? "Giriş yapılıyor…" : "Giriş Yap"}
            </Button>
          </form>
        </section>
      </div>
    </main>
  );
}
