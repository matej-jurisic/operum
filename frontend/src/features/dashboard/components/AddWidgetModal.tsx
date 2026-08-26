import {
    Group,
    Modal,
    Stack,
    Text,
    ThemeIcon,
    UnstyledButton,
    useMantineTheme,
} from "@mantine/core";
import { IconType } from "react-icons";
import { CiFilter } from "react-icons/ci";
import { FiChevronRight, FiPlusSquare } from "react-icons/fi";
import { TbChartHistogram, TbLayoutGrid, TbTable } from "react-icons/tb";
import { useState } from "react";
import {
    AddDashboardEntriesItemDto,
    AddDashboardItemDto,
    AddDashboardItemFromAnalyticDto,
    AddDashboardQuickAddItemDto,
    AddDashboardViewItemDto,
} from "../types/DashboardDto";
import { CustomAnalyticForm } from "./CustomAnalyticForm";
import { EntriesWidgetForm } from "./EntriesWidgetForm";
import { ExistingAnalyticForm } from "./ExistingAnalyticForm";
import { QuickAddTrackerForm } from "./QuickAddTrackerForm";
import { ViewWidgetForm } from "./ViewWidgetForm";

interface Props {
    color: string;
    onClose: () => void;
    onAdd: (dto: AddDashboardItemDto) => Promise<void>;
    onAddFromAnalytic: (dto: AddDashboardItemFromAnalyticDto) => Promise<void>;
    onAddQuickAdd: (dto: AddDashboardQuickAddItemDto) => Promise<void>;
    onAddView: (dto: AddDashboardViewItemDto) => Promise<void>;
    onAddEntries: (dto: AddDashboardEntriesItemDto) => Promise<void>;
}

type WidgetKind = "existing" | "custom" | "quickAdd" | "view" | "entries";

interface WidgetKindOption {
    kind: WidgetKind;
    title: string;
    /** Only when the title alone does not say what the widget does. */
    description?: string;
    icon: IconType;
    /** Modal title once this kind is being configured. */
    formTitle: string;
}

// The board's menu of widget kinds. Everything a dashboard can hold is listed here, so a
// kind that isn't a chart (a button, a saved view, a note) is added by appending to this
// list and rendering its own form below.
const WIDGET_KINDS: WidgetKindOption[] = [
    {
        kind: "existing",
        title: "Existing analytic",
        icon: TbLayoutGrid,
        formTitle: "Add an existing analytic",
    },
    {
        kind: "custom",
        title: "New chart",
        icon: TbChartHistogram,
        formTitle: "Build a chart",
    },
    {
        kind: "quickAdd",
        title: "Quick add button",
        icon: FiPlusSquare,
        formTitle: "Add a quick-add button",
    },
    {
        kind: "view",
        title: "View selector",
        icon: CiFilter,
        formTitle: "Add a view selector",
    },
    {
        kind: "entries",
        title: "Entries table",
        description: "A read-only list of one tracker's entries",
        icon: TbTable,
        formTitle: "Add an entries table",
    },
];

export function AddWidgetModal({
    color,
    onClose,
    onAdd,
    onAddFromAnalytic,
    onAddQuickAdd,
    onAddView,
    onAddEntries,
}: Props) {
    const theme = useMantineTheme();
    const [kind, setKind] = useState<WidgetKind | null>(null);

    const selected = WIDGET_KINDS.find((option) => option.kind === kind);

    // Both forms leave the modal open on failure: the api layer has already said what went
    // wrong, and closing would throw away everything the user filled in.
    const submit =
        <T,>(handler: (dto: T) => Promise<void>) =>
        async (dto: T) => {
            await handler(dto);
            onClose();
        };

    return (
        <Modal
            opened
            onClose={onClose}
            title={selected?.formTitle ?? "Add a widget"}
            size="md"
            centered
        >
            {!selected && (
                <Stack gap="sm">
                    {WIDGET_KINDS.map((option) => (
                        <UnstyledButton
                            key={option.kind}
                            onClick={() => setKind(option.kind)}
                            p="md"
                            style={{
                                borderRadius: theme.radius.md,
                                border: `1px solid ${theme.colors.gray[6]}33`,
                            }}
                        >
                            <Group wrap="nowrap">
                                <ThemeIcon
                                    size={40}
                                    radius="md"
                                    variant="light"
                                    color={color}
                                >
                                    <option.icon size={22} />
                                </ThemeIcon>
                                <Stack gap={2} style={{ flex: 1 }}>
                                    <Text fw={600}>{option.title}</Text>
                                    {option.description && (
                                        <Text size="sm" c="dimmed">
                                            {option.description}
                                        </Text>
                                    )}
                                </Stack>
                                <FiChevronRight size={18} />
                            </Group>
                        </UnstyledButton>
                    ))}
                </Stack>
            )}

            {kind === "existing" && (
                <ExistingAnalyticForm
                    onBack={() => setKind(null)}
                    onAdd={submit(onAddFromAnalytic)}
                />
            )}

            {kind === "custom" && (
                <CustomAnalyticForm
                    onBack={() => setKind(null)}
                    onAdd={submit(onAdd)}
                />
            )}

            {kind === "quickAdd" && (
                <QuickAddTrackerForm
                    onBack={() => setKind(null)}
                    onAdd={submit(onAddQuickAdd)}
                />
            )}

            {kind === "view" && (
                <ViewWidgetForm
                    onBack={() => setKind(null)}
                    onAdd={submit(onAddView)}
                />
            )}

            {kind === "entries" && (
                <EntriesWidgetForm
                    onBack={() => setKind(null)}
                    onAdd={submit(onAddEntries)}
                />
            )}
        </Modal>
    );
}
