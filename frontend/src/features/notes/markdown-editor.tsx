import { useMemo, useRef, useState, type ReactNode } from "react";
import { Button } from "../../components/design-system/button.tsx";
import { Textarea } from "../../components/design-system/field.tsx";

type Person = { id: string; name: string };

export function renderMarkdown(source: string) {
  const escaped = source
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
  const blocks: string[] = [];
  const withoutFences = escaped.replace(/```([\s\S]*?)```/g, (_, code: string) => {
    blocks.push(`<pre class="mt-2 overflow-x-auto rounded-md bg-canvas px-3 py-2 text-xs"><code>${code.replace(/^\n/, "").replace(/\n$/, "")}</code></pre>`);
    return `\u0000${blocks.length - 1}\u0000`;
  });
  const lines = withoutFences.split("\n").map((line) => {
    if (/^\u0000\d+\u0000$/.test(line.trim())) {
      return line.trim();
    }

    const checked = line.match(/^[-*] \[([ xX])\] (.*)$/);
    if (checked) {
      const mark = checked[1].toLowerCase() === "x" ? "checked" : "";
      return `<label class="flex items-start gap-2"><input type="checkbox" disabled ${mark}/> <span>${inline(checked[2])}</span></label>`;
    }

    if (line.startsWith("- ")) {
      return `<div class="pl-4">• ${inline(line.slice(2))}</div>`;
    }

    return line ? `<p>${inline(line)}</p>` : "<br/>";
  });
  return lines.join("").replace(/\u0000(\d+)\u0000/g, (_, index: string) => blocks[Number(index)] ?? "");
}

function inline(value: string) {
  return value
    .replace(/`([^`]+)`/g, "<code class=\"rounded bg-canvas px-1 text-xs\">$1</code>")
    .replace(/@\[([^\]]+)\]\(user:[0-9a-fA-F-]{36}\)/g, "<span class=\"font-medium text-accent\">@$1</span>")
    .replace(/\[([^\]]+)\]\((https?:\/\/[^\s)]+)\)/g, "<a class=\"text-accent underline\" href=\"$2\" target=\"_blank\" rel=\"noreferrer\">$1</a>")
    .replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>")
    .replace(/(^|[\s])\*([^*]+)\*(?=$|[\s])/g, "$1<em>$2</em>");
}

export function MarkdownEditor({
  id,
  label,
  value,
  people,
  onChange,
  disabled,
}: {
  id: string;
  label: string;
  value: string;
  people: Person[];
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  const area = useRef<HTMLTextAreaElement>(null);
  const [cursor, setCursor] = useState(0);
  const [preview, setPreview] = useState(false);
  const query = useMemo(() => mentionQuery(value, cursor), [value, cursor]);
  const matches = useMemo(() => {
    if (!query) {
      return [];
    }

    const term = query.query.toLowerCase();
    return people.filter((person) => person.name.toLowerCase().includes(term)).slice(0, 6);
  }, [people, query]);

  function apply(next: string, caret: number) {
    onChange(next);
    window.setTimeout(() => {
      area.current?.focus();
      area.current?.setSelectionRange(caret, caret);
      setCursor(caret);
    }, 0);
  }

  function surround(before: string, after: string) {
    const element = area.current;
    const start = element?.selectionStart ?? value.length;
    const end = element?.selectionEnd ?? value.length;
    const selected = value.slice(start, end) || "text";
    const next = `${value.slice(0, start)}${before}${selected}${after}${value.slice(end)}`;
    apply(next, start + before.length + selected.length + after.length);
  }

  function insert(snippet: string) {
    const element = area.current;
    const start = element?.selectionStart ?? value.length;
    const end = element?.selectionEnd ?? value.length;
    const next = `${value.slice(0, start)}${snippet}${value.slice(end)}`;
    apply(next, start + snippet.length);
  }

  function choose(person: Person) {
    if (!query) {
      insert(`@[${person.name}](user:${person.id}) `);
      return;
    }

    const next = `${value.slice(0, query.start)}@[${person.name}](user:${person.id}) ${value.slice(cursor)}`;
    apply(next, query.start + person.name.length + person.id.length + 11);
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-1">
        <span className="mr-1 text-[13px] font-medium">{label}</span>
        <ToolButton disabled={disabled} onClick={() => surround("**", "**")}>Bold</ToolButton>
        <ToolButton disabled={disabled} onClick={() => surround("*", "*")}>Italic</ToolButton>
        <ToolButton disabled={disabled} onClick={() => insert("\n- [ ] ")}>Checklist</ToolButton>
        <ToolButton disabled={disabled} onClick={() => insert("\n```\n\n```\n")}>Code</ToolButton>
        <ToolButton disabled={disabled} onClick={() => surround("[", "](https://)")}>Link</ToolButton>
        <ToolButton disabled={disabled} onClick={() => insert("@")}>Mention</ToolButton>
        <ToolButton disabled={disabled} onClick={() => setPreview((current) => !current)}>{preview ? "Edit" : "Preview"}</ToolButton>
      </div>
      {preview ? (
        <div className="min-h-28 space-y-1 rounded-lg border border-line bg-surface px-3 py-2 text-sm" dangerouslySetInnerHTML={{ __html: renderMarkdown(value) }} />
      ) : (
        <Textarea
          ref={area}
          id={id}
          value={value}
          disabled={disabled}
          rows={6}
          onChange={(event) => {
            onChange(event.target.value);
            setCursor(event.target.selectionStart);
          }}
          onClick={(event) => setCursor(event.currentTarget.selectionStart)}
          onKeyUp={(event) => setCursor(event.currentTarget.selectionStart)}
        />
      )}
      {matches.length > 0 ? (
        <ul className="overflow-hidden rounded-lg border border-line bg-surface">
          {matches.map((person) => (
            <li key={person.id}>
              <button type="button" className="block w-full px-3 py-1.5 text-left text-sm hover:bg-canvas" onClick={() => choose(person)}>@{person.name}</button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}

function ToolButton({ children, onClick, disabled }: { children: ReactNode; onClick: () => void; disabled?: boolean }) {
  return <Button type="button" size="sm" variant="ghost" disabled={disabled} onClick={onClick}>{children}</Button>;
}

function mentionQuery(value: string, cursor: number) {
  const match = value.slice(0, cursor).match(/(^|\s)@([^\s@]*)$/);
  if (!match) {
    return null;
  }

  return { start: cursor - match[2].length - 1, query: match[2] };
}
