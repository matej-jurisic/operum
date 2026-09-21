import {
    Anchor,
    Badge,
    Box,
    Button,
    Card,
    Container,
    Grid,
    Group,
    ScrollArea,
    SimpleGrid,
    Stack,
    Table,
    Text,
    ThemeIcon,
    Title,
    UnstyledButton,
    useMantineColorScheme,
    useMantineTheme,
} from "@mantine/core";
import { observer } from "mobx-react";
import { ReactNode, useState } from "react";
import {
    TbArrowNarrowRight,
    TbBarbell,
    TbBell,
    TbBook,
    TbBoxAlignTop,
    TbCalendar,
    TbCheck,
    TbCheckbox,
    TbCommand,
    TbCompass,
    TbDeviceGamepad,
    TbFileImport,
    TbFilter,
    TbGitBranch,
    TbHeading,
    TbLayoutColumns,
    TbLayoutDashboard,
    TbMinus,
    TbMovie,
    TbNote,
    TbSeparatorHorizontal,
    TbSquareRoundedPlus,
    TbTable,
    TbVariable,
    TbWallet,
} from "react-icons/tb";
import { Link, useNavigate } from "react-router-dom";
import AuthDialog from "../../auth/components/AuthDialog";
import HomeNavbar from "../components/HomeNavbar";
import { HOME_SECTIONS } from "../constants/homeSections";
import { readDefaultPage } from "../../../shared/constants/defaultPage";
import globalStore from "../../../shared/stores/GlobalStore";
import "./Home.css";

// ─── Sample data ──────────────────────────────────────────────────────────────

// The example running tracker the diagrams are drawn from. It is sample data for an
// illustration, not a reproduction of any screen in the app. Kept small on purpose: the
// views diagram renders every row, so a longer set would crowd the section.

const SCHEMA_FIELDS = [
    { name: "Date", type: "date" },
    { name: "Distance", type: "number" },
    { name: "Duration", type: "timespan" },
    { name: "Terrain", type: "string" },
    { name: "Shoes", type: "reference" },
];

type RunRow = {
    date: string;
    distance: number;
    duration: string;
    terrain: string;
    speed: string;
};

type RunColumn = keyof RunRow;

const RUN_COLUMN_LABELS: Record<RunColumn, string> = {
    date: "Date",
    distance: "Distance",
    duration: "Duration",
    terrain: "Terrain",
    speed: "Speed",
};

const RUNS: RunRow[] = [
    { date: "Jun 24", distance: 8.2, duration: "42:10", terrain: "Road", speed: "11.7" },
    { date: "Jun 22", distance: 21.1, duration: "1:52:40", terrain: "Road", speed: "11.2" },
    { date: "Jun 19", distance: 6.5, duration: "38:05", terrain: "Trail", speed: "10.2" },
    { date: "Jun 15", distance: 16, duration: "1:31:20", terrain: "Trail", speed: "10.5" },
    { date: "May 30", distance: 18.4, duration: "1:40:15", terrain: "Road", speed: "11.0" },
];

// Saved views the diagram switches between. Each keeps its own clauses and columns, and
// its rows are what those clauses would return from RUNS.
const VIEWS: {
    name: string;
    clauses: { kind: "filter" | "sort"; label: string }[];
    columns: RunColumn[];
    rows: RunRow[];
}[] = [
    {
        name: "All runs",
        clauses: [{ kind: "sort", label: "Date, newest first" }],
        columns: ["date", "distance", "duration", "terrain"],
        rows: RUNS,
    },
    {
        name: "This month",
        clauses: [
            { kind: "filter", label: "Date ≥ Start of month" },
            { kind: "sort", label: "Date, newest first" },
        ],
        columns: ["date", "distance", "speed"],
        rows: RUNS.filter((run) => run.date.startsWith("Jun")),
    },
    {
        name: "Long runs",
        clauses: [
            { kind: "filter", label: "Distance ≥ 15" },
            { kind: "sort", label: "Distance, highest first" },
        ],
        columns: ["date", "distance", "duration", "speed"],
        rows: RUNS.filter((run) => run.distance >= 15).sort(
            (a, b) => b.distance - a.distance,
        ),
    },
    {
        name: "Trails",
        clauses: [{ kind: "filter", label: "Terrain = Trail" }],
        columns: ["date", "distance", "terrain"],
        rows: RUNS.filter((run) => run.terrain === "Trail"),
    },
];

