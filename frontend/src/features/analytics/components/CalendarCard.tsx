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
    cardTitle,
    chartHeight,
    useCardLayout,
} from "./cardSizing";

interface CalendarCardProps {
    analytic: CalendarAnalyticDto;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (analyticId: string) => void;
    onRename?: (analyticId: string) => void;
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
    onRename,
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

    const subtitle = `${analytic.whenField.name} - ${analytic.whatField.name}`;

    // Get events for selected date
    const selectedDateKey = selectedDate ? getDateKey(selectedDate) : "";
    const eventsForSelectedDate = events.get(selectedDateKey) || [];

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
                    title={cardTitle(layout, analytic.name, subtitle)}
                    fullTitle={`${analytic.name}: ${subtitle}`}
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={analytic.id}
                    onRemove={onRemove}
                    onRename={onRename}
                />

                {!selectedDate ? (
                    <Group
                        w={"100%"}
                        justify="center"
                        h={chartHeight(fillHeight)}
                        style={{
                            ...cardBodyProps(fillHeight).style,
                            overflow: "auto",
                        }}
                    >
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
                                const hasEvents = events.has(dateKey);
                                const day = dateObj.getDate();

                                return (
                                    <Indicator
                                        size={9}
                                        color={color || "blue"}
                                        offset={-2}
                                        disabled={!hasEvents}
                                    >
                                        <div>{day}</div>
                                    </Indicator>
                                );
                            }}
                        />
                    </Group>
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
                                                        color || "blue"
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
                                                        </Text>
                                                    </Stack>
                                                    {onEntryClick && (
                                                        <ActionIcon
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
