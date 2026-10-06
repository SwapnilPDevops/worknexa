import { afterEach } from "vitest";

type Listener = (reason: unknown) => void;

type RejectionEmitter = {
  listeners(event: "unhandledRejection"): Listener[];
  removeAllListeners(event: "unhandledRejection"): void;
  on(event: "unhandledRejection", listener: Listener): void;
  off(event: "unhandledRejection", listener: Listener): void;
};

// The app tsconfig has no Node types; the tests run in Node, where `process` exists.
const emitter = (globalThis as unknown as { process: RejectionEmitter }).process;

let restore: (() => void) | null = null;

afterEach(() => {
  restore?.();
  restore = null;
});

/**
 * Collects unhandled promise rejections until the current test ends instead of letting Vitest fail the run.
 * Use it only to pin down production code that fires a promise without handling its rejection.
 */
export function captureUnhandledRejections() {
  restore?.();
  const reasons: unknown[] = [];
  const previous = emitter.listeners("unhandledRejection");
  const listener: Listener = (reason) => {
    reasons.push(reason);
  };
  emitter.removeAllListeners("unhandledRejection");
  emitter.on("unhandledRejection", listener);
  restore = () => {
    emitter.off("unhandledRejection", listener);
    for (const handler of previous) {
      emitter.on("unhandledRejection", handler);
    }
  };
  return reasons;
}
