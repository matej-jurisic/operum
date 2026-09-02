import { BarChart, DonutChart, LineChart, ScatterChart } from "@mantine/charts";
import {
    Anchor,
    Badge,
    Box,
    Button,
    Card,
    Container,
    Divider,
    Grid,
    Group,
    List,
    ScrollArea,
    SimpleGrid,
    Stack,
    Text,
    ThemeIcon,
    Title,
    useMantineColorScheme,
    useMantineTheme,
} from "@mantine/core";
import { observer } from "mobx-react";
import { useState } from "react";
import {
    TbAdjustmentsHorizontal,
    TbAlphabetLatin,
    TbArrowsJoin,
    TbBarbell,
    TbBook,
    TbCalendar,
    TbCalendarClock,
    TbChartBar,
    TbCheckbox,
    TbClock,
    TbCrown,
    TbDatabase,
    TbDeviceGamepad,
    TbEdit,
    TbEye,
    TbFilter,
    TbHash,
    TbHeading,
    TbLayoutDashboard,
    TbLayoutGrid,
    TbMicroscope,
    TbMovie,
    TbPlug,
    TbPlus,
    TbRefresh,
    TbTable,
    TbToggleRight,
    TbUsers,
    TbVariable,
    TbWallet,
    TbWebhook,
} from "react-icons/tb";
import { Link, useNavigate } from "react-router-dom";
import AuthDialog from "../../auth/components/AuthDialog";
import HomeNavbar from "../components/HomeNavbar";
import { readDefaultPage } from "../../../shared/constants/defaultPage";
import globalStore from "../../../shared/stores/GlobalStore";

// ─── Mock chart data ──────────────────────────────────────────────────────────

const LINE_DATA = [
    { month: "Jan", value: 18 },
    { month: "Feb", value: 27 },
    { month: "Mar", value: 23 },
    { month: "Apr", value: 41 },
    { month: "May", value: 35 },
    { month: "Jun", value: 52 },
    { month: "Jul", value: 48 },
];

const BAR_DATA = [
    { day: "Mon", count: 8 },
    { day: "Tue", count: 14 },
    { day: "Wed", count: 9 },
    { day: "Thu", count: 17 },
    { day: "Fri", count: 13 },
    { day: "Sat", count: 6 },
    { day: "Sun", count: 4 },
];

const DONUT_DATA = [
    { name: "Work", value: 35, color: "blue.6" },
    { name: "Personal", value: 28, color: "teal.5" },
    { name: "Health", value: 22, color: "grape.5" },
    { name: "Other", value: 15, color: "orange.5" },
];

const SCATTER_DATA = [
    {
        name: "Entries",
        color: "blue.5",
        data: [
            { x: 2, y: 12 },
            { x: 4, y: 18 },
            { x: 3, y: 15 },
            { x: 7, y: 28 },
            { x: 5, y: 22 },
            { x: 8, y: 34 },
            { x: 6, y: 26 },
            { x: 9, y: 38 },
            { x: 10, y: 42 },
            { x: 1, y: 8 },
            { x: 11, y: 45 },
            { x: 12, y: 50 },
        ],
    },
];

// ─── Section data ─────────────────────────────────────────────────────────────

