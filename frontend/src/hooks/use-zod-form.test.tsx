import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { z } from "zod";
import { useZodForm } from "./use-zod-form.ts";

const schema = z.object({
  title: z.string().trim().min(1, "Title is required."),
  estimate: z.number().int().min(0, "Estimate cannot be negative."),
});

describe("useZodForm", () => {
  it("reports schema errors and does not submit invalid values", async () => {
    const onValid = vi.fn();
    const { result } = renderHook(() => {
      const form = useZodForm(schema, { defaultValues: { title: "  ", estimate: -1 } });
      // formState is a subscription proxy; reading errors during render subscribes to them like a form component does.
      return { form, errors: form.formState.errors };
    });

    await act(() => result.current.form.handleSubmit(onValid)());

    expect(onValid).not.toHaveBeenCalled();
    expect(result.current.errors.title?.message).toBe("Title is required.");
    expect(result.current.errors.estimate?.message).toBe("Estimate cannot be negative.");
  });

  it("submits parsed values when the schema passes", async () => {
    const onValid = vi.fn();
    const { result } = renderHook(() => useZodForm(schema, { defaultValues: { title: "  Ship it ", estimate: 3 } }));

    await act(() => result.current.handleSubmit(onValid)());

    expect(onValid).toHaveBeenCalledOnce();
    expect(onValid.mock.calls[0][0]).toEqual({ title: "Ship it", estimate: 3 });
  });
});
