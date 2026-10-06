import { useState } from "react";

export function ThoughtsCard() {
  const [writing, setWriting] = useState(false);
  const [note, setNote] = useState("");

  return (
    <section className="thoughts" aria-label="Thoughts Time">
      <img src="/assets/lamp-glow.svg" width={101} height={101} alt="" className="thoughts-glow" />
      <img src="/assets/lamp.svg" width={24} height={24} alt="" className="thoughts-lamp" />
      <h2>Thoughts Time</h2>
      <p>We don’t have any notice for you, till then you can share your thoughts with your peers.</p>
      {writing ? (
        <form
          className="thoughts-form"
          onSubmit={(event) => {
            event.preventDefault();
            setWriting(false);
          }}
        >
          <input
            value={note}
            onChange={(event) => setNote(event.target.value)}
            placeholder="Write a message"
            aria-label="Write a message"
            autoFocus
          />
        </form>
      ) : (
        <button type="button" className="thoughts-button" onClick={() => setWriting(true)}>
          Write a message
        </button>
      )}
    </section>
  );
}
