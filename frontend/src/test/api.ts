import { vi } from "vitest";

export type ApiCall = { method: string; path: string; query: URLSearchParams; body: unknown; headers: Headers };

type Reply = { readonly kind: "reply"; status: number; body: unknown };

type HandlerContext = { body: unknown; query: URLSearchParams; params: Record<string, string>; calls: ApiCall[] };

type Handler = unknown | ((context: HandlerContext) => unknown);

/** An error response in the backend's ApiError shape. */
export function fail(status: number, message = "Request failed", code = "ERROR"): Reply {
  return { kind: "reply", status, body: { code, message } };
}

export const noContent: Reply = { kind: "reply", status: 204, body: null };

function isReply(value: unknown): value is Reply {
  return typeof value === "object" && value !== null && (value as Reply).kind === "reply";
}

function match(pattern: string, path: string) {
  const expected = pattern.split("/");
  const actual = path.split("/");
  if (expected.length !== actual.length) {
    return null;
  }

  const params: Record<string, string> = {};
  for (let index = 0; index < expected.length; index++) {
    if (expected[index].startsWith(":")) {
      params[expected[index].slice(1)] = decodeURIComponent(actual[index]);
    } else if (expected[index] !== actual[index]) {
      return null;
    }
  }

  return params;
}

/**
 * Replaces fetch with a route table keyed by "METHOD /api/path" (":name" segments match anything).
 * Handlers return the payload, which is wrapped in the API's { data } envelope, or a fail()/noContent reply.
 * Unmatched requests get a 404 and are listed in `unhandled`.
 */
export function mockApi(routes: Record<string, Handler>) {
  const calls: ApiCall[] = [];
  const unhandled: string[] = [];
  const table = { ...routes };

  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(typeof input === "string" ? input : input instanceof URL ? input.href : input.url, "http://localhost");
    const method = (init?.method ?? "GET").toUpperCase();
    const body = typeof init?.body === "string" ? JSON.parse(init.body) as unknown : init?.body ?? null;
    const call = { method, path: url.pathname, query: url.searchParams, body, headers: new Headers(init?.headers) };
    calls.push(call);

    for (const [key, handler] of Object.entries(table)) {
      const [routeMethod, routePath] = key.split(" ");
      const params = routeMethod === method ? match(routePath, url.pathname) : null;
      if (!params) {
        continue;
      }

      const result = typeof handler === "function" ? (handler as (context: HandlerContext) => unknown)({ body, query: url.searchParams, params, calls }) : handler;
      const resolved = await result;
      if (isReply(resolved)) {
        return new Response(resolved.body === null ? null : JSON.stringify(resolved.body), { status: resolved.status, headers: { "Content-Type": "application/json" } });
      }

      return new Response(JSON.stringify({ data: resolved }), { status: 200, headers: { "Content-Type": "application/json" } });
    }

    unhandled.push(`${method} ${url.pathname}`);
    return new Response(JSON.stringify({ code: "NOT_FOUND", message: `No mock for ${method} ${url.pathname}` }), { status: 404, headers: { "Content-Type": "application/json" } });
  });

  vi.stubGlobal("fetch", fetchMock);

  return {
    calls,
    unhandled,
    fetch: fetchMock,
    /** Adds or replaces routes after rendering, e.g. to change what a refetch returns. */
    set(more: Record<string, Handler>) {
      Object.assign(table, more);
    },
    /** Requests sent to a route, optionally filtered by method. */
    sent(method: string, path: string) {
      return calls.filter((call) => call.method === method && match(path, call.path) !== null);
    },
  };
}
