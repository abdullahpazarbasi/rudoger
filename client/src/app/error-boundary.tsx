import { Component, type ReactNode } from "react";
import { ErrorPanel } from "../shared/components/error-panel";

interface Props {
  children: ReactNode;
}

interface State {
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  public state: State = { error: null };

  public static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  public render(): ReactNode {
    if (this.state.error !== null) {
      return (
        <main className="mx-auto grid min-h-screen max-w-2xl place-content-center p-6">
          <ErrorPanel error={this.state.error} />
        </main>
      );
    }
    return this.props.children;
  }
}
