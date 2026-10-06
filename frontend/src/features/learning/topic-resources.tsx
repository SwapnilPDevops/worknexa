import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Button } from "../../components/design-system/button.tsx";
import { Field, Input, Select } from "../../components/design-system/field.tsx";
import { ApiError } from "../../services/api-client.ts";
import { addLearningResource, deleteLearningResource, fetchLearningTopic } from "./api.ts";
import { resourceTypes } from "./catalog.ts";

export function TopicResources({ topicId, manageable }: { topicId: string; manageable: boolean }) {
  const queryClient = useQueryClient();
  const topic = useQuery({ queryKey: ["learning-topic", topicId], queryFn: () => fetchLearningTopic(topicId), enabled: topicId.length > 0 });
  const [title, setTitle] = useState("");
  const [url, setUrl] = useState("");
  const [type, setType] = useState("Article");
  const [message, setMessage] = useState<string | null>(null);

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: ["learning-topic", topicId] });
  };

  const add = useMutation({
    mutationFn: () => addLearningResource(topicId, { title: title.trim(), url: url.trim() || null, type }),
    onSuccess: async () => {
      setTitle("");
      setUrl("");
      setMessage(null);
      await refresh();
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The resource could not be saved."),
  });

  const remove = useMutation({
    mutationFn: (resourceId: string) => deleteLearningResource(topicId, resourceId),
    onSuccess: () => refresh(),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    if (title.trim()) {
      add.mutate();
    }
  }

  return (
    <section className="space-y-3">
      <h2 className="text-sm font-medium">Resources</h2>
      {topic.isError ? <p className="text-sm text-danger">{topic.error instanceof ApiError ? topic.error.message : "Resources could not be loaded."}</p> : null}
      <ul className="space-y-2">
        {(topic.data?.resources ?? []).map((resource) => (
          <li key={resource.id} className="flex items-start justify-between gap-2 rounded-lg border border-line px-3 py-2">
            <div>
              <p className="text-sm font-medium">{resource.title}</p>
              <p className="text-xs text-muted">{resource.type}{resource.url ? ` · ${resource.url}` : ""}</p>
            </div>
            {manageable ? <Button type="button" size="sm" variant="ghost" onClick={() => remove.mutate(resource.id)}>Remove</Button> : null}
          </li>
        ))}
      </ul>
      {topic.data && topic.data.resources.length === 0 ? <p className="text-sm text-muted">No resources yet.</p> : null}
      {manageable ? (
        <form className="grid gap-2 sm:grid-cols-[1fr_1fr_8rem_auto]" onSubmit={submit}>
          <Field label="Resource">{({ id }) => <Input id={id} value={title} maxLength={300} onChange={(event) => setTitle(event.target.value)} />}</Field>
          <Field label="Link">{({ id }) => <Input id={id} value={url} placeholder="https://" onChange={(event) => setUrl(event.target.value)} />}</Field>
          <Field label="Type">{({ id }) => (
            <Select id={id} value={type} onChange={(event) => setType(event.target.value)}>
              {resourceTypes.map((item) => <option key={item} value={item}>{item}</option>)}
            </Select>
          )}</Field>
          <div className="flex items-end"><Button type="submit" size="sm" disabled={add.isPending || title.trim().length === 0}>Add</Button></div>
        </form>
      ) : null}
      {message ? <p className="text-sm text-danger">{message}</p> : null}
    </section>
  );
}
