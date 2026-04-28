import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import {
    ActionIcon,
    Badge,
    Card,
    Group,
    Menu,
    Stack,
    Text,
    ThemeIcon,
    Title,
} from "@mantine/core";
import { createElement, CSSProperties } from "react";
import { FiMoreVertical, FiPlus } from "react-icons/fi";
import { MdDelete, MdDragHandle, MdEdit } from "react-icons/md";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import globalStore from "../../../shared/stores/GlobalStore";
import { TrackerDto } from "../types/TrackerDto";

interface SortableTrackerCardProps {
    tracker: TrackerDto;
    color: string;
    isReordering: boolean;
    isTemplates: boolean;
    onNavigate: (tracker: TrackerDto) => void;
    onQuickAdd: (tracker: TrackerDto) => void;
    onEdit: (tracker: TrackerDto) => void;
    onDelete: (tracker: TrackerDto) => void;
}

const hasInputtableFields = (t: TrackerDto) => t.fields.some((f) => !f.isCalculated);

export default function SortableTrackerCard({
    tracker,
    color,
    isReordering,
    isTemplates,
    onNavigate,
    onQuickAdd,
    onEdit,
    onDelete,
}: SortableTrackerCardProps) {
    const {
        attributes,
        listeners,
        setNodeRef,
        transform,
        transition,
        isDragging,
    } = useSortable({ id: tracker.id });

    const style = {
        transform: CSS.Translate.toString(transform),
        transition,
        opacity: isDragging ? 0.5 : 1,
    } as CSSProperties;

    const isOwner = globalStore.currentUser?.id === tracker.ownerId;

    return (
        <Card
            ref={setNodeRef}
            style={{
                ...style,
                borderTop: `3px solid var(--mantine-color-${color}-5)`,
                cursor: isReordering ? "default" : "pointer",
            }}
            shadow="sm"
            padding="lg"
            radius="md"
            withBorder
            onClick={() => !isReordering && onNavigate(tracker)}
        >
            <Group gap="md" align="center" wrap="nowrap">
                {isReordering && (
                    <ActionIcon
                        variant="subtle"
                        color={color}
                        size="xl"
                        {...attributes}
                        {...listeners}
                        style={{ cursor: "grab", alignSelf: "center", touchAction: "none" }}
                        aria-label={`Drag to reorder tracker ${tracker.name}`}
                    >
                        <MdDragHandle size={25} />
                    </ActionIcon>
                )}
                <ThemeIcon
                    size={44}
                    radius="md"
                    variant="light"
                    color={color}
                    style={{ flexShrink: 0 }}
                >
                    {createElement(resolveTrackerIcon(tracker.icon), { size: 22 })}
                </ThemeIcon>
                <Stack gap={4} flex={1} style={{ minWidth: 0 }}>
                    <Group justify="space-between" align="center" wrap="nowrap" gap="xs">
                        <Title
                            order={4}
                            lineClamp={1}
                            className="wrapped-text"
                            style={{ minWidth: 0 }}
                        >
                            {tracker.name}
                        </Title>
                        {!isReordering && (
                            <Group gap="xs" wrap="nowrap" style={{ flexShrink: 0 }}>
                                {isOwner ? (
                                    <>
                                        {!isTemplates && hasInputtableFields(tracker) && (
                                            <ActionIcon
                                                size="lg"
                                                variant="outline"
                                                color={color}
                                                onClick={(e) => {
                                                    e.stopPropagation();
                                                    onQuickAdd(tracker);
                                                }}
                                            >
                                                <FiPlus size={18} />
                                            </ActionIcon>
                                        )}
                                        <Menu shadow="md" position="bottom-end" withinPortal>
                                            <Menu.Target>
                                                <ActionIcon
                                                    size="lg"
                                                    variant="outline"
                                                    color="gray"
                                                    onClick={(e) => e.stopPropagation()}
                                                >
                                                    <FiMoreVertical size={18} />
                                                </ActionIcon>
                                            </Menu.Target>
                                            <Menu.Dropdown>
                                                <Menu.Item
                                                    leftSection={<MdEdit size={16} />}
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        onEdit(tracker);
                                                    }}
                                                >
                                                    Edit
                                                </Menu.Item>
                                                <Menu.Item
                                                    color="red"
                                                    leftSection={<MdDelete size={16} />}
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        onDelete(tracker);
                                                    }}
                                                >
                                                    Delete
                                                </Menu.Item>
                                            </Menu.Dropdown>
                                        </Menu>
                                    </>
                                ) : tracker.currentUserCanEditData && hasInputtableFields(tracker) && !isTemplates ? (
                                    <>
                                        <ActionIcon
                                            size="lg"
                                            variant="outline"
                                            color={color}
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                onQuickAdd(tracker);
                                            }}
                                        >
                                            <FiPlus size={18} />
                                        </ActionIcon>
                                        <Badge variant="outline">
                                            Owned by: {tracker.ownerName}
                                        </Badge>
                                    </>
                                ) : (
                                    <Badge variant="outline">
                                        Owned by: {tracker.ownerName}
                                    </Badge>
                                )}
                            </Group>
                        )}
                    </Group>
                    <Text c="dimmed" size="sm" className="wrapped-text" lineClamp={2}>
                        {tracker.description || "No description"}
                    </Text>
                </Stack>
            </Group>
        </Card>
    );
}
