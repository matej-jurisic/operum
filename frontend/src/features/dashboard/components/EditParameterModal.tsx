import { Modal } from "@mantine/core";
import { useMediaQuery } from "@mantine/hooks";
import { useDashboard } from "../context/DashboardContext";
import {
    ParameterWidgetConfig,
    SaveParameterItemDto,
    WidgetTypes,
} from "../types/DashboardDto";
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
        return typeof parsed?.viewId === "string"
            ? {
                  viewId: parsed.viewId,
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
    const config =
        widget?.type === WidgetTypes.Parameter ? parseConfig(widget.config) : null;

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
                initial={
                    config
                        ? {
                              viewId: config.viewId,
                              values: config.valueByQuery,
                              links: config.links,
                          }
                        : undefined
                }
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