const MAPPINGS = [
    { source: "Resting HR", field: "Resting HR" },
    { source: "Sleep time", field: "Sleep" },
    { source: "HRV", field: "HRV" },
    { source: "Weight", field: "Body Weight" },
];

const RULES = [
    {
        when: "An entry is added to Expenses",
        condition: "Amount is over $200",
        then: "Push notification and an inbox item",
        sample: "1 new entry: Groceries, $214.60",
    },
    {
        when: "Every Sunday at 20:00",
        condition: "This week's total is over $500",
        then: "Push notification and an inbox item",
        sample: "Weekly spend: $612.40",
    },
];

const PERMISSIONS = ["View", "Edit data", "Edit schema", "Settings"];

const PEOPLE = [
    { name: "you", color: "blue", role: "owner", granted: 4 },
    { name: "alex", color: "indigo", role: "", granted: 3 },
    { name: "sam", color: "pink", role: "", granted: 2 },
    { name: "jordan", color: "orange", role: "", granted: 1 },
];

const USE_CASES = [
    { icon: <TbBook size={14} />, color: "indigo", label: "Reading list" },
    { icon: <TbBarbell size={14} />, color: "teal", label: "Workout log" },
    { icon: <TbWallet size={14} />, color: "green", label: "Expenses" },
    { icon: <TbCheckbox size={14} />, color: "grape", label: "Habits" },
    { icon: <TbDeviceGamepad size={14} />, color: "orange", label: "Game library" },
    { icon: <TbMovie size={14} />, color: "red", label: "Watchlist" },
];

// Every widget kind a board can hold, named and grouped by what it is for. Names only:
// the section's job is to show the range of pieces, not to document each one.
const WIDGET_GROUPS = [
    {
        label: "Data",
        items: [
            { icon: <TbLayoutDashboard size={14} />, color: "indigo", label: "Charts" },
            { icon: <TbVariable size={14} />, color: "violet", label: "Calculations" },
            { icon: <TbCalendar size={14} />, color: "blue", label: "Calendar" },
            { icon: <TbTable size={14} />, color: "orange", label: "Entries" },
        ],
    },
    {
        label: "Input",
        items: [
            { icon: <TbSquareRoundedPlus size={14} />, color: "teal", label: "Quick add" },
            { icon: <TbFilter size={14} />, color: "grape", label: "Filter" },
        ],
    },
    {
        label: "Layout",
        items: [
            { icon: <TbHeading size={14} />, color: "cyan", label: "Header" },
            { icon: <TbNote size={14} />, color: "pink", label: "Note" },
            { icon: <TbSeparatorHorizontal size={14} />, color: "gray", label: "Divider" },
            { icon: <TbBoxAlignTop size={14} />, color: "red", label: "Container" },
            { icon: <TbLayoutColumns size={14} />, color: "yellow", label: "Tabs" },
        ],
    },
];

const EXTRAS = [
    {
        icon: <TbCompass size={20} />,
        color: "indigo",
        title: "Explore",
        text: "Run a one-off calculation over any tracker, then keep it as a widget if it earns a spot.",
    },
    {
        icon: <TbFileImport size={20} />,
        color: "teal",
        title: "CSV import and export",
        text: "Bring in rows you already have, or export a tracker filtered to one of its views.",
    },
    {
        icon: <TbGitBranch size={20} />,
        color: "grape",
        title: "Extract to a tracker",
        text: "Move values that repeat across entries into a tracker of their own, linked back by reference.",
    },
    {
        icon: <TbCommand size={20} />,
        color: "orange",
        title: "Command palette",
        text: "Press Ctrl+K or Cmd+K to jump to any tracker or dashboard, or start a new one.",
    },
];

