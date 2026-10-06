import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { Dialog } from "./dialog.tsx";
import { Field, Input } from "./field.tsx";
import { EmptyState, ErrorState, LoadingState } from "./states.tsx";

describe("state components", () => {
  it("LoadingState is announced as a busy status", () => {
    render(<LoadingState label="Loading tasks" />);

    const status = screen.getByRole("status");
    expect(status).toHaveAttribute("aria-busy", "true");
    expect(status).toHaveTextContent("Loading tasks");
  });

  it("ErrorState is an alert with an optional retry", async () => {
    const onRetry = vi.fn();
    const { rerender } = render(<ErrorState description="The server is unavailable." />);

    expect(screen.getByRole("alert")).toHaveTextContent("Something went wrong");
    expect(screen.queryByRole("button", { name: "Try again" })).not.toBeInTheDocument();

    rerender(<ErrorState title="Tasks failed" description="The server is unavailable." onRetry={onRetry} />);
    await userEvent.click(screen.getByRole("button", { name: "Try again" }));

    expect(screen.getByRole("alert")).toHaveTextContent("Tasks failed");
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it("EmptyState renders its action", () => {
    render(<EmptyState title="No tasks" description="Create your first task." action={<button type="button">New task</button>} />);

    expect(screen.getByRole("heading", { name: "No tasks" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "New task" })).toBeInTheDocument();
  });
});

describe("Field", () => {
  it("links the label, hint and error to the control", () => {
    render(
      <Field label="Title" hint="Keep it short" error="Title is required.">
        {({ id, describedBy }) => <Input id={id} aria-describedby={describedBy} />}
      </Field>,
    );

    const input = screen.getByLabelText("Title");
    expect(input).toHaveAccessibleDescription("Keep it short Title is required.");
  });
});

describe("Dialog", () => {
  function Harness({ onClose = () => {} }: { onClose?: () => void }) {
    const [open, setOpen] = useState(false);
    return (
      <>
        <button type="button" onClick={() => setOpen(true)}>Open</button>
        <Dialog open={open} title="New task" description="Add a task" onClose={() => { onClose(); setOpen(false); }}>
          <p>Dialog body</p>
        </Dialog>
      </>
    );
  }

  it("opens as a labelled modal and closes from the close button", async () => {
    const onClose = vi.fn();
    const user = userEvent.setup();
    render(<Harness onClose={onClose} />);

    await user.click(screen.getByRole("button", { name: "Open" }));
    const dialog = screen.getByRole("dialog", { name: "New task" });
    expect(dialog).toHaveAttribute("open");
    expect(dialog).toHaveTextContent("Dialog body");

    await user.click(screen.getByRole("button", { name: "Close dialog" }));

    expect(onClose).toHaveBeenCalled();
    expect(dialog).not.toHaveAttribute("open");
  });
});
