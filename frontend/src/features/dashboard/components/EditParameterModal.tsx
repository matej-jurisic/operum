import { Modal } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { QueryKinds } from "../../../shared/constants/QueryKinds";
import { useDashboard } from "../context/DashboardContext";
import {
    ParameterWidgetConfig,
    SaveParameterItemDto,
    ViewSelectorLink,
    WidgetTypes,
} from "../types/DashboardDto";
import { AbstractClauseRow } from "./AbstractClauseListEditor";
import { ParameterForm } from "./ParameterForm";

interface Props {
    itemId: string;
    color: string;
    onClose: () => void;
    onSave: (itemId: string, dto: SaveParameterItemDto) => Promise<void>;
}

function parseConfig(config: string | undefined): ParameterWidgetConfig | null {
    if (!config) return null;
    try {
        const parsed = JSON.parse(config);
        return Array.isArray(parsed?.queryIds)
            ? {
                  queryIds: parsed.queryIds,
                  valueByQuery: parsed.valueByQuery ?? {},
                  links: parsed.links ?? [],
              }
            : null;
    } catch {
        return null;
    }
}

export function EditParameterModal({ itemId, color, onClose, onSave }: Props) {
    const { widgets } = useDashboard();
    const isMobile = useMediaQuery("(max-width: 48em)");
    const widget = widgets.find((w) => w.id === itemId);
    const isParameter = widget?.type === WidgetTypes.Parameter;
    const config = isParameter ? parseConfig(widget.config) : null;
    const clauseDtos = (isParameter && widget.parameter?.clauses) || [];

    // The form works in clause indices; the stored links are keyed by pooled query id, so
    // translate them back through the clause order the widget reports.
    const indexByQueryId = new Map<string, string>(
        clauseDtos.map((c, i) => [c.queryId, String(i)]),
    );

    const clauses: AbstractClauseRow[] = clauseDtos.map((c) => ({
        kind: QueryKinds.Filter,
        dataType: c.dataType,
        operator: c.operator ?? "",
        value: c.value ?? undefined,
        descending: false,
    }));

    const links: ViewSelectorLink[] = (config?.links ?? []).map((l) => ({
        itemId: l.itemId,
        trackerId: l.trackerId,
        fieldByQuery: Object.fromEntries(
            Object.entries(l.fieldByQuery).flatMap(([queryId, fieldId]) => {
                const index = indexByQueryId.get(queryId);
                return index ? [[index, fieldId]] : [];
            }),
        ),
    }));

    return (
        <Modal
            opened
            onClose={onClose}
            title="Edit parameter widget"
            size="lg"
            centered
            fullScreen={isMobile}
        >
            <ParameterForm
                initial={{ clauses, links }}
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
