import { ActionIcon, Group, Paper, Stack, Text, Indicator, ScrollArea } from "@mantine/core";
import { Calendar } from "@mantine/dates";
import { useState } from "react";
import { MdDelete, MdArrowBack } from "react-icons/md";
import { useTracker } from "../context/TrackerContext";
import { CalendarAnalyticResultDto } from "../model/AnalyticResultDto";
import { formatDateTime, formatFullDate } from "../util/TypeFormatter";

interface CalendarEventCardProps {
  analytic: CalendarAnalyticResultDto;
  isConfiguring: boolean;
}

export function CalendarEventCard({ analytic, isConfiguring }: CalendarEventCardProps) {
  const { tracker, RemoveAnalyticFromTracker } = useTracker();
  const [selectedDate, setSelectedDate] = useState<Date | undefined>();
  const [viewDate, setViewDate] = useState<Date>(new Date());

  // Create a map of dates to events for quick lookup
  const eventsByDate = new Map<string, typeof analytic.points>();
  analytic.points.forEach((event) => {
    const dateKey = new Date(event.date).toDateString();
    if (!eventsByDate.has(dateKey)) {
      eventsByDate.set(dateKey, []);
    }
    eventsByDate.get(dateKey)!.push(event);
  });

  // Get events for selected date
  const selectedDateKey = selectedDate?.toDateString() || "";
  const eventsForSelectedDate = eventsByDate.get(selectedDateKey) || [];

  return (
    <Paper withBorder p="md" radius="md">
      <Stack gap="xs"  >
        <Group justify="space-between" wrap="nowrap" align="flex-start">
          <Text size="sm" mb={"sm"}>
            {`${analytic.name}: ${analytic.dateFieldName} - ${analytic.eventFieldName}`}
          </Text>
          {isConfiguring && (
            <ActionIcon
              size="md"
              color={tracker.color}
              variant="outline"
              onClick={() =>
                RemoveAnalyticFromTracker(analytic.trackerAnalyticId)
              }
            >
              <MdDelete size={18} />
            </ActionIcon>
          )}
        </Group>

        {!selectedDate ? (
          <Group w={"100%"} justify="center" h={250}>
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
                const dateKey = dateObj.toDateString();
                const hasEvents = eventsByDate.has(dateKey);
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
          <Stack gap="xs" h={250}>
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
                  {eventsForSelectedDate.map((event, index) => (
                    <Paper
                      key={index}
                      withBorder
                      p="xs"
                      style={{
                        borderRadius: "6px",
                        borderLeft: `3px solid var(--mantine-color-${tracker.color || "blue"}-6)`,
                      }}
                    >
                      <Text size="sm" style={{ flex: 1 }}>
                        {event.name} - {formatDateTime(event.date.toString())}
                      </Text>
                    </Paper>
                  ))}
                </Stack>
              )}
            </ScrollArea>
          </Stack>
        )}
      </Stack>
    </Paper>
  );
}
