export interface ReplaceOperation {
  op: "replace";
  path: string;
  value: unknown;
}

export function changedFields<T extends Record<string, unknown>>(
  before: T,
  after: T,
  paths: Partial<Record<keyof T, string>>,
): ReplaceOperation[] {
  return (Object.keys(paths) as Array<keyof T>).flatMap((key) => {
    const path = paths[key];
    if (path === undefined || Object.is(before[key], after[key])) {
      return [];
    }
    return [{ op: "replace" as const, path, value: after[key] }];
  });
}
