import {
    BarChart,
    DonutChart,
    LineChart,
    ScatterChart,
} from "@mantine/charts";
import {
    ActionIcon,
    Box,
    Group,
    Indicator,
    Paper,
    ScrollArea,
    Stack,
    Text,
    Tooltip,
    em,
} from "@mantine/core";
import { Calendar } from "@mantine/dates";
import { useMediaQuery } from "@mantine/hooks";
import {
    DndContext,
    DragEndEvent,
    PointerSensor,
    useSensor,
    useSensors,
} from "@dnd-kit/core";
import {
    arrayMove,
    rectSortingStrategy,
    SortableContext,
    useSortable,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { restrictToFirstScrollableAncestor } from "@dnd-kit/modifiers";
import React, { CSSProperties, useEffect, useMemo, useState } from "react";
import Masonry, { ResponsiveMasonry } from "react-responsive-masonry";
import { CiTrash } from "react-icons/ci";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import {
    createBarChartTooltipContent,
    createDonutTooltipContent,
    createScatterTooltipContent,
    createTooltipContent,
    getAxisFormatter,
} from "../../analytics/components/ChartFormatters";
import { AnalyticResultTypeEnum } from "../../analytics/enums/AnalyticResultTypeEnum";
import {
    AnalyticDto,
    BarChartAnalyticDto,
    CalendarAnalyticDto,
    DonutChartAnaylticDto,
    LineChartAnalyticDto,
    ScatterChartAnalyticDto,
    SingleValueAnalyticDto,
} from "../../analytics/types/AnalyticDto";
import { closestToPointer } from "../../analytics/components/MasonryCollision";

interface DashboardGridProps {
    analytics: AnalyticDto[];
    color: string | undefined;
    isConfiguring: boolean;
    onReorder: (orderedIds: string[]) => void;
    onRemove?: (itemId: string) => void;
}

interface SortableCardWrapperProps {
    id: string;
    children: React.ReactNode;
    isReordering: boolean;
    index: number;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (id: string) => void;
}

function SortableCardWrapper({
    id,
    children,
    isReordering,
    index,
    color,
    isConfiguring,
    onRemove,
}: SortableCardWrapperProps) {
    const sortable = useSortable({ id, disabled: !isReordering });

    const style: CSSProperties = {
        transform: CSS.Translate.toString(sortable.transform),
        transition: sortable.isDragging ? sortable.transition : "none",
        opacity: sortable.isDragging ? 0.7 : 1,
        touchAction: isReordering ? "none" : "pan-y",
        cursor: isReordering ? "grab" : "default",
        width: "100%",
    };

    return (
        <div
            ref={sortable.setNodeRef}
            style={style}
            {...sortable.attributes}
            {...sortable.listeners}
        >
            <Indicator
                color={color}
                processing
                label={index + 1}
                position="bottom-center"
                size={20}
                disabled={!isReordering}
            >
                <div style={{ position: "relative" }}>
                    {children}
                    {isConfiguring && onRemove && (
                        <ActionIcon
                            size="md"
                            color={color}
                            variant="outline"
                            style={{
                                position: "absolute",
                                top: 10,
                                right: 10,
                                zIndex: 10,
                            }}
                            onClick={(e) => {
                                e.stopPropagation();
                                onRemove(id);
                            }}
                        >
                            <CiTrash size={18} />
                        </ActionIcon>
                    )}
                </div>
            </Indicator>
        </div>
    );
}

function SingleValueCardDisplay({
    analytic,
}: {
    analytic: SingleValueAnalyticDto;
    color: string | undefined;
}) {
    return (
        <Paper withBorder p="md" radius="md" w="100%">
            <Stack gap="xs">
                <Text size="sm" c="dimmed" fw={500}>
                    {analytic.valueField
                        ? `${analytic.name}: ${analytic.valueField.name}`
                        : analytic.name}
                </Text>
                <Text
                    size="xl"
                    fw={600}
                    style={{ wordBreak: "break-word", lineHeight: 1.2 }}
                >
                    {renderValue(analytic.valueField?.type, analytic.value)}
                </Text>
            </Stack>
        </Paper>
    );
}

function LineChartCardDisplay({
    analytic,
    color,
}: {
    analytic: LineChartAnalyticDto;
    color: string | undefined;
}) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const resolvedColor = color ?? "blue";

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Text size="sm" c="dimmed" fw={500} mb="sm">
                    {`${analytic.name}: ${analytic.xField.name} - ${analytic.yField.name}`}
                </Text>
                <LineChart
                    h={isMobile ? 210 : 300}
                    data={analytic.points}
                    dataKey="x"
                    series={[
                        {
                            name: "y",
                            color: resolvedColor,
                            label: analytic.yField.name,
                        },
                    ]}
                    tooltipAnimationDuration={200}
                    xAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.xField.type),
                    }}
                    yAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.yField.type),
                    }}
                    tooltipProps={{
                        content: createTooltipContent(analytic, resolvedColor),
                    }}
                />
            </Stack>
        </Paper>
    );
}

