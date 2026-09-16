import { useMutation } from "@tanstack/react-query";
import { useEffect } from "react";
import { useFieldArray, useForm, useWatch } from "react-hook-form";
import { toast } from "sonner";
import { createProduct } from "../../api/client/rudoger-api";
import type { CreateProductInput, Product } from "../../api/contracts";
import { queryKeys } from "../../api/query-keys";
import { queryClient } from "../../app/providers/query-client";
import { Button } from "../../shared/components/button";
import { Dialog } from "../../shared/components/dialog";
import { ErrorPanel } from "../../shared/components/error-panel";
import { TextField } from "../../shared/components/form-field";
import { applyServerFieldErrors } from "../../shared/forms/server-field-errors";
import { useAuth } from "../authn/use-auth";
import { PackagingFields } from "./packaging-fields";
import { emptyPackaging, productDraftSchema, type ProductDraft } from "./product-validation";

const defaults: ProductDraft = {
  sku: "",
  name: "",
  baseUomCode: "",
  basePriceAmount: "",
  basePriceCurrencyCode: "TRY",
  packagings: [emptyPackaging(0)],
};

export function CreateProductDialog({
  open,
  onOpenChange,
  onCreated,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: (product: Product) => void;
}) {
  const auth = useAuth();
  const form = useForm<ProductDraft>({ defaultValues: defaults });
  const fields = useFieldArray({ control: form.control, name: "packagings" });
  const baseUomCode = useWatch({ control: form.control, name: "baseUomCode" });
  useEffect(() => {
    form.setValue("packagings.0.uomCode", baseUomCode, { shouldValidate: false });
    form.setValue("packagings.0.level", "0", { shouldValidate: false });
    form.setValue("packagings.0.conversionFactor", "1", { shouldValidate: false });
  }, [baseUomCode, form]);

  const mutation = useMutation({
    mutationFn: (input: CreateProductInput) => createProduct(input),
    onError(error) {
      applyServerFieldErrors(error, form.setError);
    },
    async onSuccess(product) {
      await queryClient.invalidateQueries({ queryKey: queryKeys.products() });
      toast.success("Ürün oluşturuldu.");
      form.reset(defaults);
      onOpenChange(false);
      onCreated(product);
    },
  });

  const submit = (draft: ProductDraft): void => {
    form.clearErrors();
    const parsed = productDraftSchema.safeParse(draft);
    if (!parsed.success) {
      for (const issue of parsed.error.issues) {
        form.setError(issue.path.join(".") as never, { message: issue.message });
      }
      return;
    }
    mutation.mutate(parsed.data);
  };

  return (
    <Dialog
      description="Ürün ve zorunlu temel packaging aynı işlemde oluşturulur."
      onOpenChange={onOpenChange}
      open={open}
      title="Yeni ürün"
    >
      {mutation.isError && (
        <div className="mb-5">
          <ErrorPanel compact error={mutation.error} />
        </div>
      )}
      <form
        className="grid gap-5"
        noValidate
        onSubmit={(event) => void form.handleSubmit(submit)(event)}
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <TextField
            error={form.formState.errors.sku?.message}
            label="SKU"
            maxLength={64}
            {...form.register("sku")}
          />
          <TextField
            error={form.formState.errors.name?.message}
            label="Ürün adı"
            maxLength={200}
            {...form.register("name")}
          />
          <TextField
            error={form.formState.errors.baseUomCode?.message}
            label="Temel UoM"
            maxLength={16}
            {...form.register("baseUomCode")}
          />
          <TextField
            error={form.formState.errors.basePriceAmount?.message}
            label="Temel fiyat"
            min="0"
            step="any"
            type="number"
            {...form.register("basePriceAmount")}
          />
          <TextField
            error={form.formState.errors.basePriceCurrencyCode?.message}
            label="Para birimi"
            maxLength={3}
            {...form.register("basePriceCurrencyCode")}
          />
        </div>
        <div className="grid gap-4">
          {fields.fields.map((field, index) => (
            <PackagingFields
              errors={form.formState.errors}
              index={index}
              isBase={index === 0}
              key={field.id}
              onRemove={() => fields.remove(index)}
              register={form.register}
            />
          ))}
          <Button
            onClick={() => fields.append(emptyPackaging(fields.fields.length))}
            type="button"
            variant="secondary"
          >
            Packaging ekle
          </Button>
        </div>
        <div className="flex justify-end">
          <Button disabled={auth.status === "expired" || mutation.isPending} type="submit">
            {mutation.isPending ? "Oluşturuluyor…" : "Ürünü Oluştur"}
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
