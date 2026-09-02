import {
    ActionIcon,
    Group,
    Indicator,
    Paper,
    ScrollArea,
    Stack,
    Text,
} from "@mantine/core";
import { Calendar } from "@mantine/dates";
import { useMemo, useState } from "react";
import { MdArrowBack, MdLink } from "react-icons/md";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { CalendarAnalyticDto } from "../types/AnalyticDto";
import { AnalyticCardHeader } from "./AnalyticCardHeader";
import {
    cardBodyProps,
    cardShellProps,
    chartHeight,
    useCardLayout,
} from "./cardSizing";

interface CalendarCardProps {
    analytic: CalendarAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onEdit?: (analyticId: string) => void;
    onEntryClick?: (entryId: string) => void;
    /** Stretch to fill the height of the container instead of using a fixed one. */
    fillHeight?: boolean;
}

const getDateKey = (date: Date): string => {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
};

export function CalendarCard({
    analytic,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    onEntryClick,
    fillHeight,
}: CalendarCardProps) {
    const layout = useCardLayout(fillHeight);
    const [selectedDate, setSelectedDate] = useState<Date | undefined>();
    const [viewDate, setViewDate] = useState<Date>(new Date());

    // Create a map of dates to events for quick lookup
    const events = useMemo(() => {
        const eventsByDate = new Map<string, typeof analytic.points>();
        analytic.points.forEach((event) => {
            if (!event.date) return;

            const dateObj = new Date(event.date);
            const dateKey = getDateKey(dateObj);

            if (!eventsByDate.has(dateKey)) {
                eventsByDate.set(dateKey, []);
            }
            eventsByDate.get(dateKey)!.push(event);
        });

        return eventsByDate;
    }, [analytic.points]);

    // Get events for selected date
    const selectedDateKey = selectedDate ? getDateKey(selectedDate) : "";
    const eventsForSelectedDate = events.get(selectedDateKey) || [];

    // Distinct trackers behind the events, in first-seen order. Only populated when the
    // calendar merges more than one tracker; drives the legend and per-event colouring.
    const sources = useMemo(() => {
        const byName = new Map<string, string | undefined>();
        analytic.points.forEach((event) => {
            if (event.trackerName && !byName.has(event.trackerName)) {
                byName.set(event.trackerName, event.color);
            }
        });
        return [...byName].map(([name, color]) => ({ name, color }));
    }, [analytic.points]);

    const isMultiSource = sources.length > 1;

    // Up to three distinct dot colours for a day, so a day with events from several
    // trackers shows each tracker's colour.
    const dayColors = (dayEvents: typeof analytic.points): string[] => {
        const seen: string[] = [];
        for (const event of dayEvents) {
            const c = event.color || color || "blue";
            if (!seen.includes(c)) seen.push(c);
            if (seen.length === 3) break;
        }
        return seen;
    };

    return (
        <Paper
            ref={layout.ref}
            withBorder
            p={layout.padding}
            radius="md"
            {...cardShellProps(fillHeight)}
        >
            <Stack gap="xs" {...cardBodyProps(fillHeight)}>
                <AnalyticCardHeader
                    title={analytic.name}
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={analytic.id}
                    onRemove={onRemove}
                    onEdit={onEdit}
                />

                {!selectedDate ? (
                    <Stack
                        gap="xs"
                        w={"100%"}
                        align="center"
                        h={chartHeight(fillHeight)}
                        style={{
                            ...cardBodyProps(fillHeight).style,
                            overflow: "auto",
                        }}
                    >
                        {isMultiSource && (
                            <Group gap="sm" justify="center" wrap="wrap">
                                {sources.map((source) => (
                                    <Group key={source.name} gap={4} wrap="nowrap">
                                        <div
                                            style={{
                                                width: 8,
                                                height: 8,
                                                borderRadius: "50%",
                                                backgroundColor: `var(--mantine-color-${
                                                    source.color || color || "blue"
                                                }-6)`,
                                            }}
                                        />
                                        <Text size="xs" c="dimmed">
                                            {source.name}
                                        </Text>
                                    </Group>
                                ))}
                            </Group>
                        )}
                        <Calendar
                            date={viewDate}
                            onDateChange={(date) => setViewDate(new Date(date))}
                            // A month grid cannot reflow, so on a widget with no room
                            // for it at full size it is drawn at the smaller one.
                            size={layout.isCompact ? "xs" : "sm"}
                            getDayProps={(date) => {
                                const dateObj = new Date(date);
                                return {
                                    onClick: () => setSelectedDate(dateObj),
                                };
                            }}
                            renderDay={(date) => {
                                const dateObj = new Date(date);
                                const dateKey = getDateKey(dateObj);
                                const dayEvents = events.get(dateKey) || [];
                                const day = dateObj.getDate();

                                if (isMultiSource) {
                                    const colors = dayColors(dayEvents);
                                    return (
                                        <div
                                            style={{
                                                position: "relative",
                                                width: "100%",
                                                height: "100%",
                                                display: "flex",
                                                alignItems: "center",
                                                justifyContent: "center",
                                            }}
                                        >
                                            {day}
                                            <Group
                                                gap={2}
                                                justify="center"
                                                wrap="nowrap"
                                                style={{
                                                    position: "absolute",
                                                    bottom: 1,
                                                    left: 0,
                                                    right: 0,
                                                }}
                                            >
                                                {colors.map((c, i) => (
                                                    <div
                                                        key={i}
                                                        style={{
                                                            width: 4,
                                                            height: 4,
                                                            borderRadius: "50%",
                                                            backgroundColor: `var(--mantine-color-${c}-6)`,
                                                        }}
                                                    />
                                                ))}
                                            </Group>
                                        </div>
                                    );
                                }

                                return (
                                    <Indicator
                                        size={9}
                                        color={color || "blue"}
                                        offset={-2}
                                        disabled={dayEvents.length === 0}
                                    >
                                        <div>{day}</div>
                                    </Indicator>
                                );
                            }}
                        />
                    </Stack>
                ) : (
                    <Stack
                        gap="xs"
                        h={chartHeight(fillHeight)}
                        {...cardBodyProps(fillHeight)}
                    >
                        <Group gap="xs">
                            <ActionIcon
                                size="sm"
                                variant="subtle"
                                color={color || "blue"}
                                onClick={() => setSelectedDate(undefined)}
                            >
                                <MdArrowBack size={16} />
                            </ActionIcon>
                            <Text size="sm" fw={500}></Text>
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
                                                    borderLeft: `3px solid var(--mantine-color-${
                                                        event.color ||
                                                        color ||
                                                        "blue"
                                                    }-6)`,
                                                }}
                                            >
                                                <Group
                                                    align="flex-start"
                                                    justify="space-between"
                                                >
                                                    <Stack>
                                                        <Text
                                                            size="sm"
                                                            style={{ flex: 1 }}
                                                        >
                                                            {event.name}
                                                        </Text>
                                                        <Text
                                                            c={"dimmed"}
                                                            size="xs"
                                                        >
                                                            {renderValue(
                                                                analytic
                                                                    .whenField
                                                                    .type,
                                                                event.date,
                                                            )}
                                                            {event.trackerName
                                                                ? ` · ${event.trackerName}`
                                                                : ""}
                                                        </Text>
                                                    </Stack>
                                                    {onEntryClick && (
                                                        <ActionIcon
                                                            variant="outline"
                                                            color={color}
                                                            onClick={() =>
                                                                onEntryClick(
                                                                    event.entryId,
                                                                )
                                                            }
                                                        >
                                                            <MdLink size={18} />
                                                        </ActionIcon>
                                                    )}
                                                </Group>
                                            </Paper>
                                        ),
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