// ─── Diagram pieces ───────────────────────────────────────────────────────────

// A diagram stands in for the product mock a marketing page would usually carry. It is
// labelled as a diagram and drawn with plain boxes and connectors, so it explains the
// idea without implying the app looks like this. Every section's right column is one of
// these, at a shared width and reserved height, so they read as a series.
// The label names what the diagram shows. It is dropped where the section's own heading
// and description already say it, rather than captioning the same thing a third time.
function Diagram({ label, children }: { label?: string; children: ReactNode }) {
    return (
        <figure className="diagram">
            {label && (
                <figcaption className="diagram-label">{label}</figcaption>
            )}
            <div className="diagram-body">{children}</div>
        </figure>
    );
}

function Node({
    name,
    type,
    accent,
    children,
}: {
    name: string;
    type?: string;
    accent?: boolean;
    children?: ReactNode;
}) {
    return (
        <div className="node" data-accent={accent || undefined}>
            <Box miw={0} style={{ flex: 1 }}>
                <Text size="sm" fw={500}>
                    {name}
                </Text>
                {children}
            </Box>
            {type && <span className="node-type">{type}</span>}
        </div>
    );
}

function Connector({ label }: { label: string }) {
    return <div className="connector">{label}</div>;
}

// A row of labelled pills, used for both the hero's example trackers and the widget kinds.
function Chips({
    items,
}: {
    items: { icon: ReactNode; color: string; label: string }[];
}) {
    return (
        <Group gap="xs">
            {items.map((item) => (
                <div key={item.label} className="home-chip">
                    <ThemeIcon
                        size={24}
                        radius="xl"
                        variant="light"
                        color={item.color}
                    >
                        {item.icon}
                    </ThemeIcon>
                    <Text size="sm" fw={500}>
                        {item.label}
                    </Text>
                </div>
            ))}
        </Group>
    );
}

// ─── CTA buttons ─────────────────────────────────────────────────────────────

function CtaButtons({
    onAuthOpen,
}: {
    onAuthOpen: (tab: "login" | "register") => void;
}) {
    const navigate = useNavigate();
    if (globalStore.currentUser) {
        return (
            <Button size="lg" onClick={() => navigate(readDefaultPage())}>
                Open Operum
            </Button>
        );
    }
    return (
        <Group gap="sm">
            <Button size="lg" onClick={() => onAuthOpen("register")}>
                Get Started
            </Button>
            <Button size="lg" variant="outline" onClick={() => onAuthOpen("login")}>
                Sign In
            </Button>
        </Group>
    );
}

// ─── Page structure ───────────────────────────────────────────────────────────

// Copy left, diagram right, on every section without exception. The fixed axis is the
// point: alternating sides made the page read as noise rather than a rhythm.
function FeatureRow({
    id,
    title,
    description,
    diagram,
}: {
    id: string;
    title: string;
    description?: string;
    diagram: ReactNode;
}) {
    return (
        <Box id={id} className="home-section">
            <Grid gutter={{ base: 32, md: 64 }} align="center">
                <Grid.Col span={{ base: 12, md: 5 }}>
                    <Title order={2} mb="md">
                        {title}
                    </Title>
                    {description && (
                        <Text c="dimmed" lh={1.7}>
                            {description}
                        </Text>
                    )}
                </Grid.Col>
                <Grid.Col span={{ base: 12, md: 7 }}>{diagram}</Grid.Col>
            </Grid>
        </Box>
    );
}

// ─── Diagrams ─────────────────────────────────────────────────────────────────

// A tracker's fields, with the calculated one shown as the product of the two it reads.
// Each node carries its own type, which is what the list of field types used to say.
function SchemaDiagram() {
    return (
        <Diagram label="A tracker's fields">
            {SCHEMA_FIELDS.map((field) => (
                <Node key={field.name} name={field.name} type={field.type} />
            ))}
            <Connector label="Distance and Duration feed" />
            <Node name="Speed" type="calculated" accent>
                <code className="formula">{"{Distance} / {Duration.hours}"}</code>
            </Node>
        </Diagram>
    );
}

