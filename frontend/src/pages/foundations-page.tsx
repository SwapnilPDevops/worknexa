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
import * as Shadcn from "../components/shadcn/index.ts";
import * as ReactBits from "../components/react-bits/index.ts";

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
  const [shadcnTab, setShadcnTab] = useState("overview");
  const [dialogOpen, setDialogOpen] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [counterTrigger, setCounterTrigger] = useState(0);

  useEffect(() => {
    document.title = "Design system · Worknexa";
  }, []);

  return (
    <div className="flex w-full flex-col gap-8 pb-12">
      <div>
        <span className="inline-block text-[10px] font-mono font-semibold tracking-wider text-[#4B5563] uppercase bg-[#D7D3CB] px-2 py-0.5 rounded-[4px] border border-[#111827]/10 mb-1.5">
          <ReactBits.ShinyText>SYS.DOC // 001.4 DESIGN SYSTEM &amp; FOUNDATIONS</ReactBits.ShinyText>
        </span>
        <h1 className="wx-headline-lg text-[#111827] font-semibold">
          <ReactBits.BlurText text="Strategic Roadmap Design System" />
        </h1>
        <p className="mt-1 max-w-2xl text-sm text-[#4B5563]">
          Neuform Strategic Roadmap foundation tokens &amp; components from design.md. High-density modular panels, warm stone architectural palette (#FED7AA, #DFDCD6, #E6E1DA), Poppins typography, combined with official <strong>shadcn/ui</strong> primitives and <strong>React Bits</strong> interactive effects.
        </p>
      </div>

      {/* REACT BITS SHOWCASE */}
      <section className="space-y-4">
        <div className="flex items-center justify-between border-b border-[#D1CCC2] pb-2">
          <div>
            <span className="text-[10px] font-mono uppercase text-[#4B5563]">ANIMATIONS &amp; MOTION</span>
            <h2 className="text-lg font-semibold text-[#111827]">React Bits Components</h2>
          </div>
          <ReactBits.Magnet magnetStrength={3}>
            <Button size="sm" variant="secondary" onClick={() => setCounterTrigger((c) => c + 1)}>
              Replay Animations
            </Button>
          </ReactBits.Magnet>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <ReactBits.SpotlightCard className="p-5 flex flex-col justify-between">
            <div>
              <span className="text-[10px] font-mono uppercase text-[#4B5563]">React Bits // SpotlightCard</span>
              <h3 className="mt-2 text-base font-semibold text-[#111827]">Radial Spotlight Hover</h3>
              <p className="mt-1 text-xs text-[#4B5563] leading-relaxed">
                Move your cursor around this card. An ambient radial spotlight tracks your mouse with a subtle #FED7AA glow.
              </p>
            </div>
            <div className="mt-4 pt-3 border-t border-[#D1CCC2]/60 flex items-center justify-between">
              <span className="text-[11px] font-mono text-[#4B5563]">Cursor-responsive</span>
              <Shadcn.Badge variant="default">Active</Shadcn.Badge>
            </div>
          </ReactBits.SpotlightCard>

          <ReactBits.SpotlightCard className="p-5 flex flex-col justify-between">
            <div>
              <span className="text-[10px] font-mono uppercase text-[#4B5563]">React Bits // ShinyText</span>
              <h3 className="mt-2 text-base font-semibold text-[#111827]">
                <ReactBits.ShinyText speed={3}>Shimmering Text Highlight</ReactBits.ShinyText>
              </h3>
              <p className="mt-1 text-xs text-[#4B5563] leading-relaxed">
                Creates an animated metallic shine gradient sweep across technical badges, metrics, and callouts.
              </p>
            </div>
            <div className="mt-4 pt-3 border-t border-[#D1CCC2]/60">
              <span className="inline-block px-2.5 py-1 rounded-[6px] bg-[#EBE8E2] border border-[#D1CCC2] text-xs font-mono">
                <ReactBits.ShinyText>SYS.METRIC // OPTIMIZED</ReactBits.ShinyText>
              </span>
            </div>
          </ReactBits.SpotlightCard>

          <ReactBits.SpotlightCard className="p-5 flex flex-col justify-between">
            <div>
              <span className="text-[10px] font-mono uppercase text-[#4B5563]">React Bits // CountUp</span>
              <div key={counterTrigger} className="mt-2 flex items-baseline gap-2">
                <span className="text-3xl font-semibold text-[#111827] tabular-nums">
                  <ReactBits.CountUp to={21} prefix="$" suffix="M" decimals={1} duration={1.5} />
                </span>
                <span className="text-xs font-medium text-emerald-700 bg-emerald-500/15 px-1.5 py-0.5 rounded-[4px]">
                  +<ReactBits.CountUp to={600} suffix="%" duration={1.5} />
                </span>
              </div>
              <p className="mt-1 text-xs text-[#4B5563] leading-relaxed">
                Smooth exponential number counting easing for dashboard metrics and progress stats.
              </p>
            </div>
            <div className="mt-4 pt-3 border-t border-[#D1CCC2]/60 flex items-center justify-between">
              <span className="text-[11px] font-mono text-[#4B5563]">ARR Growth Metric</span>
              <span className="text-xs font-medium text-[#111827]">Series B</span>
            </div>
          </ReactBits.SpotlightCard>
        </div>

        {/* Magnet & Canvas Squares Playground */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <ReactBits.SpotlightCard className="p-5">
            <span className="text-[10px] font-mono uppercase text-[#4B5563]">React Bits // Magnet Attraction</span>
            <h3 className="mt-2 text-base font-semibold text-[#111827]">Magnetic Hover Interactions</h3>
            <p className="mt-1 text-xs text-[#4B5563] leading-relaxed">
              Hover near the buttons below. Elements pull toward your cursor using a gentle physics spring effect.
            </p>
            <div className="mt-4 flex flex-wrap items-center gap-3">
              <ReactBits.Magnet magnetStrength={3}>
                <Button>Magnetic Primary</Button>
              </ReactBits.Magnet>
              <ReactBits.Magnet magnetStrength={4}>
                <Button variant="secondary">Magnetic Secondary</Button>
              </ReactBits.Magnet>
              <ReactBits.Magnet magnetStrength={5}>
                <span className="px-3 py-1.5 rounded-[8px] bg-[#FED7AA] border border-[#111827]/15 text-xs font-medium text-[#111827] cursor-pointer inline-block shadow-xs">
                  Magnetic Pill
                </span>
              </ReactBits.Magnet>
            </div>
          </ReactBits.SpotlightCard>

          <div className="relative h-48 overflow-hidden rounded-[8px] border border-[#D1CCC2] bg-[#EBE8E2]">
            <ReactBits.Squares
              direction="diagonal"
              speed={0.4}
              squareSize={32}
              borderColor="rgba(17, 24, 39, 0.08)"
              hoverFillColor="rgba(254, 215, 170, 0.4)"
              className="absolute inset-0"
            />
            <div className="pointer-events-none absolute inset-0 flex flex-col justify-end p-4 bg-gradient-to-t from-[#EBE8E2] via-[#EBE8E2]/60 to-transparent">
              <span className="text-[10px] font-mono uppercase text-[#4B5563]">React Bits // Squares Background</span>
              <p className="text-xs font-semibold text-[#111827]">Interactive Kinetic Grid Canvas</p>
              <p className="text-[11px] text-[#4B5563]">Move your cursor over the canvas grid to illuminate squares.</p>
            </div>
          </div>
        </div>
      </section>

      {/* SHADCN/UI SHOWCASE */}
      <section className="space-y-4">
        <div className="border-b border-[#D1CCC2] pb-2">
          <span className="text-[10px] font-mono uppercase text-[#4B5563]">UI PRIMITIVES</span>
          <h2 className="text-lg font-semibold text-[#111827]">shadcn/ui Components</h2>
        </div>

        <Shadcn.Tabs value={shadcnTab} onValueChange={setShadcnTab}>
          <Shadcn.TabsList>
            <Shadcn.TabsTrigger value="overview">Overview &amp; Cards</Shadcn.TabsTrigger>
            <Shadcn.TabsTrigger value="buttons">Button Variants</Shadcn.TabsTrigger>
            <Shadcn.TabsTrigger value="badges">Badges &amp; Avatars</Shadcn.TabsTrigger>
            <Shadcn.TabsTrigger value="forms">Inputs &amp; Controls</Shadcn.TabsTrigger>
          </Shadcn.TabsList>

          <Shadcn.TabsContent value="overview" className="mt-4 space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Shadcn.Card>
                <Shadcn.CardHeader>
                  <Shadcn.CardTitle>Strategic Roadmap Overview</Shadcn.CardTitle>
                  <Shadcn.CardDescription>
                    Official shadcn/ui Card architecture with CardHeader, CardTitle, CardDescription, and CardContent.
                  </Shadcn.CardDescription>
                </Shadcn.CardHeader>
                <Shadcn.CardContent>
                  <p className="text-xs text-[#4B5563] leading-relaxed">
                    Designed for high information density and modular layout rhythm. Seamlessly styled to match the warm Neuform stone canvas and 8px radii.
                  </p>
                  <Shadcn.Separator className="my-3" />
                  <div className="flex items-center justify-between text-xs">
                    <span className="font-mono text-[#4B5563]">Status</span>
                    <Shadcn.Badge variant="default">Verified</Shadcn.Badge>
                  </div>
                </Shadcn.CardContent>
                <Shadcn.CardFooter className="justify-between pt-3">
                  <span className="text-[11px] font-mono text-[#4B5563]">SYS.CARD // 01</span>
                  <Shadcn.Button size="sm" variant="outline">Inspect</Shadcn.Button>
                </Shadcn.CardFooter>
              </Shadcn.Card>

              <Shadcn.Card>
                <Shadcn.CardHeader>
                  <Shadcn.CardTitle>Deployment Stream</Shadcn.CardTitle>
                  <Shadcn.CardDescription>
                    Second shadcn/ui Card instance with technical metadata.
                  </Shadcn.CardDescription>
                </Shadcn.CardHeader>
                <Shadcn.CardContent>
                  <p className="text-xs text-[#4B5563] leading-relaxed">
                    Series B infrastructure expansion tracking. Demonstrates nested surfaces and standard shadcn component composition.
                  </p>
                  <Shadcn.Separator className="my-3" />
                  <div className="flex items-center justify-between text-xs">
                    <span className="font-mono text-[#4B5563]">Cluster Health</span>
                    <Shadcn.Badge variant="secondary">99.98%</Shadcn.Badge>
                  </div>
                </Shadcn.CardContent>
                <Shadcn.CardFooter className="justify-between pt-3">
                  <span className="text-[11px] font-mono text-[#4B5563]">SYS.CARD // 02</span>
                  <Shadcn.Button size="sm" variant="default">Deploy</Shadcn.Button>
                </Shadcn.CardFooter>
              </Shadcn.Card>
            </div>
          </Shadcn.TabsContent>

          <Shadcn.TabsContent value="buttons" className="mt-4">
            <Shadcn.Card>
              <Shadcn.CardHeader>
                <Shadcn.CardTitle>shadcn/ui Button Variants</Shadcn.CardTitle>
                <Shadcn.CardDescription>All standard shadcn button variants styled to design tokens.</Shadcn.CardDescription>
              </Shadcn.CardHeader>
              <Shadcn.CardContent className="flex flex-wrap items-center gap-2.5">
                <Shadcn.Button variant="default">Default (#FED7AA)</Shadcn.Button>
                <Shadcn.Button variant="secondary">Secondary</Shadcn.Button>
                <Shadcn.Button variant="outline">Outline</Shadcn.Button>
                <Shadcn.Button variant="ghost">Ghost</Shadcn.Button>
                <Shadcn.Button variant="destructive">Destructive</Shadcn.Button>
                <Shadcn.Button variant="link">Link</Shadcn.Button>
                <Shadcn.Button size="sm">Small</Shadcn.Button>
                <Shadcn.Button size="lg">Large</Shadcn.Button>
                <Shadcn.Button size="icon" aria-label="Icon button">⚡</Shadcn.Button>
                <Shadcn.Button disabled>Disabled</Shadcn.Button>
              </Shadcn.CardContent>
            </Shadcn.Card>
          </Shadcn.TabsContent>

          <Shadcn.TabsContent value="badges" className="mt-4">
            <Shadcn.Card>
              <Shadcn.CardHeader>
                <Shadcn.CardTitle>shadcn/ui Badges &amp; Avatars</Shadcn.CardTitle>
                <Shadcn.CardDescription>Status indicators and profile specimens.</Shadcn.CardDescription>
              </Shadcn.CardHeader>
              <Shadcn.CardContent className="space-y-4">
                <div className="flex flex-wrap items-center gap-2">
                  <Shadcn.Badge variant="default">Default</Shadcn.Badge>
                  <Shadcn.Badge variant="secondary">Secondary</Shadcn.Badge>
                  <Shadcn.Badge variant="outline">Outline</Shadcn.Badge>
                  <Shadcn.Badge variant="destructive">Destructive</Shadcn.Badge>
                </div>
                <Shadcn.Separator />
                <div className="flex items-center gap-3">
                  <Shadcn.Avatar>
                    <Shadcn.AvatarFallback>SP</Shadcn.AvatarFallback>
                  </Shadcn.Avatar>
                  <Shadcn.Avatar>
                    <Shadcn.AvatarFallback>WN</Shadcn.AvatarFallback>
                  </Shadcn.Avatar>
                  <span className="text-xs text-[#4B5563]">shadcn/ui Avatar with stone fallback container</span>
                </div>
              </Shadcn.CardContent>
            </Shadcn.Card>
          </Shadcn.TabsContent>

          <Shadcn.TabsContent value="forms" className="mt-4">
            <Shadcn.Card>
              <Shadcn.CardHeader>
                <Shadcn.CardTitle>shadcn/ui Form Inputs</Shadcn.CardTitle>
                <Shadcn.CardDescription>Standard input controls with focus rings and 8px radii.</Shadcn.CardDescription>
              </Shadcn.CardHeader>
              <Shadcn.CardContent className="grid gap-3 sm:grid-cols-2">
                <Shadcn.Input placeholder="Project title or query..." />
                <Shadcn.Input type="email" placeholder="email@worknexa.internal" />
              </Shadcn.CardContent>
            </Shadcn.Card>
          </Shadcn.TabsContent>
        </Shadcn.Tabs>
      </section>

      {/* CORE WORKNEXA DESIGN SYSTEM SPECIMENS */}
      <section className="space-y-4">
        <div className="border-b border-[#D1CCC2] pb-2">
          <span className="text-[10px] font-mono uppercase text-[#4B5563]">APPLICATION FOUNDATIONS</span>
          <h2 className="text-lg font-semibold text-[#111827]">Core Application Controls</h2>
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
      </section>
    </div>
  );
}

