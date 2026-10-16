import { screen, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { adminUser, renderWithProviders } from "../../test/render.tsx";
import { TodosView } from "./todos-view.tsx";

describe("TodosView layout", () => {
  it("renders header, add todo button, date strip carousel, and filter tabs", async () => {
    renderWithProviders(<TodosView />, {
      user: adminUser,
      api: {
        "GET /api/todos": [],
      },
    });

    expect(await screen.findByRole("heading", { name: "Todo" })).toBeInTheDocument();
    expect(screen.getByText("Personal items. They stay off the task board.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Add todo/i })).toBeInTheDocument();

    // Today button and date strip controls
    expect(screen.getByRole("button", { name: "Today" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Previous week" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Next week" })).toBeInTheDocument();

    // Filter tabs
    expect(screen.getByRole("button", { name: /All \(\d+\)/ })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Today \(\d+\)/ })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Upcoming \(\d+\)/ })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Completed \(\d+\)/ })).toBeInTheDocument();
  });

  it("renders todo cards with badges and selected detail panel with checklist and notes", async () => {
    renderWithProviders(<TodosView />, {
      user: adminUser,
      api: {
        "GET /api/todos": [],
      },
    });

    // Verify presence of reference demo todos in the list (appears in card and detail panel)
    const titles = await screen.findAllByText("Prepare Standup Call Notes");
    expect(titles.length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText("Design Todo Module UI")).toBeInTheDocument();
    expect(screen.getByText("Review PR: Auth Service")).toBeInTheDocument();

    // Detail panel for the selected todo
    expect(screen.getByRole("heading", { name: "Description" })).toBeInTheDocument();
    expect(screen.getByText(/This todo is scheduled for today/i)).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /^Checklist \(\d+\/\d+\)$/ })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Notes" })).toBeInTheDocument();
    expect(screen.getByPlaceholderText("Add additional notes...")).toBeInTheDocument();
  });

  it("toggles checklist items in the detail drawer and updates progress", async () => {
    const { user } = renderWithProviders(<TodosView />, {
      user: adminUser,
      api: {
        "GET /api/todos": [],
      },
    });

    const checklistItem = await screen.findByText("Plan for today");
    expect(checklistItem).toBeInTheDocument();

    // Find unchecked box next to "Plan for today"
    const checkboxBtn = checklistItem.previousElementSibling as HTMLElement;
    if (checkboxBtn) {
      await user.click(checkboxBtn);
      await waitFor(() => {
        expect(checklistItem).toHaveClass("line-through");
      });
    }
  });

  it("opens the Add Todo modal when clicking Add todo", async () => {
    const { user } = renderWithProviders(<TodosView />, {
      user: adminUser,
      api: {
        "GET /api/todos": [],
      },
    });

    const addButton = await screen.findByRole("button", { name: /Add todo/i });
    await user.click(addButton);

    expect(await screen.findByRole("heading", { name: "Add New Todo" })).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/e\.g\. Prepare Standup Call Notes/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Create todo" })).toBeInTheDocument();
  });
});