const formatRunCell = (run: RunRow, column: RunColumn) => {
    if (column === "distance") return `${run.distance.toFixed(1)} km`;
    if (column === "speed") return `${run.speed} km/h`;
    return run[column];
};

// Picking a view swaps the clauses, the columns, and the rows they return. Every view's
// result stays stacked in one grid cell so the diagram never changes height.
function ViewsDiagram() {
    const [active, setActive] = useState(2);

    return (
        <Diagram label="One tracker, four saved views">
            <div className="view-pills">
                {VIEWS.map((view, i) => (
                    <UnstyledButton
                        key={view.name}
                        className="view-pill"
                        data-active={i === active || undefined}
                        aria-pressed={i === active}
                        onClick={() => setActive(i)}
                    >
                        {view.name}
                    </UnstyledButton>
                ))}
            </div>
            <div className="swap">
                {VIEWS.map((view, i) => (
                    <div
                        key={view.name}
                        className="swap-item"
                        data-active={i === active || undefined}
                        aria-hidden={i !== active}
                    >
                        <Stack gap={8}>
                            <Group gap={6}>
                                {view.clauses.map((clause) => (
                                    <Badge
                                        key={clause.label}
                                        variant="light"
                                        color={
                                            clause.kind === "filter"
                                                ? "blue"
                                                : "gray"
                                        }
                                        radius="sm"
                                        tt="none"
                                        fw={500}
                                    >
                                        {clause.label}
                                    </Badge>
                                ))}
                            </Group>
                            <Text size="xs" c="dimmed">
                                {view.rows.length} of {RUNS.length} entries,{" "}
                                {view.columns.length} columns
                            </Text>
                            <Table fz="xs" verticalSpacing={6} horizontalSpacing="xs">
                                <Table.Thead>
                                    <Table.Tr>
                                        {view.columns.map((column) => (
                                            <Table.Th key={column}>
                                                {RUN_COLUMN_LABELS[column]}
                                            </Table.Th>
                                        ))}
                                    </Table.Tr>
                                </Table.Thead>
                                <Table.Tbody>
                                    {view.rows.map((run) => (
                                        <Table.Tr key={run.date}>
                                            {view.columns.map((column) => (
                                                <Table.Td key={column}>
                                                    {formatRunCell(run, column)}
                                                </Table.Td>
                                            ))}
                                        </Table.Tr>
                                    ))}
                                </Table.Tbody>
                            </Table>
                        </Stack>
                    </div>
                ))}
            </div>
        </Diagram>
    );
}

// The widget kinds a board can hold, grouped by what they are for.
function WidgetsDiagram() {
    return (
        <Diagram label="Widget kinds">
            <Stack gap="md">
                {WIDGET_GROUPS.map((group) => (
                    <Stack key={group.label} gap={8}>
                        <Text
                            size="xs"
                            fw={600}
                            tt="uppercase"
                            c="dimmed"
                            style={{ letterSpacing: "0.06em" }}
                        >
                            {group.label}
                        </Text>
                        <Chips items={group.items} />
                    </Stack>
                ))}
            </Stack>
        </Diagram>
    );
}

// A connection, the tracker it feeds, and which of its values fill which fields.
function IntegrationsDiagram() {
    return (
        <Diagram label="A connection filling a tracker's fields">
            <Node name="intervals.icu" type="pull" />
            <Connector label="On a schedule, mapped field by field" />
            <div className="node">
                <Box miw={0} style={{ flex: 1 }}>
                    <Text size="sm" fw={500} mb={8}>
                        Wellness
                    </Text>
                    <Stack gap={6}>
                        {MAPPINGS.map((mapping) => (
                            <div key={mapping.source} className="mapping">
                                <span>{mapping.source}</span>
                                <TbArrowNarrowRight size={14} />
                                <span data-target>{mapping.field}</span>
                            </div>
                        ))}
                    </Stack>
                </Box>
            </div>
            <Connector label="A second connection, pushed in by webhook" />
            <Node name="Firefly III" type="push">
                <Text size="xs" c="dimmed">
                    Transactions into Expenses, 3 fields mapped
                </Text>
            </Node>
        </Diagram>
    );
}

