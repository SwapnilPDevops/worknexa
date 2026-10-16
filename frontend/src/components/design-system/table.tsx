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
    <div className="overflow-x-auto rounded-[8px] border border-[#D1CCC2] bg-[#DFDCD6] shadow-xs">
      <table className="w-full min-w-[36rem] border-collapse text-left text-sm">
        <caption className="sr-only">{caption}</caption>
        <thead className="bg-[#EBE8E2] text-[11px] font-mono tracking-wider text-[#4B5563] uppercase border-b border-[#D1CCC2]">
          <tr>
            {columns.map((column) => (
              <th key={column.key} scope="col" className="px-4 py-3 font-semibold">
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-[#D1CCC2]">
          {rows.map((row) => (
            <tr key={getRowKey(row)} className="transition-colors hover:bg-[#EBE8E2]/60">
              {columns.map((column) => (
                <td key={column.key} className="px-4 py-3 text-[#111827]">
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
