import {
    Badge,
    Button,
    Group,
    Loader,
    Modal,
    Paper,
    Stack,
    Tabs,
    Text,
    ThemeIcon,
    UnstyledButton,
    useMantineTheme,
} from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { ReactNode, useEffect, useRef, useState } from "react";
import { IconType } from "react-icons";
import { FiChevronRight, FiPlusSquare, FiRefreshCw } from "react-icons/fi";
import { MdOutlineHorizontalRule } from "react-icons/md";
import {
    TbAdjustmentsHorizontal,
    TbCalendar,
    TbChartBar,
    TbChartDonut,
    TbChartHistogram,
    TbChartLine,
    TbChartScatter,
    TbHeading,
    TbLayoutGrid,
    TbLayoutBoardSplit,
    TbLayoutNavbar,
    TbNote,
    TbNumber123,
    TbTable,
    TbTarget,
} from "react-icons/tb";
import { analyticsController } from "../../analytics/api/analyticsController";
import { AnalyticResultTypeEnum } from "../../analytics/enums/AnalyticResultTypeEnum";
import { AnalyticConfigDto } from "../../analytics/types/AnalyticConfigDto";
import { CustomAnalyticForm } from "../../dashboard/components/CustomAnalyticForm";
import { EntriesWidgetForm } from "../../dashboard/components/EntriesWidgetForm";
import { HeaderWidgetForm } from "../../dashboard/components/HeaderWidgetForm";
import { NoteWidgetForm } from "../../dashboard/components/NoteWidgetForm";
import { QuickAddTrackerForm } from "../../dashboard/components/QuickAddTrackerForm";
import { FilterWidgetForm } from "../../dashboard/components/FilterWidgetForm";
import { useDashboard } from "../../dashboard/context/DashboardContext";
import { correlationInsightsController } from "../api/correlationInsightsController";
import { CorrelationInsightDto } from "../types/CorrelationInsightDto";
import { DiscoveredInsightCard } from "./DiscoveredInsightCard";

interface Props {
    color: string;
    onClose: () => void;
}

type TabValue = "charts" | "controls" | "layout";

type Panel =
    | { kind: "list" }
    | { kind: "new-chart"; resultType: string }
    | { kind: "new-table" }
    | {
          kind: "config";
          widgetKind: "quickAdd" | "filter" | "header" | "note";
      };

const TAB_META: { value: TabValue; label: string; icon: IconType }[] = [
    { value: "charts", label: "Charts", icon: TbChartHistogram },
    { value: "controls", label: "Controls", icon: FiPlusSquare },
    { value: "layout", label: "Layout", icon: TbLayoutGrid },
];

// Marks the "Entries table" row in the same list as the chart types, since it isn't a result type.
const ENTRIES_TABLE_KEY = "entriesTable";

const CHART_TYPE_ICONS: Record<string, IconType> = {
    [AnalyticResultTypeEnum.SingleValue]: TbNumber123,
    [AnalyticResultTypeEnum.Goal]: TbTarget,
    [AnalyticResultTypeEnum.LineChart]: TbChartLine,
    [AnalyticResultTypeEnum.BarChart]: TbChartBar,
    [AnalyticResultTypeEnum.ScatterChart]: TbChartScatter,
    [AnalyticResultTypeEnum.Donut]: TbChartDonut,
    [AnalyticResultTypeEnum.Calendar]: TbCalendar,
};

interface InstantOption {
    key:
        | "quickAdd"
        | "filter"
        | "header"
        | "divider"
        | "note"
        | "container"
        | "tabsContainer";
    title: string;
    icon: IconType;
}

const CONTROL_OPTIONS = [
    { key: "quickAdd", title: "Quick-add button", icon: FiPlusSquare },
    { key: "filter", title: "Filter", icon: TbAdjustmentsHorizontal },
] satisfies InstantOption[];

const LAYOUT_OPTIONS = [
    { key: "header", title: "Header", icon: TbHeading },
    { key: "divider", title: "Divider", icon: MdOutlineHorizontalRule },
    { key: "note", title: "Note", icon: TbNote },
    { key: "container", title: "Container", icon: TbLayoutBoardSplit },
    { key: "tabsContainer", title: "Tabs container", icon: TbLayoutNavbar },
] satisfies InstantOption[];

