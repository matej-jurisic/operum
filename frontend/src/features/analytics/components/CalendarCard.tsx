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
import { MdArrowBack, MdDelete, MdLink } from "react-icons/md";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import {
    formatDateTime,
    formatFullDate,
} from "../../../shared/utils/formatters/TypeFormatter";
import { useTracker } from "../../trackers/context/TrackerContext";
import { CalendarAnalyticResultDto } from "../types/AnalyticDto";

interface CalendarCardProps {
    analytic: CalendarAnalyticResultDto;
    isConfiguring: boolean;
    onEntryClick: (entryId: string) => void;
}

const getDateKey = (date: Date): string => {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
};

export function CalendarCard({
    analytic,
    isConfiguring,
    onEntryClick,
}: CalendarCardProps) {
    const { tracker } = useTracker();
    const { removeAnalytic } = useTrackerOperations();
    const [selectedDate, setSelectedDate] = useState<Date | undefined>();
    const [viewDate, setViewDate] = useState<Date>(new Date());

    // Create a map of dates to events for quick lookup
    const events = useMemo(() => {
        const eventsByDate = new Map<string, typeof analytic.points>();
        analytic.points.forEach((event) => {
            if (!event.date) return;

            const dateObj = new Date(event.date);
            const dateKey = getDateKey(dateObj);

            console.log(
                "Event:",
                event.name,
                "Date:",
                event.date,
                "Key:",
                dateKey
            );

            if (!eventsByDate.has(dateKey)) {
                eventsByDate.set(dateKey, []);
            }
            eventsByDate.get(dateKey)!.push(event);
        });

        console.log("All event keys:", Array.from(eventsByDate.keys()));
        return eventsByDate;
    }, [analytic.points]);

    // Get events for selected date
    const selectedDateKey = selectedDate ? getDateKey(selectedDate) : "";
    const eventsForSelectedDate = events.get(selectedDateKey) || [];

    return (
        <Paper withBorder p="md" radius="md">
            <Stack gap="xs">
                <Group justify="space-between" wrap="nowrap" align="flex-start">
                    <Text size="sm" mb={"sm"}>
                        {`${analytic.name}: ${analytic.dateFieldName} - ${analytic.eventFieldName}`}
                    </Text>
                    {isConfiguring && (
                        <ActionIcon
                            size="md"
                            color={tracker.color}
                            variant="outline"
                            onClick={() => removeAnalytic(analytic.id)}
                        >
                            <MdDelete size={18} />
                        </ActionIcon>
                    )}
                </Group>

                {!selectedDate ? (
                    <Group w={"100%"} justify="center" h={280}>
                        <Calendar
                            date={viewDate}
                            onDateChange={(date) => setViewDate(new Date(date))}
                            size={"sm"}
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
                                        size={6}
                                        color={tracker.color || "blue"}
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
                    <Stack gap="xs" h={280}>
                        <Group gap="xs">
                            <ActionIcon
                                size="sm"
                                variant="subtle"
                                color={tracker.color || "blue"}
                                onClick={() => setSelectedDate(undefined)}
                            >
                                <MdArrowBack size={16} />
                            </ActionIcon>
                            <Text size="sm" fw={500}>
                                {formatFullDate(selectedDate)}
                            </Text>
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
                                                        tracker.color || "blue"
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
                                                            {formatDateTime(
                                                                event.date
                                                            )}
                                                        </Text>
                                                    </Stack>
                                                    <ActionIcon
                                                        color={tracker.color}
                                                        onClick={() =>
                                                            onEntryClick(
                                                                event.entryId
                                                            )
                                                        }
                                                    >
                                                        <MdLink size={18} />
                                                    </ActionIcon>
                                                </Group>
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