function BarChartCardDisplay({
    analytic,
    color,
}: {
    analytic: BarChartAnalyticDto;
    color: string | undefined;
}) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const resolvedColor = color ?? "blue";
    const subtitle = analytic.valueField
        ? `${analytic.nameField.name} - ${analytic.valueField.name}`
        : analytic.nameField.name;

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Text size="sm" c="dimmed" fw={500} mb="sm">
                    {`${analytic.name}: ${subtitle}`}
                </Text>
                <BarChart
                    h={isMobile ? 210 : 300}
                    data={analytic.points}
                    dataKey="name"
                    series={[
                        {
                            name: "value",
                            color: resolvedColor,
                            label: analytic.valueField?.name ?? "Count",
                        },
                    ]}
                    tooltipAnimationDuration={200}
                    xAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.nameField.type),
                    }}
                    yAxisProps={{
                        tickFormatter: analytic.valueField
                            ? getAxisFormatter(analytic.valueField.type)
                            : undefined,
                    }}
                    tooltipProps={{
                        content: createBarChartTooltipContent(analytic, resolvedColor),
                    }}
                />
            </Stack>
        </Paper>
    );
}

function DonutChartCardDisplay({
    analytic,
    color,
}: {
    analytic: DonutChartAnaylticDto;
    color: string | undefined;
}) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const baseColor = color ?? "blue";

    const { positivePoints, excludedPoints } = useMemo(() => {
        const positive = analytic.points.filter((x) => (x.value ?? 0) > 0);
        const excluded = analytic.points.filter((x) => (x.value ?? 0) <= 0);
        return { positivePoints: positive, excludedPoints: excluded };
    }, [analytic.points]);

    const coloredPoints = useMemo(
        () =>
            positivePoints.map((x, index) => {
                const opacity =
                    0.2 + (index / Math.max(positivePoints.length, 1)) * 0.8;
                return {
                    name: x.name,
                    value: x.value,
                    color: `color-mix(in srgb, var(--mantine-color-${baseColor}-6) ${
                        opacity * 100
                    }%, white)`,
                };
            }),
        [positivePoints, baseColor]
    );

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Text size="sm" c="dimmed" fw={500} mb="sm">
                    {`${analytic.name}: ${analytic.nameField.name} - ${analytic.valueField.name}`}
                </Text>
                <Box
                    h={isMobile ? 210 : 300}
                    style={{
                        display: "flex",
                        flexDirection: "column",
                        gap: "var(--mantine-spacing-xs)",
                    }}
                >
                    {excludedPoints.length > 0 && (
                        <Tooltip
                            label={excludedPoints
                                .map((p) => p.name ?? "Unknown")
                                .join(", ")}
                            multiline
                            maw={260}
                        >
                            <Text
                                size="xs"
                                c="dimmed"
                                style={{ cursor: "default" }}
                            >
                                {excludedPoints.length} categor
                                {excludedPoints.length === 1 ? "y" : "ies"} not
                                shown (zero or negative value)
                            </Text>
                        </Tooltip>
                    )}
                    <Box
                        style={{
                            flex: 1,
                            minHeight: 0,
                            display: "flex",
                            justifyContent: "center",
                        }}
                    >
                        <DonutChart
                            withLabelsLine
                            w="100%"
                            withLabels
                            size={isMobile ? 150 : 200}
                            thickness={20}
                            paddingAngle={2}
                            tooltipDataSource="segment"
                            tooltipProps={{
                                content: createDonutTooltipContent(analytic),
                            }}
                            labelsType="percent"
                            tooltipAnimationDuration={200}
                            data={coloredPoints}
                            h="100%"
                        />
                    </Box>
                </Box>
            </Stack>
        </Paper>
    );
}

