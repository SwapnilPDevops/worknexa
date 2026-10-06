import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { MarkdownEditor, renderMarkdown } from "./markdown-editor.tsx";

function toDom(source: string) {
  const container = document.createElement("div");
  container.innerHTML = renderMarkdown(source);
  return container;
}

const people = [{ id: "44444444-4444-4444-4444-444444444444", name: "Priya Patel" }];

describe("renderMarkdown", () => {
  it("renders bold, italic and inline code", () => {
    const dom = toDom("Ship **the release** and *celebrate* with `npm run build`");

    expect(dom.querySelector("strong")).toHaveTextContent("the release");
    expect(dom.querySelector("em")).toHaveTextContent("celebrate");
    expect(dom.querySelector("code")).toHaveTextContent("npm run build");
    expect(dom.querySelector("p")).toHaveTextContent("Ship the release and celebrate with npm run build");
  });

  it("keeps heading markers as plain paragraph text because headings are not supported", () => {
    const dom = toDom("# Weekly sync");

    expect(dom.querySelector("h1, h2, h3, h4, h5, h6")).toBeNull();
    expect(dom.querySelector("p")).toHaveTextContent("# Weekly sync");
  });

  it("renders fenced code blocks verbatim without applying inline formatting", () => {
    const dom = toDom("Before\n```\nconst total = a < b && **c**;\n```\nAfter");

    const code = dom.querySelector("pre > code");
    expect(code?.textContent).toBe("const total = a < b && **c**;");
    expect(dom.querySelector("pre strong")).toBeNull();
    expect(dom.querySelectorAll("p")).toHaveLength(2);
  });

  it("renders checklists as disabled checkboxes reflecting their state", () => {
    const dom = toDom("- [x] Book the room\n- [ ] Send the agenda\n* [X] Invite guests");

    const boxes = [...dom.querySelectorAll<HTMLInputElement>("input[type=checkbox]")];
    expect(boxes.map((box) => box.checked)).toEqual([true, false, true]);
    expect(boxes.every((box) => box.disabled)).toBe(true);
    expect(dom.querySelectorAll("label")[1]).toHaveTextContent("Send the agenda");
  });

  it("renders bullets, mentions and http(s) links", () => {
    const dom = toDom(`- Follow up with @[Priya Patel](user:${people[0].id})\nSee [the docs](https://docs.worknexa.test/notes)`);

    expect(dom.querySelector("div")).toHaveTextContent("• Follow up with @Priya Patel");
    expect(dom.querySelector("span")).toHaveTextContent("@Priya Patel");
    const link = dom.querySelector("a");
    expect(link).toHaveTextContent("the docs");
    expect(link).toHaveAttribute("href", "https://docs.worknexa.test/notes");
    expect(link).toHaveAttribute("target", "_blank");
    expect(link).toHaveAttribute("rel", "noreferrer");
  });

  it("escapes raw HTML so no elements are injected", () => {
    const source = "<img src=x onerror=alert(1)> <script>alert(2)</script> <b onclick=\"x()\">b</b> & more";
    const dom = toDom(source);

    expect(dom.querySelector("img, script, b, [onerror], [onclick]")).toBeNull();
    expect(dom.textContent).toBe(source);
  });

  it("escapes HTML inside mentions, code and checklist items", () => {
    const dom = toDom(`@[<img src=x onerror=alert(1)>](user:${people[0].id})\n\`<svg onload=alert(1)>\`\n- [ ] <iframe src=javascript:alert(1)>`);

    expect(dom.querySelector("img, svg, iframe, [onerror], [onload]")).toBeNull();
    expect(dom.textContent).toContain("<svg onload=alert(1)>");
  });

  it("escapes double quotes so a link cannot break out of its href attribute", () => {
    const html = renderMarkdown('[x](https://a.test/"onmouseover="alert(1))');
    const dom = toDom('[x](https://a.test/"onmouseover="alert(1))');

    expect(html).toContain("&quot;onmouseover=&quot;");
    expect(dom.querySelector("[onmouseover]")).toBeNull();
    const link = dom.querySelector("a");
    expect(link?.getAttributeNames().sort()).toEqual(["class", "href", "rel", "target"]);
    expect(link?.getAttribute("href")).toBe('https://a.test/"onmouseover="alert(1');
  });

  it("escapes single quotes as well", () => {
    const html = renderMarkdown("[x](https://a.test/'onmouseover='alert(1))");
    const dom = toDom("[x](https://a.test/'onmouseover='alert(1))");

    expect(html).toContain("&#39;onmouseover=&#39;");
    expect(dom.querySelector("[onmouseover]")).toBeNull();
    expect(dom.querySelector("a")?.getAttributeNames().sort()).toEqual(["class", "href", "rel", "target"]);
  });

  it("does not turn javascript: or data: URLs into links", () => {
    const dom = toDom("[click](javascript:alert(1)) [data](data:text/html,<script>alert(1)</script>) [x](JaVaScRiPt:alert(1))");

    expect(dom.querySelector("a")).toBeNull();
    expect(dom.textContent).toContain("[click](javascript:alert(1))");
  });
});

