export function createIdempotencyKey(scope: string): string {
  return `${scope}-${crypto.randomUUID()}`;
}

export class IdempotencyIntent {
  private signature: string | null = null;
  private key: string | null = null;

  public constructor(private readonly scope: string) {}

  public keyFor(payload: unknown): string {
    const signature = JSON.stringify(payload);
    if (signature !== this.signature || this.key === null) {
      this.signature = signature;
      this.key = createIdempotencyKey(this.scope);
    }
    return this.key;
  }

  public reset(): void {
    this.signature = null;
    this.key = null;
  }
}
