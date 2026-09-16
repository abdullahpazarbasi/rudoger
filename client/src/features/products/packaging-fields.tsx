import type { FieldErrors, UseFormRegister } from "react-hook-form";
import { Button } from "../../shared/components/button";
import { TextField } from "../../shared/components/form-field";
import type { ProductDraft } from "./product-validation";

interface PackagingFieldsProps {
  index: number;
  register: UseFormRegister<ProductDraft>;
  errors: FieldErrors<ProductDraft>;
  isBase: boolean;
  onRemove: () => void;
}

export function PackagingFields({
  index,
  register,
  errors,
  isBase,
  onRemove,
}: PackagingFieldsProps) {
  const fieldErrors = errors.packagings?.[index];
  return (
    <fieldset className="rounded-lg border p-4">
      <div className="mb-4 flex items-center justify-between gap-3">
        <legend className="font-semibold">
          {isBase ? "Temel packaging (level 0)" : `Packaging ${index + 1}`}
        </legend>
        <Button disabled={isBase} onClick={onRemove} type="button" variant="secondary">
          Satırı kaldır
        </Button>
      </div>
      <div className="grid gap-4 sm:grid-cols-3">
        <TextField
          disabled={isBase}
          error={fieldErrors?.level?.message}
          label="Level"
          min="0"
          step="1"
          type="number"
          {...register(`packagings.${index}.level`)}
        />
        <TextField
          disabled={isBase}
          error={fieldErrors?.uomCode?.message}
          label="UoM kodu"
          maxLength={16}
          {...register(`packagings.${index}.uomCode`)}
        />
        <TextField
          disabled={isBase}
          error={fieldErrors?.conversionFactor?.message}
          label="Dönüşüm katsayısı"
          min="0"
          step="any"
          type="number"
          {...register(`packagings.${index}.conversionFactor`)}
        />
        <TextField
          error={fieldErrors?.barcode?.message}
          label="Barkod"
          maxLength={64}
          {...register(`packagings.${index}.barcode`)}
        />
        <TextField
          error={fieldErrors?.weightInKg?.message}
          label="Ağırlık (kg)"
          min="0"
          step="any"
          type="number"
          {...register(`packagings.${index}.weightInKg`)}
        />
        <TextField
          error={fieldErrors?.lengthInMm?.message}
          label="Uzunluk (mm)"
          min="0"
          step="any"
          type="number"
          {...register(`packagings.${index}.lengthInMm`)}
        />
        <TextField
          error={fieldErrors?.widthInMm?.message}
          label="Genişlik (mm)"
          min="0"
          step="any"
          type="number"
          {...register(`packagings.${index}.widthInMm`)}
        />
        <TextField
          error={fieldErrors?.heightInMm?.message}
          label="Yükseklik (mm)"
          min="0"
          step="any"
          type="number"
          {...register(`packagings.${index}.heightInMm`)}
        />
      </div>
    </fieldset>
  );
}
