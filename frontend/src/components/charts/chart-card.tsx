import { useId, type ReactNode } from "react";
import { Card, CardBody, CardHeader } from "../design-system/card.tsx";
import { Table, type TableColumn } from "../design-system/table.tsx";
import { cn } from "../../utils/cn.ts";

export type LegendItem = { label: string; color: string };

type ChartCardProps<T> = {
  title: string;
  question: string;
  insight: string;
  legend?: LegendItem[];
  note?: string;
  empty?: string;
  rows: T[];
  columns: TableColumn<T>[];
  getRowKey: (row: T) => string;
  className?: string;
  children: ReactNode;
};

/**
 * The chart itself is hidden from assistive technology; the insight sentence and the data
 * table carry the same information in text.
 */
export function ChartCard<T>({ title, question, insight, legend, note, empty, rows, columns, getRowKey, className, children }: ChartCardProps<T>) {
  const insightId = useId();
  return (
    <Card className={cn("min-w-0", className)}>
      <CardHeader title={title} description={question} />
      <CardBody>
        {empty ? (
          <p className="text-sm text-muted">{empty}</p>
        ) : (
          <figure aria-describedby={insightId} className="space-y-3">
            <figcaption id={insightId} className="text-sm leading-6 text-ink">{insight}</figcaption>
            {legend ? (
              <ul className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted" aria-hidden="true">
                {legend.map((item) => (
                  <li key={item.label} className="flex items-center gap-1.5">
                    <span className="size-2.5 rounded-sm" style={{ background: item.color }} />
                    {item.label}
                  </li>
                ))}
              </ul>
            ) : null}
            <div aria-hidden="true">{children}</div>
            {note ? <p className="text-xs text-muted">{note}</p> : null}
            <details className="text-sm">
              <summary className="w-fit cursor-pointer rounded text-xs font-medium text-muted hover:text-ink">Show data</summary>
              <div className="mt-2">
                <Table caption={title} columns={columns} rows={rows} getRowKey={getRowKey} />
              </div>
            </details>
          </figure>
        )}
      </CardBody>
    </Card>
  );
}