function panelTitle(panel: Panel): string {
    switch (panel.kind) {
        case "list":
            return "Widgets";
        case "new-chart":
            return `New ${panel.resultType}`;
        case "new-table":
            return "New entries table";
        case "config":
            return panel.widgetKind === "quickAdd"
                ? "Add a quick-add button"
                : panel.widgetKind === "filter"
                ? "Add a filter"
                : panel.widgetKind === "header"
                ? "Add a header"
                : "Add a note";
    }
}

export function WidgetLibraryModal({ color, onClose }: Props) {
    const theme = useMantineTheme();
    const isMobile = useMediaQuery("(max-width: 48em)");
    const {
        createAndPlaceWidget,
        createAndPlaceEntriesWidget,
        addQuickAddItem,
        addFilterItem,
        addHeaderItem,
        addDividerItem,
        addNoteItem,
        addContainerItem,
        addTabsContainerItem,
    } = useDashboard();

    const [tab, setTab] = useState<TabValue>("charts");
    const [panel, setPanel] = useState<Panel>({ kind: "list" });
    const [addingInstantKey, setAddingInstantKey] =
        useState<InstantOption["key"] | null>(null);
    const [analyticConfig, setAnalyticConfig] = useState<AnalyticConfigDto>();

    const [insights, setInsights] = useState<CorrelationInsightDto[]>([]);
    const [insightsLoading, setInsightsLoading] = useState(true);
    const [insightsRunning, setInsightsRunning] = useState(false);
    const [addingInsightId, setAddingInsightId] = useState<string | null>(null);
    const [dismissingInsightId, setDismissingInsightId] = useState<string | null>(null);

    useEffect(() => {
        analyticsController.getAnalyticsConfig().then((res) => setAnalyticConfig(res.data));
        correlationInsightsController
            .getInsights()
            .then((res) => setInsights(res.data ?? []))
            .finally(() => setInsightsLoading(false));
    }, []);

    // Every chart type gets its own row; the entries table option rides along in the same list.
    const chartAndTableOptions = [
        ...(analyticConfig?.resultTypes ?? []).map((rt) => ({
            key: rt.name,
            title: rt.name,
            icon: CHART_TYPE_ICONS[rt.name] ?? TbChartHistogram,
        })),
        { key: ENTRIES_TABLE_KEY, title: "Entries table", icon: TbTable },
    ];

    const runInsightsNow = async () => {
        setInsightsRunning(true);
        try {
            const res = await correlationInsightsController.runNow();
            setInsights(res.data ?? []);
        } finally {
            setInsightsRunning(false);
        }
    };

    const dismissInsight = async (insight: CorrelationInsightDto) => {
        setDismissingInsightId(insight.id);
        try {
            await correlationInsightsController.dismiss(insight.id);
            setInsights((prev) => prev.filter((i) => i.id !== insight.id));
        } finally {
            setDismissingInsightId(null);
        }
    };

    const addInsight = async (insight: CorrelationInsightDto) => {
        setAddingInsightId(insight.id);
        try {
            await createAndPlaceWidget({
                name: `${insight.valueFieldAName} vs ${insight.valueFieldBName}`,
                resultType: "Scatter Chart",
                code: "Correlation Scatter",
                sources: [
                    {
                        trackerId: insight.trackerAId,
                        analyticFields: [
                            { purpose: "Match", fieldId: insight.matchFieldAId },
                            { purpose: "Value", fieldId: insight.valueFieldAId },
                        ],
                    },
                    {
                        trackerId: insight.trackerBId,
                        analyticFields: [
                            { purpose: "Match", fieldId: insight.matchFieldBId },
                            { purpose: "Value", fieldId: insight.valueFieldBId },
                        ],
                    },
                ],
            });
            await correlationInsightsController.dismiss(insight.id);
            setInsights((prev) => prev.filter((i) => i.id !== insight.id));
        } finally {
            setAddingInsightId(null);
        }
    };

    const backToList = () => setPanel({ kind: "list" });

    // Leaves the modal open on failure so the filled-in form isn't lost; closes on success.
    const closeAfter =
        <A extends unknown[]>(handler: (...args: A) => Promise<unknown>) =>
        async (...args: A) => {
            await handler(...args);
            onClose();
        };

    const pickInstant = async (key: InstantOption["key"]) => {
        // These carry no configuration to fill in first, so they're placed straight away.
        if (key === "divider" || key === "container" || key === "tabsContainer") {
            setAddingInstantKey(key);
            try {
                await (key === "container"
                    ? addContainerItem()
                    : key === "tabsContainer"
                    ? addTabsContainerItem()
                    : addDividerItem());
                onClose();
            } finally {
                setAddingInstantKey(null);
            }
            return;
        }
        setPanel({ kind: "config", widgetKind: key });
    };

    const pickChartOrTable = (key: string) => {
        setPanel(
            key === ENTRIES_TABLE_KEY
                ? { kind: "new-table" }
                : { kind: "new-chart", resultType: key },
        );
    };

    // Add forms open in a second modal stacked on top of the list.
    const isList = panel.kind === "list";
    const lastSubPanelRef = useRef<Panel>({ kind: "list" });
    if (!isList) {
        lastSubPanelRef.current = panel;
    }
    // Keeps rendering the last panel's content during the close transition so the box doesn't empty out mid-fade.
    const subPanel = isList ? lastSubPanelRef.current : panel;
    const isWideSub =
        subPanel.kind === "new-chart" ||
        subPanel.kind === "new-table" ||
        (subPanel.kind === "config" && subPanel.widgetKind === "filter");

    // The toolbar stays pinned at the top of the tab; only the rows below it scroll.
    const scrollRegion = (children: ReactNode) => (
        <div style={{ flex: 1, minHeight: 0, overflowY: "auto", paddingRight: 4 }}>
            {children}
        </div>
    );

    const optionList = <K extends string>(
        options: { key: K; title: string; icon: IconType }[],
        onSelect: (key: K) => void,
    ) => (
        <Paper withBorder radius="md" p={4}>
            <Stack gap={2}>
                {options.map((option) => (
                    <UnstyledButton
                        key={option.key}
                        onClick={() => onSelect(option.key)}
                        disabled={addingInstantKey === option.key}
                        px="sm"
                        py="xs"
                        style={{ borderRadius: theme.radius.sm, width: "100%" }}
                    >
                        <Group wrap="nowrap" gap="sm">
                            <ThemeIcon size={34} radius="md" variant="light" color={color}>
                                <option.icon size={18} />
                            </ThemeIcon>
                            <Text fw={500} style={{ flex: 1 }}>
                                {option.title}
                            </Text>
                            <FiChevronRight size={18} />
                        </Group>
                    </UnstyledButton>
                ))}
            </Stack>
        </Paper>
    );

    return (
        <>
            <Modal
                opened
                onClose={onClose}
                title="Widgets"
                size={960}
                centered
                fullScreen={isMobile}
                // The sub-panel modal below owns scroll-lock/focus-trap while it's open, so this one doesn't fight it.
                lockScroll={isList}
                trapFocus={isList}
                styles={{
                    // Fixed height so the modal doesn't jump around as filtered results change row count.
                    content: {
                        height: isMobile ? "100%" : "min(92vh, 840px)",
                        display: "flex",
                        flexDirection: "column",
                    },
                    body: {
                        flex: 1,
                        minHeight: 0,
                        display: "flex",
                        flexDirection: "column",
                        overflow: "hidden",
                    },
                }}
            >
                <Tabs
                    value={tab}
                    onChange={(value) => setTab(value as TabValue)}
                    // Must unmount inactive panels: a hidden panel with explicit `display` set ignores Mantine's `hidden` state.
                    keepMounted={false}
                    styles={{
                        root: {
                            flex: 1,
                            minHeight: 0,
                            display: "flex",
                            flexDirection: "column",
                        },
                        panel: { flex: 1, minHeight: 0 },
                    }}
                >
                    <Tabs.List mb="md">
                        {TAB_META.map(({ value, label, icon: Icon }) => (
                            <Tabs.Tab
                                key={value}
                                value={value}
                                px={isMobile ? "xs" : undefined}
                                leftSection={<Icon size={16} />}
                            >
                                {(!isMobile || tab === value) && label}
                            </Tabs.Tab>
                        ))}
                    </Tabs.List>

                    <Tabs.Panel value="charts" style={{ display: "flex", flexDirection: "column" }}>
                        <Stack gap="md" style={{ flex: 1, minHeight: 0 }}>
                            {scrollRegion(
                                <Stack gap="md">
                                    {analyticConfig ? (
                                        optionList(chartAndTableOptions, pickChartOrTable)
                                    ) : (
                                        <Group justify="center" py="sm">
                                            <Loader size="sm" />
                                        </Group>
                                    )}

                                    <Stack gap="xs">
                                        <Group justify="space-between" align="center">
                                            <Group gap="xs">
                                                <Text size="sm" fw={600}>
                                                    Discovered
                                                </Text>
                                                {insights.length > 0 && (
                                                    <Badge variant="light" color={color}>
                                                        {insights.length}
                                                    </Badge>
                                                )}
                                            </Group>
                                            <Button
                                                variant="subtle"
                                                size="compact-sm"
                                                leftSection={<FiRefreshCw size={14} />}
                                                loading={insightsRunning}
                                                onClick={runInsightsNow}
                                            >
                                                Refresh
                                            </Button>
                                        </Group>
                                        {insightsLoading ? (
                                            <Group justify="center" py="sm">
                                                <Loader size="sm" />
                                            </Group>
                                        ) : insights.length === 0 ? (
                                            <Text size="xs" c="dimmed">
                                                No correlations found across your trackers yet. Refresh to scan for new ones.
                                            </Text>
                                        ) : (
                                            <Paper withBorder radius="md" p={4}>
                                                <Stack gap={2}>
                                                    {insights.map((insight) => (
                                                        <DiscoveredInsightCard
                                                            key={insight.id}
                                                            insight={insight}
                                                            color={color}
                                                            adding={addingInsightId === insight.id}
                                                            dismissing={dismissingInsightId === insight.id}
                                                            onAdd={() => addInsight(insight)}
                                                            onDismiss={() => dismissInsight(insight)}
                                                        />
                                                    ))}
                                                </Stack>
                                            </Paper>
                                        )}
                                    </Stack>
                                </Stack>
                            )}
                        </Stack>
                    </Tabs.Panel>

                    <Tabs.Panel value="controls" style={{ display: "flex", flexDirection: "column" }}>
                        {scrollRegion(optionList(CONTROL_OPTIONS, pickInstant))}
                    </Tabs.Panel>
                    <Tabs.Panel value="layout" style={{ display: "flex", flexDirection: "column" }}>
                        {scrollRegion(optionList(LAYOUT_OPTIONS, pickInstant))}
                    </Tabs.Panel>
                </Tabs>
            </Modal>

            <Modal
                opened={!isList}
                onClose={backToList}
                title={panelTitle(subPanel)}
                size={isWideSub ? "lg" : "md"}
                centered
                fullScreen={isMobile}
                // Lighter overlay avoids stacking two full-strength scrims over the library.
                overlayProps={{ backgroundOpacity: 0.35 }}
            >
                {subPanel.kind === "new-chart" && (
                    <CustomAnalyticForm
                        presetResultType={subPanel.resultType}
                        onBack={backToList}
                        onAdd={closeAfter(createAndPlaceWidget)}
                    />
                )}

                {subPanel.kind === "new-table" && (
                    <EntriesWidgetForm
                        onBack={backToList}
                        onAdd={closeAfter(createAndPlaceEntriesWidget)}
                    />
                )}

                {subPanel.kind === "config" && subPanel.widgetKind === "quickAdd" && (
                    <QuickAddTrackerForm onBack={backToList} onAdd={closeAfter(addQuickAddItem)} />
                )}

                {subPanel.kind === "config" && subPanel.widgetKind === "filter" && (
                    <FilterWidgetForm
                        color={color}
                        onBack={backToList}
                        onAdd={closeAfter(addFilterItem)}
                    />
                )}

                {subPanel.kind === "config" && subPanel.widgetKind === "header" && (
                    <HeaderWidgetForm onBack={backToList} onAdd={closeAfter(addHeaderItem)} />
                )}

                {subPanel.kind === "config" && subPanel.widgetKind === "note" && (
                    <NoteWidgetForm onBack={backToList} onAdd={closeAfter(addNoteItem)} />
                )}
            </Modal>
        </>
    );
}