function ControlledEditor({ initial = "", onChange }: { initial?: string; onChange?: (value: string) => void }) {
  const [value, setValue] = useState(initial);
  return (
    <MarkdownEditor
      id="body"
      label="Body"
      value={value}
      people={people}
      onChange={(next) => {
        setValue(next);
        onChange?.(next);
      }}
    />
  );
}

describe("MarkdownEditor", () => {
  it("toggles a preview that renders the sanitized markdown and back to the editor", async () => {
    const user = userEvent.setup();
    const { container } = render(<ControlledEditor initial={"**Decision** made\n<img src=x onerror=alert(1)>\n- [x] Done"} />);

    expect(container.querySelector("textarea")).toHaveValue("**Decision** made\n<img src=x onerror=alert(1)>\n- [x] Done");
    await user.click(screen.getByRole("button", { name: "Preview" }));

    expect(container.querySelector("textarea")).toBeNull();
    expect(screen.getByText("Decision").tagName).toBe("STRONG");
    expect(screen.getByRole("checkbox")).toBeChecked();
    expect(screen.getByText("<img src=x onerror=alert(1)>")).toBeInTheDocument();
    expect(container.querySelector("img, [onerror]")).toBeNull();

    await user.click(screen.getByRole("button", { name: "Edit" }));
    expect(container.querySelector("textarea")).toHaveValue("**Decision** made\n<img src=x onerror=alert(1)>\n- [x] Done");
    expect(screen.getByRole("button", { name: "Preview" })).toBeInTheDocument();
  });

  it("previews what was typed", async () => {
    const user = userEvent.setup();
    const { container } = render(<ControlledEditor />);

    await user.click(container.querySelector("textarea")!);
    await user.paste('Read [spec](https://a.test/"onclick="x())');
    await user.click(screen.getByRole("button", { name: "Preview" }));

    expect(screen.getByRole("link", { name: "spec" })).toHaveAttribute("href", 'https://a.test/"onclick="x(');
    expect(container.querySelector("[onclick]")).toBeNull();
  });

  it("wraps placeholder text in bold markers from the toolbar", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<ControlledEditor onChange={onChange} />);

    await user.click(screen.getByRole("button", { name: "Bold" }));

    expect(onChange).toHaveBeenLastCalledWith("**text**");
  });

  it("suggests people while typing a mention and inserts the mention token", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { container } = render(<ControlledEditor onChange={onChange} />);

    await user.type(container.querySelector("textarea")!, "Ask @pri");
    await user.click(await screen.findByRole("button", { name: "@Priya Patel" }));

    expect(onChange).toHaveBeenLastCalledWith(`Ask @[Priya Patel](user:${people[0].id}) `);
    expect(screen.queryByRole("button", { name: "@Priya Patel" })).not.toBeInTheDocument();
  });
});
