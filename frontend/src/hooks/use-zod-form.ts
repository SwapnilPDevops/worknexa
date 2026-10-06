import { zodResolver } from "@hookform/resolvers/zod";
import { useForm, type FieldValues, type UseFormProps, type UseFormReturn } from "react-hook-form";
import type { ZodType } from "zod";

export function useZodForm<T extends FieldValues>(
  schema: ZodType<T, T>,
  props?: Omit<UseFormProps<T, unknown, T>, "resolver">,
): UseFormReturn<T, unknown, T> {
  return useForm<T, unknown, T>({
    ...props,
    resolver: zodResolver(schema),
  });
}
