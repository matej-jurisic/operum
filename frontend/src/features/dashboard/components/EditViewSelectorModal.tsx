import { Modal } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useDashboard } from "../context/DashboardContext";
import {
    SaveViewSelectorItemDto,
    ViewSelectorWidgetConfig,
    WidgetTypes,
} from "../types/DashboardDto";
import { ViewSelectorForm } from "./ViewSelectorForm";

interface Props {
    itemId: string;
    color: string;
    onClose: () => void;
    onSave: (itemId: string, dto: SaveViewSelectorItemDto) => Promise<void>;
}

function parseConfig(config: string | undefined): ViewSelectorWidgetConfig | null {
    if (!config) return null;
    try {
        const parsed = JSON.parse(config);
        return Array.isArray(parsed?.optionIds)
            ? {
                  optionIds: parsed.optionIds,
                  selectedId: parsed.selectedId ?? null,
                  links: parsed.links ?? [],
              }
            : null;
    } catch {
        return null;
    }
}

export function EditViewSelectorModal({ itemId, color, onClose, onSave }: Props) {
    const { widgets } = useDashboard();
    const isMobile = useMediaQuery("(max-width: 48em)");
    const widget = widgets.find((w) => w.id === itemId);
    const config =
        widget?.type === WidgetTypes.ViewSelector
            ? parseConfig(widget.config)
            : null;

    return (
        <Modal
            opened
            onClose={onClose}
            title="Edit filter selector"
            size="lg"
            centered
            fullScreen={isMobile}
        >
            <ViewSelectorForm
                initial={config ?? undefined}
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
