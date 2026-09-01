import {
    Button,
    Group,
    Modal,
    Paper,
    Stack,
    Text,
    TextInput,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { viewsController } from "../../views/api/viewsController";
import { ViewDto } from "../../views/types/ViewDto";
import { dashboardController } from "../api/dashboardController";
import { useDashboard } from "../context/DashboardContext";
import {
    DashboardItemSourceDto,
    UpdateDashboardItemDto,
    WidgetTypes,
} from "../types/DashboardDto";
import { ExpandableOptionFields } from "./ExpandableOptionFields";
import { SourceViewSelect } from "./SourceViewSelect";

interface Props {
    itemId: string;
    color: string;
    onClose: () => void;
    onSave: (itemId: string, dto: UpdateDashboardItemDto) => Promise<void>;
}

/** One source's editable half, alongside the parts of it the form only shows. */
interface SourceRow {
    source: DashboardItemSourceDto;
    label: string;
    viewId: string | null;
    views: ViewDto[];
}

/**
 * Edits a chart widget after it has been placed. Only what the board itself decides is
 * here: what the widget is called, and which view each of its sources reads through. The
 * chart it draws is the definition it was added with, so changing that means adding a new
 * widget rather than quietly turning this one into something else.
 */
export function EditWidgetModal({ itemId, color, onClose, onSave }: Props) {
    const { dashboardId } = useDashboard();
    const [rows, setRows] = useState<SourceRow[] | null>(null);
    const [expandable, setExpandable] = useState(false);
    const [mobileExpandable, setMobileExpandable] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);

    // The board's render endpoint carries the calculated charts, not the definitions
    // behind them, so the sources being edited are read from the dashboard itself.
    useEffect(() => {
        const load = async () => {
            const res = await dashboardController.getDashboard(dashboardId);
            const item = res.data?.items.find((i) => i.id === itemId);

            if (!item || item.type !== WidgetTypes.Analytic) {
                onClose();
                return;
            }

            const sources = [...item.sources].sort((a, b) => a.order - b.order);
            const viewsByTracker = new Map<string, ViewDto[]>();

            await Promise.all(
                [...new Set(sources.map((s) => s.trackerId))].map(
                    async (trackerId) => {
                        const views =
                            await viewsController.getViewList(trackerId);
                        viewsByTracker.set(trackerId, views.data ?? []);
                    },
                ),
            );

            setRows(
                sources.map((source) => ({
                    source,
                    label: source.label ?? "",
                    viewId: source.viewId ?? null,
                    views: viewsByTracker.get(source.trackerId) ?? [],
                })),
            );
            setExpandable(item.layout.expandable);
            setMobileExpandable(item.mobileLayout.expandable);
        };

        load();
    }, [dashboardId, itemId, onClose]);

    const updateRow = (index: number, changes: Partial<SourceRow>) =>
        setRows((current) =>
            current
                ? current.map((row, i) =>
                      i === index ? { ...row, ...changes } : row,
                  )
                : current,
        );

    const handleSubmit = async () => {
        if (!rows) return;

        setIsSubmitting(true);
        try {
            // Every source, every time: the payload stands for the whole widget, so a
            // name or a view cleared here has to arrive as cleared rather than missing.
            await onSave(itemId, {
                expandable,
                mobileExpandable,
                sources: rows.map((row) => ({
                    sourceId: row.source.id,
                    label: row.label.trim() || null,
                    viewId: row.viewId,
                })),
            });
        } finally {
            setIsSubmitting(false);
        }

        onClose();
    };

    const isCombined = (rows?.length ?? 0) > 1;

    return (
        <Modal opened onClose={onClose} title="Edit widget" size="md" centered>
            {/* The global request loader already covers the fetch above, so this renders
                nothing rather than stacking a second spinner on top of it. */}
            {rows && (
                <Stack gap="md">
                    {rows.map((row, index) => {
                        const nameInput = (
                            <TextInput
                                label={isCombined ? "Series name" : "Name"}
                                description={
                                    isCombined ? "Names this series in the chart's legend" : undefined
                                }
                                placeholder={row.source.name}
                                maxLength={100}
                                value={row.label}
                                onChange={(event) =>
                                    updateRow(index, {
                                        label: event.currentTarget.value,
                                    })
                                }
                            />
                        );

                        const viewSelect = (
                            <SourceViewSelect
                                views={row.views}
                                value={{ viewId: row.viewId }}
                                onChange={(selection) =>
                                    updateRow(index, selection)
                                }
                            />
                        );

                        // A single source is the widget, so its fields are the form. Only
                        // once there are several does each one need saying which tracker
                        // it belongs to.
                        return isCombined ? (
                            <Paper
                                key={row.source.id}
                                withBorder
                                p="sm"
                                radius="md"
                            >
                                <Stack gap="sm">
                                    <Stack gap={0}>
                                        <Text size="sm" fw={600}>
                                            {row.source.trackerName}
                                        </Text>
                                        <Text size="xs" c="dimmed">
                                            {row.source.name}
                                        </Text>
                                    </Stack>
                                    {nameInput}
                                    {viewSelect}
                                </Stack>
                            </Paper>
                        ) : (
                            <Stack key={row.source.id} gap="md">
                                {nameInput}
                                {viewSelect}
                            </Stack>
                        );
                    })}

                    <ExpandableOptionFields
                        expandable={expandable}
                        mobileExpandable={mobileExpandable}
                        onExpandableChange={setExpandable}
                        onMobileExpandableChange={setMobileExpandable}
                    />

                    <Group justify="flex-end" mt="sm">
                        <Button variant="default" onClick={onClose}>
                            Cancel
                        </Button>
                        <Button
                            color={color}
                            loading={isSubmitting}
                            onClick={handleSubmit}
                        >
                            Save
                        </Button>
                    </Group>
                </Stack>
            )}
        </Modal>
    );
}