const FEATURES = [
    {
        icon: <TbLayoutGrid size={22} />,
        color: "indigo",
        title: "Custom Trackers",
        description:
            "Define any schema with your choice of field types. Color-code and name them freely.",
    },
    {
        icon: <TbVariable size={22} />,
        color: "blue",
        title: "Calculated Fields",
        description:
            "Derive values automatically with formula syntax. Reference any field using {FieldName}.",
    },
    {
        icon: <TbDatabase size={22} />,
        color: "cyan",
        title: "Constants",
        description:
            "Named reusable values with up to 6 conditional variants. Perfect for dynamic rates.",
    },
    {
        icon: <TbFilter size={22} />,
        color: "teal",
        title: "Saved Views",
        description:
            "Persist filter, sort, and column combinations. Dynamic dates like 'start of month' resolve at query time.",
    },
    {
        icon: <TbLayoutDashboard size={22} />,
        color: "green",
        title: "Dashboards",
        description:
            "Arrange charts, tables, and quick-add buttons on boards. Separate desktop and mobile layouts.",
    },
    {
        icon: <TbChartBar size={22} />,
        color: "orange",
        title: "Analytics",
        description:
            "29 chart variants across 6 chart types. Combine trackers on one axis or scope a chart to a view.",
    },
    {
        icon: <TbPlug size={22} />,
        color: "grape",
        title: "Integrations",
        description:
            "Pull data from intervals.icu and Firefly III straight onto tracker fields you map.",
    },
    {
        icon: <TbUsers size={22} />,
        color: "red",
        title: "Collaboration",
        description:
            "Share trackers with teammates using fine-grained read and write permissions.",
    },
];

const DATA_TYPES = [
    {
        icon: <TbHash size={20} />,
        color: "blue",
        label: "Number",
        description: "Integer and decimal values with full math support.",
    },
    {
        icon: <TbAlphabetLatin size={20} />,
        color: "teal",
        label: "String",
        description: "Free text or predefined select options.",
    },
    {
        icon: <TbToggleRight size={20} />,
        color: "grape",
        label: "Boolean",
        description: "True / false values for yes/no tracking.",
    },
    {
        icon: <TbCalendar size={20} />,
        color: "orange",
        label: "Date",
        description: "Calendar date without time, filterable and sortable.",
    },
    {
        icon: <TbCalendarClock size={20} />,
        color: "red",
        label: "DateTime",
        description: "Full timestamp with date and time components.",
    },
    {
        icon: <TbClock size={20} />,
        color: "indigo",
        label: "TimeSpan",
        description: "Duration values for tracking time spent.",
    },
];

const USE_CASES = [
    {
        icon: <TbBook size={22} />,
        color: "indigo",
        title: "Reading List",
        description: "Track books, ratings, and reading progress over time.",
    },
    {
        icon: <TbBarbell size={22} />,
        color: "teal",
        title: "Workout Log",
        description: "Log exercises, sets, reps, and weight each session.",
    },
    {
        icon: <TbWallet size={22} />,
        color: "green",
        title: "Expense Tracker",
        description: "Monitor spending by category, date, and amount.",
    },
    {
        icon: <TbCheckbox size={22} />,
        color: "grape",
        title: "Habit Tracker",
        description: "Build streaks with daily boolean check-ins.",
    },
    {
        icon: <TbDeviceGamepad size={22} />,
        color: "orange",
        title: "Game Library",
        description: "Catalog games with ratings, playtime, and status.",
    },
    {
        icon: <TbMovie size={22} />,
        color: "red",
        title: "Movie Watchlist",
        description: "Track films and shows with scores and notes.",
    },
];

const ANALYTICS_CHARTS = [
    { title: "Trend Analysis", subtitle: "Track values changing over time" },
    { title: "Period Comparison", subtitle: "Compare totals across categories" },
    { title: "Distribution", subtitle: "See proportions at a glance" },
    { title: "Correlation", subtitle: "Find relationships between fields" },
];

const WIDGET_TYPES = [
    {
        icon: <TbChartBar size={20} />,
        color: "indigo",
        label: "Charts",
        description:
            "Any chart from the Widget Library, built once and placed on as many boards as you want.",
    },
    {
        icon: <TbTable size={20} />,
        color: "blue",
        label: "Entries tables",
        description:
            "A tracker's most recent rows with the columns you choose.",
    },
    {
        icon: <TbPlus size={20} />,
        color: "teal",
        label: "Quick-add buttons",
        description:
            "Open a tracker's quick-add entry dialog straight from the board.",
    },
    {
        icon: <TbAdjustmentsHorizontal size={20} />,
        color: "green",
        label: "Filters",
        description:
            "Live controls that narrow the charts and tables set to follow them.",
    },
    {
        icon: <TbHeading size={20} />,
        color: "grape",
        label: "Headers and notes",
        description: "Section titles, dividers, and free-form text.",
    },
    {
        icon: <TbArrowsJoin size={20} />,
        color: "orange",
        label: "Combined charts",
        description:
            "Line and bar charts that draw several trackers on one shared axis.",
    },
];

