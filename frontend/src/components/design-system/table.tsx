import type { ReactNode } from "react";

export type TableColumn<T> = {
  key: string;
  header: string;
  render: (row: T) => ReactNode;
};

type TableProps<T> = {
  columns: TableColumn<T>[];
  rows: T[];
  getRowKey: (row: T) => string;
  caption: string;
};

export function Table<T>({ columns, rows, getRowKey, caption }: TableProps<T>) {
  return (
    <div className="overflow-x-auto rounded-xl border border-line">
      <table className="w-full min-w-[36rem] border-collapse text-left text-sm">
        <caption className="sr-only">{caption}</caption>
        <thead className="bg-surface-muted text-xs tracking-wide text-muted uppercase">
          <tr>
            {columns.map((column) => (
              <th key={column.key} scope="col" className="px-3 py-2 font-medium">
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={getRowKey(row)} className="border-t border-line">
              {columns.map((column) => (
                <td key={column.key} className="px-3 py-2.5 text-ink">
                  {column.render(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