// Two alert rules read as sentences: what starts the check, what has to be true, what happens.
function RulesDiagram() {
    const theme = useMantineTheme();

    return (
        <Diagram label="Two alert rules">
            {RULES.map((rule) => (
                <div key={rule.when} className="rule">
                    <span className="rule-keyword">WHEN</span>
                    <Text size="sm">{rule.when}</Text>
                    <span className="rule-keyword">IF</span>
                    <Text size="sm">{rule.condition}</Text>
                    <span className="rule-keyword">THEN</span>
                    <Box>
                        <Text size="sm">{rule.then}</Text>
                        <Group gap={6} wrap="nowrap" mt={6}>
                            <ThemeIcon
                                size={20}
                                radius="sm"
                                variant="light"
                                color={theme.primaryColor}
                            >
                                <TbBell size={12} />
                            </ThemeIcon>
                            <Text size="xs" c="dimmed" truncate>
                                {rule.sample}
                            </Text>
                        </Group>
                    </Box>
                </div>
            ))}
        </Diagram>
    );
}

// Who can do what on a shared tracker. Everyone can view; the owner keeps the last column.
function PermissionsDiagram() {
    return (
        <Diagram label="Who can do what on a shared tracker">
            <div className="matrix">
                <div />
                {PERMISSIONS.map((permission) => (
                    <div key={permission} className="matrix-head">
                        {permission}
                    </div>
                ))}
                {PEOPLE.map((person) => (
                    <div key={person.name} className="matrix-row">
                        <div className="matrix-name">
                            <ThemeIcon
                                size={26}
                                radius="xl"
                                variant="light"
                                color={person.color}
                            >
                                <Text size="xs" fw={700}>
                                    {person.name.charAt(0).toUpperCase()}
                                </Text>
                            </ThemeIcon>
                            <Box miw={0}>
                                <Text size="sm" fw={500} truncate>
                                    {person.name}
                                </Text>
                                {person.role && (
                                    <Text size="xs" c="dimmed">
                                        {person.role}
                                    </Text>
                                )}
                            </Box>
                        </div>
                        {PERMISSIONS.map((permission, i) => (
                            <div key={permission} className="matrix-cell">
                                {i < person.granted ? (
                                    <Box c="green" style={{ display: "flex" }}>
                                        <TbCheck size={16} />
                                    </Box>
                                ) : (
                                    <Box c="dimmed" style={{ display: "flex" }}>
                                        <TbMinus size={16} />
                                    </Box>
                                )}
                            </div>
                        ))}
                    </div>
                ))}
            </div>
        </Diagram>
    );
}

function ExtrasDiagram() {
    return (
        <Diagram>
            <SimpleGrid cols={{ base: 1, xs: 2 }} spacing="lg" verticalSpacing="lg">
                {EXTRAS.map((extra) => (
                    <Stack key={extra.title} gap={6}>
                        <ThemeIcon
                            size={34}
                            radius="md"
                            variant="light"
                            color={extra.color}
                        >
                            {extra.icon}
                        </ThemeIcon>
                        <Text fw={600} size="sm">
                            {extra.title}
                        </Text>
                        <Text size="sm" c="dimmed" lh={1.5}>
                            {extra.text}
                        </Text>
                    </Stack>
                ))}
            </SimpleGrid>
        </Diagram>
    );
}

// ─── Section copy ─────────────────────────────────────────────────────────────

// Keyed by the ids in HOME_SECTIONS, so that one list drives both the nav links and the
// page body and they cannot drift apart.
const SECTION_COPY: Record<
    string,
    { title: string; description?: string; diagram: ReactNode }
