import { useEffect } from "react";
import { useLocation, useNavigate } from "react-router";
import { toast } from "sonner";
import { authStore } from "./auth-store";
import { useAuth } from "./use-auth";

const TOAST_ID = "session-expired";

export function SessionExpiryNotice() {
  const auth = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  useEffect(() => {
    if (auth.status !== "expired") {
      toast.dismiss(TOAST_ID);
      return;
    }
    toast.error("Oturumunuzun süresi doldu.", {
      id: TOAST_ID,
      duration: Infinity,
      dismissible: false,
      action: {
        label: "Yeniden Giriş Yap",
        onClick: () => {
          const returnTo = `${location.pathname}${location.search}${location.hash}`;
          authStore.clear();
          void navigate(`/giris?returnTo=${encodeURIComponent(returnTo)}`);
        },
      },
    });
  }, [auth.status, location.hash, location.pathname, location.search, navigate]);

  return null;
}
