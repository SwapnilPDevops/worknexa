import { useEffect, useRef, useState } from "react";
import { writeDraft } from "./draft-store.ts";

export function useAutosave<T>(key: string, value: T, baseline: string, canSave: boolean, initialStatus: string | null, save: (value: T) => Promise<void>) {
  const [status, setStatus] = useState(initialStatus);
  const latest = useRef(value);
  const saveRef = useRef(save);
  latest.current = value;
  saveRef.current = save;

  useEffect(() => {
    const flush = () => writeDraft(key, latest.current);
    window.addEventListener("beforeunload", flush);
    document.addEventListener("visibilitychange", flush);
    return () => {
      flush();
      window.removeEventListener("beforeunload", flush);
      document.removeEventListener("visibilitychange", flush);
    };
  }, [key]);

  useEffect(() => {
    writeDraft(key, value);
    const current = JSON.stringify(value);
    if (current === baseline) {
      return;
    }

    if (!canSave) {
      setStatus("Kept on this device");
      return;
    }

    setStatus("Saving…");
    let active = true;
    const handle = window.setTimeout(() => {
      void saveRef.current(value).then(() => {
        if (active) {
          setStatus("Saved");
        }
      }).catch(() => {
        if (active) {
          setStatus("Kept on this device");
        }
      });
    }, 800);
    return () => {
      active = false;
      window.clearTimeout(handle);
    };
  }, [baseline, canSave, key, value]);

  return status === "Saving…" && JSON.stringify(value) === baseline ? "Saved" : status;
}
