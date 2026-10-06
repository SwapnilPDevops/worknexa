import { useEffect, useState } from "react";
import { Badge } from "../components/design-system/badge.tsx";
import { Avatar, AvatarGroup } from "../components/design-system/avatar.tsx";
import { Button } from "../components/design-system/button.tsx";
import { Card, CardBody, CardHeader } from "../components/design-system/card.tsx";
import { Dialog } from "../components/design-system/dialog.tsx";
import { Drawer } from "../components/design-system/drawer.tsx";
import { Field, Input, Select, Textarea } from "../components/design-system/field.tsx";
import { Progress } from "../components/design-system/progress.tsx";
import { EmptyState, ErrorState, LoadingState } from "../components/design-system/states.tsx";
import { Table, type TableColumn } from "../components/design-system/table.tsx";
import { Tabs } from "../components/design-system/tabs.tsx";
import { Tooltip } from "../components/design-system/tooltip.tsx";

type Specimen = { name: string; state: string; owner: string };

const rows: Specimen[] = [
  { name: "Specimen row", state: "Ready", owner: "Design" },
  { name: "Second row", state: "Review", owner: "System" },
];

const columns: TableColumn<Specimen>[] = [
  { key: "name", header: "Name", render: (row) => row.name },
  { key: "state", header: "State", render: (row) => <Badge tone="accent">{row.state}</Badge> },
  { key: "owner", header: "Owner", render: (row) => row.owner },
];

export function FoundationsPage() {
  const [tab, setTab] = useState("cards");

  useEffect(() => {
    document.title = "Design system · Worknexa";
  }, []);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);

  return (
    <div className="mx-auto flex max-w-5xl flex-col gap-6">
      <div>
        <h1 className="text-xl font-semibold tracking-tight">Design system</h1>
        <p className="mt-1 max-w-2xl text-sm text-muted">
          Specimens for the Worknexa interface. These controls are not connected to work data.
        </p>
      </div>

      <Card>
        <CardHeader title="Buttons" />
        <CardBody className="flex flex-wrap gap-2">
          <Button>Primary</Button>
          <Button variant="secondary">Secondary</Button>
          <Button variant="ghost">Ghost</Button>
          <Button variant="danger">Danger</Button>
          <Button disabled>Disabled</Button>
          <Tooltip label="More actions">
            <Button variant="secondary" size="icon" aria-label="More actions">
              ···
            </Button>
          </Tooltip>
        </CardBody>
      </Card>

      <Card>
        <CardHeader title="Fields" />
        <CardBody className="grid gap-4 md:grid-cols-2">
          <Field label="Title" hint="Shown on the card.">
            {({ id, describedBy }) => <Input id={id} aria-describedby={describedBy} placeholder="Name the work" />}
          </Field>
          <Field label="Owner" error="Choose someone when assignment exists.">
            {({ id, describedBy }) => (
              <Select id={id} aria-describedby={describedBy} defaultValue="">
                <option value="" disabled>
                  Select
                </option>
                <option>Unassigned</option>
              </Select>
            )}
          </Field>
          <Field label="Notes">
            {({ id }) => <Textarea id={id} placeholder="Optional detail" />}
          </Field>
        </CardBody>
      </Card>

      <Card>
        <CardHeader title="Status" />
        <CardBody className="flex flex-col gap-4">
          <div className="flex flex-wrap items-center gap-2">
            <Badge>Neutral</Badge>
            <Badge tone="accent">Accent</Badge>
            <Badge tone="signal">Signal</Badge>
            <Badge tone="warning">Warning</Badge>
            <Badge tone="danger">Danger</Badge>
            <Avatar name="Worknexa Dev" />
            <AvatarGroup names={["Ada Lovelace", "Grace Hopper", "Alan Kay"]} />
          </div>
          <Progress value={42} label="Specimen progress" />
          <Tabs
            label="Specimen tabs"
            value={tab}
            onChange={setTab}
            tabs={[
              { id: "cards", label: "Cards" },
              { id: "lists", label: "Lists" },
              { id: "board", label: "Board" },
            ]}
          />
        </CardBody>
      </Card>

      <Table columns={columns} rows={rows} getRowKey={(row) => row.name} caption="Specimen table" />

      <div className="grid gap-4 lg:grid-cols-2">
        <EmptyState title="Empty" description="Use this when a module has nothing to show yet." />
        <div className="space-y-4">
          <LoadingState label="Loading specimen" />
          <ErrorState title="Unable to load" description="This is the error treatment. Retry does not call an API." onRetry={() => undefined} />
        </div>
      </div>

      <div className="flex gap-2">
        <Button variant="secondary" onClick={() => setDialogOpen(true)}>
          Open dialog
        </Button>
        <Button variant="secondary" onClick={() => setDrawerOpen(true)}>
          Open drawer
        </Button>
      </div>
      <Dialog open={dialogOpen} title="Dialog" description="A focused surface for a short decision." onClose={() => setDialogOpen(false)}>
        <p className="text-sm text-muted">Dialog content stays in the page until a module needs it.</p>
      </Dialog>
      <Drawer open={drawerOpen} title="Drawer" onClose={() => setDrawerOpen(false)}>
        <p className="text-sm text-muted">Drawers hold secondary detail without leaving the current page.</p>
      </Drawer>
    </div>
  );
}
