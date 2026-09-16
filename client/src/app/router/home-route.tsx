import { Navigate } from "react-router";
import { useAuth } from "../../features/authn/use-auth";

export function HomeRoute() {
  const auth = useAuth();
  return <Navigate replace to={auth.status === "anonymous" ? "/giris" : "/urunler"} />;
}