function ScatterChartCardDisplay({
    analytic,
    color,
}: {
    analytic: ScatterChartAnalyticDto;
    color: string | undefined;
}) {
    const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
    const resolvedColor = color ?? "blue";

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Text size="sm" c="dimmed" fw={500} mb="sm">
                    {`${analytic.name}: ${analytic.xField.name} - ${analytic.yField.name}`}
                </Text>
                <ScatterChart
                    tooltipAnimationDuration={200}
                    gridAxis="x"
                    data={[
                        {
                            name: analytic.yField.name,
                            color: resolvedColor,
                            data: analytic.points,
                        },
                    ]}
                    h={isMobile ? 210 : 300}
                    xAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.xField.type),
                    }}
                    yAxisProps={{
                        tickFormatter: getAxisFormatter(analytic.yField.type),
                    }}
                    tooltipProps={{
                        content: createScatterTooltipContent(
                            analytic,
                            resolvedColor
                        ),
                    }}
                    dataKey={{ x: "x", y: "y" }}
                />
            </Stack>
        </Paper>
    );
}

const getDateKey = (date: Date): string => {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
};

function CalendarCardDisplay({
    analytic,
    color,
}: {
    analytic: CalendarAnalyticDto;
    color: string | undefined;
}) {
    const resolvedColor = color ?? "blue";
    const [selectedDate, setSelectedDate] = useState<Date | undefined>();
    const [viewDate, setViewDate] = useState<Date>(new Date());

    const events = useMemo(() => {
        const eventsByDate = new Map<string, typeof analytic.points>();
        analytic.points.forEach((event) => {
            if (!event.date) return;
            const dateKey = getDateKey(new Date(event.date));
            if (!eventsByDate.has(dateKey)) eventsByDate.set(dateKey, []);
            eventsByDate.get(dateKey)!.push(event);
        });
        return eventsByDate;
    }, [analytic.points]);

    const eventsForSelectedDate = selectedDate
        ? (events.get(getDateKey(selectedDate)) ?? [])
        : [];

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Text size="sm" c="dimmed" fw={500} mb="sm">
                    {`${analytic.name}: ${analytic.whenField.name} - ${analytic.whatField.name}`}
                </Text>
                {!selectedDate ? (
                    <Group w="100%" justify="center" h={300}>
                        <Calendar
                            date={viewDate}
                            onDateChange={(d) => setViewDate(new Date(d))}
                            size="sm"
                            getDayProps={(date) => ({
                                onClick: () => setSelectedDate(new Date(date)),
                            })}
                            renderDay={(date) => {
                                const dateObj = new Date(date);
                                const hasEvents = events.has(
                                    getDateKey(dateObj)
                                );
                                return (
                                    <Indicator
                                        size={9}
                                        color={resolvedColor}
                                        offset={-2}
                                        disabled={!hasEvents}
                                    >
                                        <div>{dateObj.getDate()}</div>
                                    </Indicator>
                                );
                            }}
                        />
                    </Group>
                ) : (
                    <Stack gap="xs" h={300}>
                        <Group gap="xs">
                            <ActionIcon
                                size="sm"
                                variant="subtle"
                                color={resolvedColor}
                                onClick={() => setSelectedDate(undefined)}
                            >
                                <Text size="xs">←</Text>
                            </ActionIcon>
                        </Group>
                        <ScrollArea h="100%">
                            {eventsForSelectedDate.length === 0 ? (
                                <Text size="sm" c="dimmed" ta="center" py="xl">
                                    No events on this date
                                </Text>
                            ) : (
                                <Stack gap="xs" pr="xs">
                                    {eventsForSelectedDate.map(
                                        (event, index) => (
                                            <Paper
                                                key={index}
                                                withBorder
                                                p="xs"
                                                style={{
                                                    borderRadius: "6px",
                                                    borderLeft: `3px solid var(--mantine-color-${resolvedColor}-6)`,
                                                }}
                                            >
                                                <Stack>
                                                    <Text size="sm">
                                                        {event.name}
                                                    </Text>
                                                    <Text c="dimmed" size="xs">
                                                        {renderValue(
                                                            analytic.whenField
                                                                .type,
                                                            event.date
                                                        )}
                                                    </Text>
                                                </Stack>
                                            </Paper>
                                        )
                                    )}
                                </Stack>
                            )}
                        </ScrollArea>
                    </Stack>
                )}
            </Stack>
        </Paper>
    );
}

