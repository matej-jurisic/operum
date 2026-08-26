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
import { MdOutlineHorizontalRule } from "react-icons/md";
import { TbChartHistogram, TbHeading, TbLayoutGrid, TbNote, TbTable } from "react-icons/tb";
import { useState } from "react";
import {
    AddDashboardHeaderItemDto,
    AddDashboardNoteItemDto,
    AddDashboardQuickAddItemDto,
    AddDashboardViewItemDto,
    CreateAndPlaceEntriesWidgetDto,
    CreateAndPlaceWidgetDto,
    PlaceEntriesWidgetDto,
    PlaceWidgetDto,
} from "../types/DashboardDto";
import { CustomAnalyticForm } from "./CustomAnalyticForm";
import { EntriesWidgetForm } from "./EntriesWidgetForm";
import { HeaderWidgetForm } from "./HeaderWidgetForm";
import { NoteWidgetForm } from "./NoteWidgetForm";
import { PlaceFromLibraryForm } from "./PlaceFromLibraryForm";
import { QuickAddTrackerForm } from "./QuickAddTrackerForm";
import { ViewWidgetForm } from "./ViewWidgetForm";

interface Props {
    color: string;
    onClose: () => void;
    onCreateAndPlaceWidget: (dto: CreateAndPlaceWidgetDto) => Promise<void>;
    onPlaceWidget: (dto: PlaceWidgetDto) => Promise<void>;
    onAddQuickAdd: (dto: AddDashboardQuickAddItemDto) => Promise<void>;
    onAddView: (dto: AddDashboardViewItemDto) => Promise<void>;
    onCreateAndPlaceEntriesWidget: (dto: CreateAndPlaceEntriesWidgetDto) => Promise<void>;
    onPlaceEntriesWidget: (dto: PlaceEntriesWidgetDto) => Promise<void>;
    onAddHeader: (dto: AddDashboardHeaderItemDto) => Promise<void>;
    onAddDivider: () => Promise<void>;
    onAddNote: (dto: AddDashboardNoteItemDto) => Promise<void>;
}

type WidgetKind =
    | "existing"
    | "custom"
    | "quickAdd"
    | "view"
    | "entries"
    | "header"
    | "divider"
    | "note";

interface WidgetKindOption {
    kind: WidgetKind;
    title: string;
    icon: IconType;
    /** Modal title once this kind is being configured. Unused by a kind (Divider) that
        never reaches a form of its own. */
    formTitle?: string;
}

// The board's menu of widget kinds. Everything a dashboard can hold is listed here, so a
// kind that isn't a chart (a button, a saved view, a note) is added by appending to this
// list and rendering its own form below.
const WIDGET_KINDS: WidgetKindOption[] = [
    {
        kind: "existing",
        title: "From Widget Library",
        icon: TbLayoutGrid,
        formTitle: "Add from the Widget Library",
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
        icon: TbTable,
        formTitle: "Add an entries table",
    },
    {
        kind: "header",
        title: "Header",
        icon: TbHeading,
        formTitle: "Add a header",
    },
    {
        kind: "divider",
        title: "Divider",
        icon: MdOutlineHorizontalRule,
        // Nothing to configure, so this never opens a form — see handleSelect below.
    },
    {
        kind: "note",
        title: "Note",
        icon: TbNote,
        formTitle: "Add a note",
    },
];

export function AddWidgetModal({
    color,
    onClose,
    onCreateAndPlaceWidget,
    onPlaceWidget,
    onAddQuickAdd,
    onAddView,
    onCreateAndPlaceEntriesWidget,
    onPlaceEntriesWidget,
    onAddHeader,
    onAddDivider,
    onAddNote,
}: Props) {
    const theme = useMantineTheme();
    const [kind, setKind] = useState<WidgetKind | null>(null);
    const [isAddingDivider, setIsAddingDivider] = useState(false);

    const selected = WIDGET_KINDS.find((option) => option.kind === kind);

    // Both forms leave the modal open on failure: the api layer has already said what went
    // wrong, and closing would throw away everything the user filled in.
    const submit =
        <T,>(handler: (dto: T) => Promise<void>) =>
        async (dto: T) => {
            await handler(dto);
            onClose();
        };

    // A Divider has nothing to configure, so picking it adds it immediately instead of
    // stepping into a form with nothing in it.
    const handleSelect = async (option: WidgetKindOption) => {
        if (option.kind !== "divider") {
            setKind(option.kind);
            return;
        }

        setIsAddingDivider(true);
        try {
            await onAddDivider();
            onClose();
        } finally {
            setIsAddingDivider(false);
        }
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
                            onClick={() => handleSelect(option)}
                            disabled={option.kind === "divider" && isAddingDivider}
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
                                <Text fw={600} style={{ flex: 1 }}>
                                    {option.title}
                                </Text>
                                <FiChevronRight size={18} />
                            </Group>
                        </UnstyledButton>
                    ))}
                </Stack>
            )}

            {kind === "existing" && (
                <PlaceFromLibraryForm
                    onBack={() => setKind(null)}
                    onPlaceWidget={submit(onPlaceWidget)}
                    onPlaceEntriesWidget={submit(onPlaceEntriesWidget)}
                />
            )}

            {kind === "custom" && (
                <CustomAnalyticForm
                    onBack={() => setKind(null)}
                    onAdd={submit(onCreateAndPlaceWidget)}
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
                    onAdd={submit(onCreateAndPlaceEntriesWidget)}
                />
            )}

            {kind === "header" && (
                <HeaderWidgetForm
                    onBack={() => setKind(null)}
                    onAdd={submit(onAddHeader)}
                />
            )}

            {kind === "note" && (
                <NoteWidgetForm
                    onBack={() => setKind(null)}
                    onAdd={submit(onAddNote)}
                />
            )}
        </Modal>
    );
}