> = {
    trackers: {
        title: "Trackers",
        description:
            "A tracker is a table you design. Give it the columns you need: numbers, text, dates, a link to a row in another tracker, or a formula.",
        diagram: <SchemaDiagram />,
    },
    views: {
        title: "Views",
        description:
            "A view is a saved filter, sort, and column choice.",
        diagram: <ViewsDiagram />,
    },
    widgets: {
        title: "Widgets",
        description:
            "A dashboard is a grid of widgets you drag into place, with separate desktop and mobile layouts.",
        diagram: <WidgetsDiagram />,
    },
    integrations: {
        title: "Integrations",
        description:
            "Connect a service, pick a tracker, and choose which of its values fill which fields.",
        diagram: <IntegrationsDiagram />,
    },
    notifications: {
        title: "Notifications",
        description:
            "Alert rules watch a tracker's entries or a chart's value, on a schedule or the moment a condition turns true.",
        diagram: <RulesDiagram />,
    },
    collaboration: {
        title: "Collaboration",
        description:
            "Share a tracker and pick what each person can do: view it, edit the entries, or change the fields too.",
        diagram: <PermissionsDiagram />,
    },
    more: {
        title: "More",
        diagram: <ExtrasDiagram />,
    },
};

// ─── Home ─────────────────────────────────────────────────────────────────────