function renderCard(analytic: AnalyticDto, color: string | undefined) {
    switch (analytic.resultType) {
        case AnalyticResultTypeEnum.SingleValue:
            return (
                <SingleValueCardDisplay
                    analytic={analytic as SingleValueAnalyticDto}
                    color={color}
                />
            );
        case AnalyticResultTypeEnum.LineChart:
            return (
                <LineChartCardDisplay
                    analytic={analytic as LineChartAnalyticDto}
                    color={color}
                />
            );
        case AnalyticResultTypeEnum.BarChart:
            return (
                <BarChartCardDisplay
                    analytic={analytic as BarChartAnalyticDto}
                    color={color}
                />
            );
        case AnalyticResultTypeEnum.Donut:
            return (
                <DonutChartCardDisplay
                    analytic={analytic as DonutChartAnaylticDto}
                    color={color}
                />
            );
        case AnalyticResultTypeEnum.ScatterChart:
            return (
                <ScatterChartCardDisplay
                    analytic={analytic as ScatterChartAnalyticDto}
                    color={color}
                />
            );
        case AnalyticResultTypeEnum.Calendar:
            return (
                <CalendarCardDisplay
                    analytic={analytic as CalendarAnalyticDto}
                    color={color}
                />
            );
        default:
            return null;
    }
}

export function DashboardGrid({
    analytics,
    color,
    isConfiguring,
    onReorder,
    onRemove,
}: DashboardGridProps) {
    const [orderedAnalytics, setOrderedAnalytics] = useState(analytics);

    useEffect(() => {
        setOrderedAnalytics(analytics);
    }, [analytics]);

    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 5 } })
    );

    const handleDragEnd = (event: DragEndEvent) => {
        const { active, over } = event;
        if (!isConfiguring || !over || active.id === over.id) return;

        const oldIndex = orderedAnalytics.findIndex((a) => a.id === active.id);
        const newIndex = orderedAnalytics.findIndex((a) => a.id === over.id);

        const newOrder = arrayMove(orderedAnalytics, oldIndex, newIndex);
        setOrderedAnalytics(newOrder);
        onReorder(newOrder.map((x) => x.id));
    };

    return (
        <Stack>
            <DndContext
                sensors={sensors}
                onDragEnd={handleDragEnd}
                modifiers={[restrictToFirstScrollableAncestor]}
                collisionDetection={closestToPointer}
            >
                <SortableContext
                    items={orderedAnalytics.map((a) => a.id)}
                    strategy={rectSortingStrategy}
                >
                    <ResponsiveMasonry
                        columnsCountBreakPoints={{
                            350: 1,
                            640: 2,
                            1024: 3,
                            1536: 4,
                        }}
                    >
                        <Masonry gutter="16px">
                            {orderedAnalytics.map((analytic, index) => (
                                <SortableCardWrapper
                                    key={analytic.id}
                                    id={analytic.id}
                                    isReordering={isConfiguring}
                                    index={index}
                                    color={color}
                                    isConfiguring={isConfiguring}
                                    onRemove={onRemove}
                                >
                                    {renderCard(analytic, color)}
                                </SortableCardWrapper>
                            ))}
                        </Masonry>
                    </ResponsiveMasonry>
                </SortableContext>
            </DndContext>
        </Stack>
    );
}
