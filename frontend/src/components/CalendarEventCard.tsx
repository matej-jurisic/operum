import { ActionIcon, em, Group, Paper, Stack, Text, Indicator, ScrollArea } from "@mantine/core";
import { Calendar } from "@mantine/dates";
import { useMediaQuery } from "@mantine/hooks";
import { useState } from "react";
import { MdDelete, MdArrowBack } from "react-icons/md";
import { useTracker } from "../context/TrackerContext";
import { CalendarAnalyticResultDto } from "../model/AnalyticResultDto";
import { formatDateOnlyFromDate, formatDateTime, formatDateTimeFromDate } from "../util/TypeFormatter";

interface CalendarEventCardProps {
  analytic: CalendarAnalyticResultDto;
  isConfiguring: boolean;
}

export function CalendarEventCard({ analytic, isConfiguring }: CalendarEventCardProps) {
  const { tracker, RemoveAnalyticFromTracker } = useTracker();
  const isMobile = useMediaQuery(`(max-width: ${em(750)})`);
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

  // Format date for display
  const formatDate = (date: Date) => {
    return new Date(date).toLocaleDateString(undefined, {
      weekday: "long",
      month: "long",
      day: "numeric",
      year: "numeric",
    });
  };

  return (
    <Paper withBorder p="md" radius="md">
      <Stack gap="xs" >
        <Group justify="space-between" wrap="nowrap" align="flex-start">
          <Text size="sm">
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
          <Group w={"100%"} justify="center">
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
          <Stack gap="xs">
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
                {formatDate(selectedDate)}
              </Text>
            </Group>
            <ScrollArea h={isMobile ? 210 : 250}>
              {eventsForSelectedDate.length === 0 ? (
                <Text size="sm" c="dimmed" ta="center" py="xl">
                  No events on this date
                </Text>
              ) : (
                <Stack gap="xs" pr="xs">
                  {eventsForSelectedDate.map((event, index) => (
                    <Group
                      key={index}
                      gap="sm"
                      wrap="nowrap"
                      p="xs"
                      style={{
                        borderRadius: "6px",
                        backgroundColor: `var(--mantine-color-${tracker.color || "blue"}-0)`,
                        borderLeft: `3px solid var(--mantine-color-${tracker.color || "blue"}-6)`,
                      }}
                    >
                      <Text size="sm" style={{ flex: 1 }}>
                        {event.name} - {formatDateTime(event.date.toString())}
                      </Text>
                    </Group>
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