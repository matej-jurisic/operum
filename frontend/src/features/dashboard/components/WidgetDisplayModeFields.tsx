import { Group, SegmentedControl, Stack, Text } from "@mantine/core";
import { DashboardItemDisplayMode } from "../types/DashboardDto";

interface Props {
    displayMode: DashboardItemDisplayMode;
    mobileDisplayMode: DashboardItemDisplayMode;
    onDisplayModeChange: (value: DashboardItemDisplayMode) => void;
    onMobileDisplayModeChange: (value: DashboardItemDisplayMode) => void;
}

const OPTIONS = [
    { label: "Show", value: String(DashboardItemDisplayMode.Full) },
    { label: "Button", value: String(DashboardItemDisplayMode.Expandable) },
    { label: "Hidden", value: String(DashboardItemDisplayMode.Hidden) },
];

function ModeRow({
    label,
    value,
    onChange,
}: {
    label: string;
    value: DashboardItemDisplayMode;
    onChange: (value: DashboardItemDisplayMode) => void;
}) {
    return (
        <Group justify="space-between" wrap="nowrap">
            <Text size="sm">{label}</Text>
            <SegmentedControl
                size="xs"
                data={OPTIONS}
                value={String(value)}
                onChange={(next) => onChange(Number(next) as DashboardItemDisplayMode)}
            />
        </Group>
    );
}

/**
 * How an Analytic/Entries widget is drawn on each of the board's two grids, set on its
 * create/edit form. Shared rather than repeated across CustomAnalyticForm,
 * PlaceFromLibraryForm, EntriesWidgetForm, EditWidgetModal and EditEntriesWidgetModal.
 *
 * Button collapses the widget to a tile that opens it in a popup; Hidden drops it from
 * that grid entirely, leaving it reachable only from the board's hidden-widgets list.
 */
export function WidgetDisplayModeFields({
    displayMode,
    mobileDisplayMode,
    onDisplayModeChange,
    onMobileDisplayModeChange,
}: Props) {
    return (
        <Stack gap="xs">
            <Text size="sm" fw={500}>
                Display
            </Text>
            <Text size="xs" c="dimmed">
                Button shows a tile that opens the widget in a popup.
            </Text>
            <ModeRow
                label="Desktop"
                value={displayMode}
                onChange={onDisplayModeChange}
            />
            <ModeRow
                label="Mobile"
                value={mobileDisplayMode}
                onChange={onMobileDisplayModeChange}
            />
        </Stack>
    );
}