const PROVIDERS = [
    {
        icon: <TbRefresh size={22} />,
        color: "blue",
        title: "intervals.icu",
        detail: "Pulled on a schedule with an API key.",
        abilities: [
            "Daily wellness snapshots",
            "Activities",
            "Fans in Garmin, Strava, Wahoo, and more",
        ],
    },
    {
        icon: <TbWebhook size={22} />,
        color: "teal",
        title: "Firefly III",
        detail: "Pushed by webhook from your self-hosted instance.",
        abilities: [
            "Transactions as they happen",
            "Signed deliveries with a rotatable secret",
            "Point it at your own instance URL",
        ],
    },
];

const COLLAB_ROLES = [
    {
        icon: <TbEye size={22} />,
        color: "blue",
        title: "View Only",
        abilities: [
            "View all entries",
            "Access saved views",
            "See analytics",
        ],
    },
    {
        icon: <TbEdit size={22} />,
        color: "teal",
        title: "Edit Data",
        abilities: [
            "Add and edit entries",
            "Delete entries",
            "Cannot modify schema",
        ],
    },
    {
        icon: <TbMicroscope size={22} />,
        color: "grape",
        title: "Edit Schema",
        abilities: [
            "Create and edit fields",
            "Manage views and constants",
            "Cannot manage collaborators",
        ],
    },
    {
        icon: <TbCrown size={22} />,
        color: "orange",
        title: "Owner",
        abilities: [
            "Full access to everything",
            "Add and remove collaborators",
            "Delete the tracker",
        ],
    },
];

// ─── Reusable section header ──────────────────────────────────────────────────

