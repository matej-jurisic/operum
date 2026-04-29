import {
    closestCenter,
    DndContext,
    DragEndEvent,
    KeyboardSensor,
    PointerSensor,
    useSensor,
    useSensors,
} from "@dnd-kit/core";
import { restrictToParentElement } from "@dnd-kit/modifiers";
import {
    arrayMove,
    SortableContext,
    sortableKeyboardCoordinates,
    verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import {
    ActionIcon,
    Button,
    Group,
    Menu,
    ScrollArea,
    Select,
    Stack,
    Text,
    ThemeIcon,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { observer } from "mobx-react";
import { CiSquarePlus } from "react-icons/ci";
import { FiPlus, FiZap } from "react-icons/fi";
import { RiListOrdered2 } from "react-icons/ri";
import { TbLayoutGrid } from "react-icons/tb";
import { useNavigate } from "react-router-dom";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import Header from "../../../shared/components/Header";
import {
    TrackerFilters,
    TrackerFiltersForSelect,
} from "../../../shared/constants/TrackerFilters";
import { trackersController } from "../api/trackersController";
import { TrackerDto } from "../types/TrackerDto";
import QuickAddEntryDialog from "../../entries/components/QuickAddEntryDialog";
import TrackerFormDialog from "./TrackerFormDialog";
import TrackerWizard from "./TrackerWizard";
import SortableTrackerCard from "./SortableTrackerCard";

enum OpenDialogType {
    CreateTracker,
    DeleteTracker,
    UpdateTracker,
    CreateFromTemplate,
    Wizard,
    QuickAddEntry,
}

interface Props {
    isTemplates?: boolean;
}

const Trackers = observer(function Trackers({ isTemplates = false }: Props) {
    const [trackerList, setTrackerList] = useState<TrackerDto[]>([]);
    const [selectedTracker, setSelectedTracker] = useState<TrackerDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [selectedFilter, setSelectedFilter] = useState<string>(
        TrackerFilters.Owned
    );
    const [isLoading, setIsLoading] = useState(true);
    const [isReordering, setIsReordering] = useState(false);
    const theme = useMantineTheme();
    const navigate = useNavigate();

    const sensors = useSensors(
        useSensor(PointerSensor, { activationConstraint: { distance: 4 } }),
        useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
    );

    const GetData = async () => {
        setIsLoading(true);
        let response;
        if (isTemplates) {
            response = await trackersController.getAdminTemplateList();
        } else {
            response = await trackersController.getTrackerList(selectedFilter);
        }
        setTrackerList(response.data);
        setIsLoading(false);
    };

    useEffect(() => {
        GetData();
    }, []);

    const handleDragEnd = async (event: DragEndEvent) => {
        const { active, over } = event;
        if (over && active.id !== over.id) {
            const oldIndex = trackerList.findIndex((t) => t.id === active.id);
            const newIndex = trackerList.findIndex((t) => t.id === over.id);
            const newList = arrayMove(trackerList, oldIndex, newIndex);
            setTrackerList(newList);
            try {
                await trackersController.reorderTrackers(newList.map((t) => t.id), selectedFilter);
            } catch {
                setTrackerList([...trackerList]);
            }
        }
    };

    return (
        <>
            <Stack gap="md" h="100%">
                {!isTemplates && (
                    <Group w="100%" justify="space-between">
                        <Title order={2} c={theme.primaryColor}>
                            Operum
                        </Title>
                        <Header />
                    </Group>
                )}
                <Group justify="space-between" align="flex-end">
                    {!isTemplates && (
                        <Select
                            value={selectedFilter}
                            w={150}
                            label="Filter"
                            data={TrackerFiltersForSelect}
                            allowDeselect={false}
                            onChange={async (e) => {
                                if (e) {
                                    setSelectedFilter(e);
                                    setIsReordering(false);
                                    setIsLoading(true);
                                    const response =
                                        await trackersController.getTrackerList(e);
                                    setTrackerList(response.data);
                                    setIsLoading(false);
                                }
                            }}
                        />
                    )}
                    <Group gap="xs">
                        {!isTemplates && (
                            <ActionIcon
                                size="lg"
                                variant={isReordering ? "filled" : "outline"}
                                onClick={() => setIsReordering((prev) => !prev)}
                                color={theme.primaryColor}
                            >
                                <RiListOrdered2 size={18} />
                            </ActionIcon>
                        )}
                        <Menu shadow="md" position="bottom-start">
                            <Menu.Target>
                                <Button
                                    variant="outline"
                                    leftSection={<FiPlus size={18} />}
                                >
                                    Create
                                </Button>
                            </Menu.Target>
                            <Menu.Dropdown>
                                <Menu.Item
                                    leftSection={<FiZap size={16} />}
                                    onClick={() =>
                                        setOpenDialogType(OpenDialogType.Wizard)
                                    }
                                >
                                    Guided Setup
                                </Menu.Item>
                                <Menu.Divider />
                                <Menu.Item
                                    leftSection={<FiPlus size={16} />}
                                    onClick={() =>
                                        setOpenDialogType(OpenDialogType.CreateTracker)
                                    }
                                >
                                    Create New
                                </Menu.Item>
                                <Menu.Item
                                    leftSection={<CiSquarePlus size={16} />}
                                    onClick={() =>
                                        setOpenDialogType(OpenDialogType.CreateFromTemplate)
                                    }
                                >
                                    Create From Template
                                </Menu.Item>
                            </Menu.Dropdown>
                        </Menu>
                    </Group>
                </Group>

                <ScrollArea flex={1}>
                    {!isLoading && trackerList.length === 0 ? (
                        selectedFilter === TrackerFilters.Collaborating ? (
                            <Stack align="center" gap="md" py={80}>
                                <Text fw={700} size="xl">No shared trackers</Text>
                                <Text size="sm" c="dimmed">
                                    Trackers shared with you will appear here.
                                </Text>
                            </Stack>
                        ) : (
                            <Stack align="center" gap="md" py={80}>
                                <ThemeIcon
                                    size={72}
                                    radius="xl"
                                    variant="light"
                                    color={theme.primaryColor}
                                >
                                    <TbLayoutGrid size={36} />
                                </ThemeIcon>
                                <Stack align="center" gap={4}>
                                    <Text fw={700} size="xl">
                                        No trackers yet
                                    </Text>
                                    <Text size="sm" c="dimmed">
                                        Create your first tracker to start logging data
                                    </Text>
                                </Stack>
                                <Button
                                    leftSection={<FiZap size={16} />}
                                    onClick={() =>
                                        setOpenDialogType(OpenDialogType.Wizard)
                                    }
                                >
                                    Get Started
                                </Button>
                            </Stack>
                        )
                    ) : (
                        <DndContext
                            sensors={sensors}
                            collisionDetection={closestCenter}
                            onDragEnd={handleDragEnd}
                            modifiers={[restrictToParentElement]}
                        >
                            <SortableContext
                                items={trackerList.map((t) => t.id)}
                                strategy={verticalListSortingStrategy}
                            >
                                <Stack>
                                    {trackerList.map((x) => {
                                        const color =
                                            x.color && x.color in theme.colors
                                                ? x.color
                                                : "indigo";
                                        return (
                                            <SortableTrackerCard
                                                key={x.id}
                                                tracker={x}
                                                color={color}
                                                isReordering={isReordering}
                                                isTemplates={isTemplates}
                                                onNavigate={(t) => navigate(`/trackers/${t.id}`)}
                                                onQuickAdd={(t) => {
                                                    setSelectedTracker(t);
                                                    setOpenDialogType(OpenDialogType.QuickAddEntry);
                                                }}
                                                onEdit={(t) => {
                                                    setSelectedTracker(t);
                                                    setOpenDialogType(OpenDialogType.UpdateTracker);
                                                }}
                                                onDelete={(t) => {
                                                    setSelectedTracker(t);
                                                    setOpenDialogType(OpenDialogType.DeleteTracker);
                                                }}
                                            />
                                        );
                                    })}
                                </Stack>
                            </SortableContext>
                        </DndContext>
                    )}
                </ScrollArea>
            </Stack>

            {openDialogType === OpenDialogType.Wizard && (
                <TrackerWizard
                    onClose={() => {
                        setOpenDialogType(undefined);
                        GetData();
                    }}
                />
            )}
            {openDialogType === OpenDialogType.CreateTracker && (
                <TrackerFormDialog
                    asTemplate={isTemplates}
                    onConfirm={() => GetData()}
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}
            {openDialogType === OpenDialogType.CreateFromTemplate && (
                <TrackerFormDialog
                    asTemplate={isTemplates}
                    withTemplate
                    onConfirm={() => GetData()}
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}
            {openDialogType === OpenDialogType.UpdateTracker &&
                selectedTracker && (
                    <TrackerFormDialog
                        asTemplate={isTemplates}
                        onConfirm={() => GetData()}
                        onClose={() => setOpenDialogType(undefined)}
                        initialValues={selectedTracker}
                        trackerId={selectedTracker.id}
                    />
                )}
            {openDialogType === OpenDialogType.QuickAddEntry &&
                selectedTracker && (
                    <QuickAddEntryDialog
                        tracker={selectedTracker}
                        onClose={() => {
                            setSelectedTracker(undefined);
                            setOpenDialogType(undefined);
                        }}
                    />
                )}
            {openDialogType === OpenDialogType.DeleteTracker &&
                selectedTracker && (
                    <ConfirmationDialog
                        isOpen
                        onClose={() => {
                            setSelectedTracker(undefined);
                            setOpenDialogType(undefined);
                        }}
                        onConfirm={async () => {
                            await trackersController.deleteTracker(
                                selectedTracker.id
                            );
                            GetData();
                            setSelectedTracker(undefined);
                            setOpenDialogType(undefined);
                        }}
                        message={
                            isTemplates
                                ? "Are you sure you want to delete the template?"
                                : "Are you sure you want to delete the tracker?"
                        }
                        severity="important"
                    />
                )}
        </>
    );
});

export default Trackers;
