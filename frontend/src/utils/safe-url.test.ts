import { describe, expect, it } from "vitest";
import { safeHref } from "./safe-url.ts";

describe("safeHref", () => {
  it.each([
    ["https://github.com/worknexa/app", "https://github.com/worknexa/app"],
    ["  http://docs.example.com/a?b=1 ", "http://docs.example.com/a?b=1"],
  ])("keeps web links: %s", (input, expected) => {
    expect(safeHref(input)).toBe(expected);
  });

  it.each([
    "javascript:alert(1)",
    "JAVASCRIPT:alert(1)",
    " javascript:alert(document.cookie)",
    "data:text/html,<script>alert(1)</script>",
    "vbscript:msgbox(1)",
    "github.com/worknexa",
    "/relative/path",
    "",
    null,
    undefined,
  ])("drops unsafe or relative links: %s", (input) => {
    expect(safeHref(input)).toBeUndefined();
  });
});