const Home = observer(() => {
    const theme = useMantineTheme();
    const { colorScheme } = useMantineColorScheme();
    const [scrolled, setScrolled] = useState(false);
    const [authTab, setAuthTab] = useState<"login" | "register" | null>(null);

    const isDark = colorScheme === "dark";

    const titleGradient = `linear-gradient(135deg, var(--mantine-color-${theme.primaryColor}-6) 0%, var(--mantine-color-cyan-5) 100%)`;

    const heroBg = isDark
        ? `radial-gradient(ellipse 140% 55% at 50% 0%, color-mix(in srgb, var(--mantine-color-${theme.primaryColor}-9) 80%, transparent) 0%, transparent 100%)`
        : `radial-gradient(ellipse 140% 55% at 50% 0%, color-mix(in srgb, var(--mantine-color-${theme.primaryColor}-2) 100%, transparent) 0%, transparent 100%)`;

    const dotPattern = isDark
        ? "radial-gradient(circle, rgba(255,255,255,0.07) 1px, transparent 1px)"
        : "radial-gradient(circle, rgba(0,0,0,0.08) 1px, transparent 1px)";

    const scrollTo = (id: string) =>
        document.getElementById(id)?.scrollIntoView({ behavior: "smooth" });

    return (
        <>
            <HomeNavbar
                scrolled={scrolled}
                scrollTo={scrollTo}
                onAuthOpen={setAuthTab}
            />

            <Box style={{ position: "relative", height: "100%", background: heroBg }}>
                {/* Dot grid */}
                <Box
                    style={{
                        position: "absolute",
                        inset: 0,
                        backgroundImage: dotPattern,
                        backgroundSize: "28px 28px",
                        pointerEvents: "none",
                        zIndex: 0,
                    }}
                />

                <ScrollArea
                    scrollbars="y"
                    style={{ height: "100%", position: "relative", zIndex: 1 }}
                    styles={{
                        root: { background: "transparent" },
                        viewport: { background: "transparent" },
                    }}
                    onScrollPositionChange={({ y }) => setScrolled(y > 20)}
                >
                    {/* ── Hero ──────────────────────────────────────────── */}
                    <Box id="hero" style={{ scrollMarginTop: "60px" }}>
                        <Container size="lg" pt={140} pb={100}>
                            <Stack align="center" gap="xl">
                                <Badge
                                    size="lg"
                                    variant="light"
                                    radius="xl"
                                    color={theme.primaryColor}
                                >
                                    Flexible Data Tracking
                                </Badge>

                                <Title
                                    order={1}
                                    ta="center"
                                    style={{
                                        fontSize: "clamp(2.5rem, 6vw, 4.5rem)",
                                        fontWeight: 900,
                                        lineHeight: 1.1,
                                        backgroundImage: titleGradient,
                                        WebkitBackgroundClip: "text",
                                        WebkitTextFillColor: "transparent",
                                        backgroundClip: "text",
                                        paddingBottom: "0.1em",
                                    }}
                                >
                                    Track Anything,
                                    <br />
                                    Your Way
                                </Title>

                                <Text
                                    size="xl"
                                    c="dimmed"
                                    maw={560}
                                    ta="center"
                                    lh={1.6}
                                >
                                    Create a tracker with the fields you want,
                                    log entries against it, and build dashboards
                                    from what you have logged.
                                </Text>

                                <CtaButtons onAuthOpen={setAuthTab} />

                                <Stack gap="sm" align="center" mt="lg">
                                    <Text size="sm" c="dimmed">
                                        For example
                                    </Text>
                                    <Group justify="center" maw={640}>
                                        <Chips items={USE_CASES} />
                                    </Group>
                                </Stack>
                            </Stack>
                        </Container>
                    </Box>

                    {/* ── Features ──────────────────────────────────────── */}
                    <Container size="lg" py={80}>
                        <Stack gap={80}>
                            {HOME_SECTIONS.map((section) => (
                                <FeatureRow
                                    key={section.id}
                                    id={section.id}
                                    {...SECTION_COPY[section.id]}
                                />
                            ))}
                        </Stack>
                    </Container>

                    {/* ── CTA ───────────────────────────────────────────── */}
                    {!globalStore.currentUser && (
                        <Container size="lg" py={80}>
                            <Card
                                padding="xl"
                                radius="lg"
                                style={{
                                    background: isDark
                                        ? `linear-gradient(135deg, color-mix(in srgb, var(--mantine-color-${theme.primaryColor}-9) 60%, var(--mantine-color-dark-7)) 0%, var(--mantine-color-dark-7) 100%)`
                                        : `linear-gradient(135deg, var(--mantine-color-${theme.primaryColor}-0) 0%, var(--mantine-color-cyan-0) 100%)`,
                                    border: `1px solid var(--mantine-color-${theme.primaryColor}-${isDark ? "8" : "2"})`,
                                }}
                            >
                                <Stack align="center" gap="lg" py="xl">
                                    <Title order={2} ta="center">
                                        Create an account
                                    </Title>
                                    <Text
                                        size="lg"
                                        c="dimmed"
                                        ta="center"
                                        maw={480}
                                    >
                                        Build your first tracker and start
                                        logging entries.
                                    </Text>
                                    <CtaButtons onAuthOpen={setAuthTab} />
                                </Stack>
                            </Card>
                        </Container>
                    )}
                    {/* ── Footer ───────────────────────────────────────── */}
                    <Box
                        style={{
                            borderTop: "1px solid var(--mantine-color-default-border)",
                        }}
                    >
                        <Container size="lg" py="lg">
                            <Group justify="space-between" align="center">
                                <Text size="sm" c="dimmed">
                                    © {new Date().getFullYear()} Operum. All rights reserved.
                                </Text>
                                <Group gap="lg">
                                    <Anchor
                                        component={Link}
                                        to="/terms"
                                        size="sm"
                                        c="dimmed"
                                        underline="hover"
                                    >
                                        Terms of Service
                                    </Anchor>
                                    <Anchor
                                        component={Link}
                                        to="/privacy"
                                        size="sm"
                                        c="dimmed"
                                        underline="hover"
                                    >
                                        Privacy Policy
                                    </Anchor>
                                </Group>
                            </Group>
                        </Container>
                    </Box>
                </ScrollArea>
            </Box>

            {authTab && (
                <AuthDialog
                    initialTab={authTab}
                    onClose={() => setAuthTab(null)}
                />
            )}
        </>
    );
});

export default Home;
