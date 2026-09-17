import { Modal } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { QueryKinds } from "../../../shared/constants/QueryKinds";
import { useDashboard } from "../context/DashboardContext";
import {
    parseFilterWidgetConfig,
    SaveFilterItemDto,
    WidgetLink,
    WidgetTypes,
} from "../types/DashboardDto";
import { AbstractClauseRow } from "./AbstractClauseListEditor";
import { FilterForm } from "./FilterForm";

interface Props {
    itemId: string;
    color: string;
    onClose: () => void;
    onSave: (itemId: string, dto: SaveFilterItemDto) => Promise<void>;
}

export function EditFilterModal({ itemId, color, onClose, onSave }: Props) {
    const { widgets } = useDashboard();
    const isMobile = useMediaQuery("(max-width: 48em)");
    const widget = widgets.find((w) => w.id === itemId);
    const isFilter = widget?.type === WidgetTypes.Filter;
    const config = isFilter ? parseFilterWidgetConfig(widget.config) : null;
    const clauseDtos = (isFilter && widget.filter?.clauses) || [];

    // Links are keyed by clause slot id, but the form works in clause indices; translate through the widget's clause order.
    const indexBySlotId = new Map<string, string>(
        clauseDtos.map((c, i) => [c.slotId, String(i)]),
    );

    const clauses: AbstractClauseRow[] = clauseDtos.map((c) => ({
        kind: QueryKinds.Filter,
        dataType: c.dataType,
        operator: c.operator ?? "",
        value: c.value ?? undefined,
        descending: false,
    }));

    const links: WidgetLink[] = (config?.links ?? []).map((l) => ({
        itemId: l.itemId,
        trackerId: l.trackerId,
        fieldByQuery: Object.fromEntries(
            Object.entries(l.fieldByQuery).flatMap(([slotId, fieldId]) => {
                const index = indexBySlotId.get(slotId);
                return index !== undefined ? [[index, fieldId]] : [];
            }),
        ),
    }));

    return (
        <Modal
            opened
            onClose={onClose}
            title="Edit filter widget"
            size="lg"
            centered
            fullScreen={isMobile}
        >
            <FilterForm
                initial={{
                    clauses,
                    links,
                    presetIds: config?.presetIds ?? [],
                }}
                submitLabel="Save"
                color={color}
                onBack={onClose}
                onSubmit={async (dto) => {
                    await onSave(itemId, dto);
                    onClose();
                }}
            />
        </Modal>
    );
}