function SectionHeader({
    eyebrow,
    title,
    subtitle,
    primaryColor,
}: {
    eyebrow: string;
    title: string;
    subtitle?: string;
    primaryColor: string;
}) {
    return (
        <Stack gap={8} align="center">
            <Text
                size="sm"
                fw={700}
                tt="uppercase"
                c={primaryColor}
                style={{ letterSpacing: "0.08em" }}
            >
                {eyebrow}
            </Text>
            <Title order={2} ta="center">
                {title}
            </Title>
            {subtitle && (
                <Text size="md" c="dimmed" ta="center" maw={520}>
                    {subtitle}
                </Text>
            )}
        </Stack>
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
            <Button
                size="lg"
                variant="outline"
                onClick={() => onAuthOpen("login")}
            >
                Sign In
            </Button>
        </Group>
    );
}

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

    const altBg = isDark
        ? "var(--mantine-color-dark-8)"
        : "var(--mantine-color-gray-0)";

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
                                        fontSize:
                                            "clamp(2.5rem, 6vw, 4.5rem)",
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
                                    Define custom schemas, log entries, build
                                    saved views, and assemble dashboards that
                                    visualize your data, without writing a
                                    single line of code.
                                </Text>

                                <CtaButtons onAuthOpen={setAuthTab} />

                                <Divider w="100%" maw={500} opacity={0.3} />

                                <SimpleGrid cols={3} spacing={40}>
                                    {[
                                        {
                                            value: "6",
                                            label: "Data Types",
                                        },
                                        {
                                            value: "29",
                                            label: "Chart Variants",
                                        },
                                        {
                                            value: "∞",
                                            label: "Combinations",
                                        },
                                    ].map((stat) => (
                                        <Stack
                                            key={stat.label}
                                            align="center"
                                            gap={4}
                                        >
                                            <Text
                                                style={{
                                                    fontSize: "2.2rem",
                                                    fontWeight: 900,
                                                    lineHeight: 1,
                                                    backgroundImage:
                                                        titleGradient,
                                                    WebkitBackgroundClip:
                                                        "text",
                                                    WebkitTextFillColor:
                                                        "transparent",
                                                    backgroundClip: "text",
                                                }}
                                            >
                                                {stat.value}
                                            </Text>
                                            <Text size="sm" c="dimmed" ta="center">
                                                {stat.label}
                                            </Text>
                                        </Stack>
                                    ))}
                                </SimpleGrid>
                            </Stack>
                        </Container>
                    </Box>

                    {/* ── Features ──────────────────────────────────────── */}
                    <Box
                        id="features"
                        style={{ background: altBg, scrollMarginTop: "60px" }}
                    >
                        <Container size="lg" py={80}>
                            <Stack gap={48}>
                                <SectionHeader
                                    eyebrow="Features"
                                    title="Everything you need"
                                    subtitle="All the building blocks to capture, filter, and understand any kind of data."
                                    primaryColor={theme.primaryColor}
                                />
                                <Grid>
                                    {FEATURES.map((f) => (
                                        <Grid.Col
                                            key={f.title}
                                            span={{ base: 6, sm: 6, md: 3 }}
                                        >
                                            <Card
                                                withBorder
                                                radius="md"
                                                p="lg"
                                                h="100%"
                                                style={{
                                                    borderTop: `3px solid var(--mantine-color-${f.color}-5)`,
                                                }}
                                            >
                                                <Stack gap="sm">
                                                    <ThemeIcon
                                                        size={44}
                                                        radius="md"
                                                        variant="light"
                                                        color={f.color}
                                                    >
                                                        {f.icon}
                                                    </ThemeIcon>
                                                    <Text fw={600} size="sm">
                                                        {f.title}
                                                    </Text>
                                                    <Text
                                                        size="xs"
                                                        c="dimmed"
                                                        lh={1.5}
                                                    >
                                                        {f.description}
                                                    </Text>
                                                </Stack>
                                            </Card>
                                        </Grid.Col>
                                    ))}
                                </Grid>
                            </Stack>
                        </Container>
                    </Box>

                    {/* ── Data Types ────────────────────────────────────── */}
                    <Box id="data-types" style={{ scrollMarginTop: "60px" }}>
                        <Container size="lg" py={80}>
                            <Grid gutter={60} align="center">
                                <Grid.Col span={{ base: 12, md: 5 }}>
                                    <Stack gap="lg">
                                        <div>
                                            <Text
                                                size="sm"
                                                fw={700}
                                                tt="uppercase"
                                                c={theme.primaryColor}
                                                style={{
                                                    letterSpacing: "0.08em",
                                                }}
                                                mb={8}
                                            >
                                                Data Types
                                            </Text>
                                            <Title order={2} mb="md">
                                                The right type for every field
                                            </Title>
                                            <Text c="dimmed" lh={1.7}>
                                                Choose from six data types to
                                                model your data precisely.
                                                Number, TimeSpan, and Boolean
                                                fields support calculated values,
                                                derived automatically from
                                                other fields using formula
                                                syntax.
                                            </Text>
                                        </div>

                                    </Stack>
                                </Grid.Col>

                                <Grid.Col span={{ base: 12, md: 7 }}>
                                    <SimpleGrid cols={2} spacing="md">
                                        {DATA_TYPES.map((dt) => (
                                            <Card
                                                key={dt.label}
                                                withBorder
                                                radius="md"
                                                p="lg"
                                            >
                                                <Group gap="md" mb="xs">
                                                    <ThemeIcon
                                                        size={36}
                                                        radius="md"
                                                        variant="light"
                                                        color={dt.color}
                                                    >
                                                        {dt.icon}
                                                    </ThemeIcon>
                                                    <Text fw={600} size="sm">
                                                        {dt.label}
                                                    </Text>
                                                </Group>
                                                <Text size="xs" c="dimmed" lh={1.5}>
                                                    {dt.description}
                                                </Text>
                                            </Card>
                                        ))}
                                    </SimpleGrid>
                                </Grid.Col>
                            </Grid>
                        </Container>
                    </Box>

                    {/* ── Use Cases ─────────────────────────────────────── */}
                    <Box
                        id="use-cases"
                        style={{ background: altBg, scrollMarginTop: "60px" }}
                    >
                        <Container size="lg" py={80}>
                            <Stack gap={48}>
                                <SectionHeader
                                    eyebrow="Use Cases"
                                    title="Track anything you can imagine"
                                    subtitle="From personal habits to complex projects. If it has data, you can track it."
                                    primaryColor={theme.primaryColor}
                                />
                                <Grid>
                                    {USE_CASES.map((uc) => (
                                        <Grid.Col
                                            key={uc.title}
                                            span={{ base: 6, sm: 6, md: 4 }}
                                        >
                                            <Card
                                                withBorder
                                                radius="md"
                                                p="lg"
                                                h="100%"
                                            >
                                                <Stack gap="sm">
                                                    <ThemeIcon
                                                        size={44}
                                                        radius="md"
                                                        variant="light"
                                                        color={uc.color}
                                                    >
                                                        {uc.icon}
                                                    </ThemeIcon>
                                                    <Text fw={600} size="sm">
                                                        {uc.title}
                                                    </Text>
                                                    <Text
                                                        size="xs"
                                                        c="dimmed"
                                                        lh={1.5}
                                                    >
                                                        {uc.description}
                                                    </Text>
                                                </Stack>
                                            </Card>
                                        </Grid.Col>
                                    ))}
                                </Grid>
                            </Stack>
                        </Container>
                    </Box>

                    {/* ── Dashboards ────────────────────────────────────── */}
                    <Box id="dashboards" style={{ scrollMarginTop: "60px" }}>
                        <Container size="lg" py={80}>
                            <Stack gap={48}>
                                <SectionHeader
                                    eyebrow="Dashboards"
                                    title="Put it all on one board"
                                    subtitle="Drag widgets into place on any number of boards. Desktop and mobile layouts are kept separately."
                                    primaryColor={theme.primaryColor}
                                />
                                <Grid>
                                    {WIDGET_TYPES.map((w) => (
                                        <Grid.Col
                                            key={w.label}
                                            span={{ base: 6, sm: 6, md: 4 }}
                                        >
                                            <Card
                                                withBorder
                                                radius="md"
                                                p="lg"
                                                h="100%"
                                            >
                                                <Group gap="md" mb="xs">
                                                    <ThemeIcon
                                                        size={36}
                                                        radius="md"
                                                        variant="light"
                                                        color={w.color}
                                                    >
                                                        {w.icon}
                                                    </ThemeIcon>
                                                    <Text fw={600} size="sm">
                                                        {w.label}
                                                    </Text>
                                                </Group>
                                                <Text size="xs" c="dimmed" lh={1.5}>
                                                    {w.description}
                                                </Text>
                                            </Card>
                                        </Grid.Col>
                                    ))}
                                </Grid>
                            </Stack>
                        </Container>
                    </Box>

                    {/* ── Analytics ─────────────────────────────────────── */}
                    <Box
                        id="analytics"
                        style={{ background: altBg, scrollMarginTop: "60px" }}
                    >
                        <Container size="lg" py={80}>
                            <Stack gap={48}>
                                <SectionHeader
                                    eyebrow="Analytics"
                                    title="29 ways to see your data"
                                    subtitle="Six chart types, 29 variants. Combine trackers on one axis, or scope a chart to a saved view."
                                    primaryColor={theme.primaryColor}
                                />
                                <Grid>
                                    {/* Line */}
                                    <Grid.Col span={{ base: 12, sm: 6 }}>
                                        <Card withBorder radius="md" p="lg" h="100%">
                                            <Stack gap="xs" mb="md">
                                                <Text fw={600} size="sm">
                                                    {ANALYTICS_CHARTS[0].title}
                                                </Text>
                                                <Text size="xs" c="dimmed">
                                                    {
                                                        ANALYTICS_CHARTS[0]
                                                            .subtitle
                                                    }
                                                </Text>
                                            </Stack>
                                            <LineChart
                                                data={LINE_DATA}
                                                dataKey="month"
                                                series={[
                                                    {
                                                        name: "value",
                                                        color: `${theme.primaryColor}.6`,
                                                        label: "Entries",
                                                    },
                                                ]}
                                                h={240}
                                                gridAxis="x"
                                                withDots={false}
                                                withTooltip={false}
                                            />
                                        </Card>
                                    </Grid.Col>

                                    {/* Bar */}
                                    <Grid.Col span={{ base: 12, sm: 6 }}>
                                        <Card withBorder radius="md" p="lg" h="100%">
                                            <Stack gap="xs" mb="md">
                                                <Text fw={600} size="sm">
                                                    {ANALYTICS_CHARTS[1].title}
                                                </Text>
                                                <Text size="xs" c="dimmed">
                                                    {
                                                        ANALYTICS_CHARTS[1]
                                                            .subtitle
                                                    }
                                                </Text>
                                            </Stack>
                                            <BarChart
                                                data={BAR_DATA}
                                                dataKey="day"
                                                series={[
                                                    {
                                                        name: "count",
                                                        color: "teal.6",
                                                        label: "Count",
                                                    },
                                                ]}
                                                h={240}
                                                withTooltip={false}
                                                gridAxis="x"
                                            />
                                        </Card>
                                    </Grid.Col>

                                    {/* Donut */}
                                    <Grid.Col span={{ base: 12, sm: 6 }}>
                                        <Card withBorder radius="md" p="lg" h="100%">
                                            <Stack gap="xs" mb="md">
                                                <Text fw={600} size="sm">
                                                    {ANALYTICS_CHARTS[2].title}
                                                </Text>
                                                <Text size="xs" c="dimmed">
                                                    {
                                                        ANALYTICS_CHARTS[2]
                                                            .subtitle
                                                    }
                                                </Text>
                                            </Stack>
                                            <Box
                                                style={{
                                                    display: "flex",
                                                    justifyContent: "center",
                                                }}
                                            >
                                                <DonutChart
                                                    data={DONUT_DATA}
                                                    size={160}
                                                    thickness={22}
                                                    paddingAngle={2}
                                                    withLabels
                                                    withLabelsLine
                                                    labelsType="percent"
                                                    tooltipDataSource="segment"
                                                    h={240}
                                                />
                                            </Box>
                                        </Card>
                                    </Grid.Col>

                                    {/* Scatter */}
                                    <Grid.Col span={{ base: 12, sm: 6 }}>
                                        <Card withBorder radius="md" p="lg" h="100%">
                                            <Stack gap="xs" mb="md">
                                                <Text fw={600} size="sm">
                                                    {ANALYTICS_CHARTS[3].title}
                                                </Text>
                                                <Text size="xs" c="dimmed">
                                                    {
                                                        ANALYTICS_CHARTS[3]
                                                            .subtitle
                                                    }
                                                </Text>
                                            </Stack>
                                            <ScatterChart
                                                data={SCATTER_DATA}
                                                dataKey={{ x: "x", y: "y" }}
                                                h={240}
                                                gridAxis="x"
                                                withTooltip={false}
                                            />
                                        </Card>
                                    </Grid.Col>
                                </Grid>
                            </Stack>
                        </Container>
                    </Box>

                    {/* ── Integrations ──────────────────────────────────── */}
                    <Box id="integrations" style={{ scrollMarginTop: "60px" }}>
                        <Container size="lg" py={80}>
                            <Grid gutter={60} align="center">
                                <Grid.Col span={{ base: 12, md: 5 }}>
                                    <Stack gap="lg">
                                        <div>
                                            <Text
                                                size="sm"
                                                fw={700}
                                                tt="uppercase"
                                                c={theme.primaryColor}
                                                style={{
                                                    letterSpacing: "0.08em",
                                                }}
                                                mb={8}
                                            >
                                                Integrations
                                            </Text>
                                            <Title order={2} mb="md">
                                                Bring data in automatically
                                            </Title>
                                            <Text c="dimmed" lh={1.7}>
                                                Connect a service and map its
                                                values onto tracker fields.
                                                Imports are read-only and update
                                                rows in place instead of
                                                duplicating them, and you can
                                                re-import from any date to
                                                backfill history. Stored
                                                credentials are encrypted at
                                                rest.
                                            </Text>
                                        </div>
                                    </Stack>
                                </Grid.Col>

                                <Grid.Col span={{ base: 12, md: 7 }}>
                                    <SimpleGrid
                                        cols={{ base: 1, sm: 2 }}
                                        spacing="md"
                                    >
                                        {PROVIDERS.map((p) => (
                                            <Card
                                                key={p.title}
                                                withBorder
                                                radius="md"
                                                p="lg"
                                                h="100%"
                                                style={{
                                                    borderTop: `3px solid var(--mantine-color-${p.color}-5)`,
                                                }}
                                            >
                                                <Stack gap="sm">
                                                    <ThemeIcon
                                                        size={44}
                                                        radius="md"
                                                        variant="light"
                                                        color={p.color}
                                                    >
                                                        {p.icon}
                                                    </ThemeIcon>
                                                    <Text fw={600} size="sm">
                                                        {p.title}
                                                    </Text>
                                                    <Text
                                                        size="xs"
                                                        c="dimmed"
                                                        lh={1.5}
                                                    >
                                                        {p.detail}
                                                    </Text>
                                                    <List
                                                        size="xs"
                                                        c="dimmed"
                                                        spacing={4}
                                                    >
                                                        {p.abilities.map((a) => (
                                                            <List.Item key={a}>
                                                                {a}
                                                            </List.Item>
                                                        ))}
                                                    </List>
                                                </Stack>
                                            </Card>
                                        ))}
                                    </SimpleGrid>
                                </Grid.Col>
                            </Grid>
                        </Container>
                    </Box>

                    {/* ── Collaboration ─────────────────────────────────── */}
                    <Box
                        id="collaboration"
                        style={{ background: altBg, scrollMarginTop: "60px" }}
                    >
                        <Container size="lg" py={80}>
                            <Stack gap={48}>
                                <SectionHeader
                                    eyebrow="Collaboration"
                                    title="Share with the right permissions"
                                    subtitle="Grant teammates exactly the level of access they need, nothing more."
                                    primaryColor={theme.primaryColor}
                                />
                                <Grid>
                                    {COLLAB_ROLES.map((role) => (
                                        <Grid.Col
                                            key={role.title}
                                            span={{ base: 6, sm: 6, md: 3 }}
                                        >
                                            <Card
                                                withBorder
                                                radius="md"
                                                p="lg"
                                                h="100%"
                                                style={{
                                                    borderTop: `3px solid var(--mantine-color-${role.color}-5)`,
                                                }}
                                            >
                                                <Stack gap="sm">
                                                    <ThemeIcon
                                                        size={44}
                                                        radius="md"
                                                        variant="light"
                                                        color={role.color}
                                                    >
                                                        {role.icon}
                                                    </ThemeIcon>
                                                    <Text fw={600} size="sm">
                                                        {role.title}
                                                    </Text>
                                                    <List
                                                        size="xs"
                                                        c="dimmed"
                                                        spacing={4}
                                                    >
                                                        {role.abilities.map(
                                                            (a) => (
                                                                <List.Item
                                                                    key={a}
                                                                >
                                                                    {a}
                                                                </List.Item>
                                                            )
                                                        )}
                                                    </List>
                                                </Stack>
                                            </Card>
                                        </Grid.Col>
                                    ))}
                                </Grid>
                            </Stack>
                        </Container>
                    </Box>

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
                                        Ready to start tracking?
                                    </Title>
                                    <Text
                                        size="lg"
                                        c="dimmed"
                                        ta="center"
                                        maw={480}
                                    >
                                        Create an account and build your first
                                        tracker in minutes.
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
