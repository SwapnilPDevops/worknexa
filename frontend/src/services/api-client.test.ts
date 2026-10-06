import { describe, expect, it } from "vitest";
import { fail, mockApi, noContent } from "../test/api.ts";
import { ApiError, getJson, sendForm, sendJson } from "./api-client.ts";

describe("api-client", () => {
  it("sends the CSRF header and includes credentials on every request", async () => {
    const api = mockApi({ "GET /api/things": [1, 2] });

    await expect(getJson("/api/things")).resolves.toEqual({ data: [1, 2] });

    const [call] = api.sent("GET", "/api/things");
    expect(call.headers.get("X-Worknexa-Request")).toBe("1");
    expect(api.fetch.mock.calls[0][1]?.credentials).toBe("include");
  });

  it("serializes JSON bodies with a JSON content type", async () => {
    const api = mockApi({ "POST /api/things": ({ body }: { body: unknown }) => body });

    await sendJson("/api/things", "POST", { title: "A" });

    const [call] = api.sent("POST", "/api/things");
    expect(call.body).toEqual({ title: "A" });
    expect(call.headers.get("Content-Type")).toBe("application/json");
  });

  it("returns null for 204 responses", async () => {
    mockApi({ "DELETE /api/things/:id": noContent });

    await expect(sendJson("/api/things/1", "DELETE")).resolves.toBeNull();
  });

  it("does not force a JSON content type on multipart uploads", async () => {
    const api = mockApi({ "POST /api/files": { id: "f1" } });
    const form = new FormData();
    form.append("file", new Blob(["hello"], { type: "text/plain" }), "hello.txt");

    await sendForm("/api/files", form);

    expect(api.sent("POST", "/api/files")[0].headers.get("Content-Type")).toBeNull();
  });

  it("maps error responses to ApiError with status, message and code", async () => {
    mockApi({ "POST /api/things": fail(400, "Title is required.", "VALIDATION_FAILED") });

    const error = await sendJson("/api/things", "POST", {}).catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 400, message: "Title is required.", code: "VALIDATION_FAILED" });
  });

  it("falls back to a status message when the error body is not JSON", async () => {
    const api = mockApi({});
    api.fetch.mockResolvedValueOnce(new Response("<html>bad gateway</html>", { status: 502 }));

    const error = await getJson("/api/things").catch((caught: unknown) => caught);

    expect(error).toMatchObject({ status: 502, message: "Request failed with status 502", code: "UNEXPECTED_ERROR" });
  });

  it("does not read or persist tokens in web storage", async () => {
    sessionStorage.setItem("worknexa.access-token", "stale");
    const api = mockApi({ "GET /api/things": [] });

    await getJson("/api/things");

    expect(api.sent("GET", "/api/things")[0].headers.get("Authorization")).toBeNull();
  });
});
