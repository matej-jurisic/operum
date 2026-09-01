import { Center, Paper, Select, Stack, Text } from "@mantine/core";
import { TbFilter } from "react-icons/tb";
import { AnalyticCardHeader } from "../../analytics/components/AnalyticCardHeader";
import {
    cardBodyProps,
    cardShellProps,
    useCardLayout,
} from "../../analytics/components/cardSizing";
import { ViewSelectorWidgetDto } from "../types/DashboardDto";

const NONE_VALUE = "";

interface Props {
    widgetId: string;
    /** The options + current selection, resolved by the board itself. */
    viewSelector: ViewSelectorWidgetDto | undefined;
    color: string | undefined;
    isConfiguring: boolean;
    onRemove?: (itemId: string) => void;
    /** Opens the selector's edit dialog: its options and which widgets follow it. */
    onEdit?: (itemId: string) => void;
    /** Persists the new selection and recomputes every widget linked to this one. */
    onSelect: (itemId: string, selectedId: string | null) => void;
}

/**
 * A board widget that is a live, swappable filter rather than a chart: its dropdown picks
 * one of the board's DashboardViews ("Current Month", "All Time"), and every Analytic
 * widget wired to it is recalculated against the picked clause set. The selection is saved
 * on the widget itself, so it is what every viewer sees on the next load too.
 */
export function ViewSelectorWidgetCard({
    widgetId,
    viewSelector,
    color,
    isConfiguring,
    onRemove,
    onEdit,
    onSelect,
}: Props) {
    const layout = useCardLayout(true);

    const options = [
        { value: NONE_VALUE, label: "None" },
        ...(viewSelector?.options.map((o) => ({ value: o.id, label: o.name })) ?? []),
    ];

    return (
        <Paper
            ref={layout.ref}
            withBorder={isConfiguring}
            p={0}
            radius="md"
            w="100%"
            {...cardShellProps(true)}
        >
            <Stack
                gap="xs"
                justify="center"
                {...cardBodyProps(true)}
                h="100%"
                style={{ position: "relative" }}
            >
                <AnalyticCardHeader
                    title="Filter"
                    layout={layout}
                    color={color}
                    isConfiguring={isConfiguring}
                    analyticId={widgetId}
                    onRemove={onRemove}
                    onEdit={onEdit}
                    compact
                />
                <Center
                    style={{
                        flex: 1,
                        minHeight: 0,
                        zIndex: 1,
                        pointerEvents: isConfiguring ? "none" : "auto",
                    }}
                >
                    {viewSelector ? (
                        <Select
                            w="100%"
                            h="100%"
                            leftSection={<TbFilter size={16} />}
                            data={options}
                            value={viewSelector.selectedId ?? NONE_VALUE}
                            onChange={(value) =>
                                onSelect(
                                    widgetId,
                                    value && value !== NONE_VALUE ? value : null,
                                )
                            }
                            disabled={isConfiguring}
                            allowDeselect={false}
                            comboboxProps={{ withinPortal: true }}
                            // Fill the whole cell rather than sitting as a fixed-height
                            // control centered in it, the way the quick-add button does --
                            // otherwise the widget reads as inset from its neighbours.
                            styles={{
                                wrapper: { height: "100%" },
                                // Match the shell's corners: the input reaches the
                                // Paper's edges, and its square bottom corners would
                                // otherwise be sliced off by the rounded overflow clip.
                                input: {
                                    height: "100%",
                                    borderBottomLeftRadius:
                                        "var(--mantine-radius-md)",
                                    borderBottomRightRadius:
                                        "var(--mantine-radius-md)",
                                },
                            }}
                        />
                    ) : (
                        <Text size="sm" c="dimmed" ta="center">
                            This selector is misconfigured.
                        </Text>
                    )}
                </Center>
            </Stack>
        </Paper>
    );
}
