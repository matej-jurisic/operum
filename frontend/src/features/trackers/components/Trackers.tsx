import {
    ActionIcon,
    Badge,
    Button,
    Card,
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
import { FiMoreVertical, FiPlus, FiPlusSquare, FiZap } from "react-icons/fi";
import { MdDelete, MdEdit } from "react-icons/md";
import { TbLayoutGrid } from "react-icons/tb";
import { createElement } from "react";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import { useNavigate } from "react-router-dom";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import Header from "../../../shared/components/Header";
import {
    TrackerFilters,
    TrackerFiltersForSelect,
} from "../../../shared/constants/TrackerFilters";
import globalStore from "../../../shared/stores/GlobalStore";
import { trackersController } from "../api/trackersController";
import { TrackerDto } from "../types/TrackerDto";
import QuickAddEntryDialog from "../../entries/components/QuickAddEntryDialog";
import TrackerFormDialog from "./TrackerFormDialog";
import TrackerWizard from "./TrackerWizard";

enum OpenDialogType {
    CreateTracker,
    DeleteTracker,
    UpdateTracker,
    CreateFromTemplate,
    Wizard,
    QuickAddEntry,
}

const hasInputtableFields = (t: TrackerDto) => t.fields.some((f) => !f.isCalculated);

interface Props {
    isTemplates?: boolean;
}

export default function Trackers({ isTemplates = false }: Props) {
    const [trackerList, setTrackerList] = useState<TrackerDto[]>([]);
    const [selectedTracker, setSelectedTracker] = useState<TrackerDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const [selectedFilter, setSelectedFilter] = useState<string>(
        TrackerFilters.Owned
    );
    const [isLoading, setIsLoading] = useState(true);
    const theme = useMantineTheme();
    const navigate = useNavigate();

    const GetData = async () => {
        setIsLoading(true);
        let response;
        if (isTemplates) {
            response = await trackersController.getAdminTemplateList();
        } else {
            response = await trackersController.getTrackerList();
        }
        setTrackerList(response.data);
        setIsLoading(false);
    };

    useEffect(() => {
        GetData();
    }, []);

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
                                        setIsLoading(true);
                                        const response =
                                            await trackersController.getTrackerList(e);
                                        setTrackerList(response.data);
                                        setIsLoading(false);
                                    }
                                }}
                            />
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
                                    leftSection={<FiPlusSquare size={16} />}
                                    onClick={() =>
                                        setOpenDialogType(OpenDialogType.CreateFromTemplate)
                                    }
                                >
                                    Create From Template
                                </Menu.Item>
                            </Menu.Dropdown>
                        </Menu>
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
                            <Stack>
                                {trackerList.map((x) => {
                                    const color =
                                        x.color && x.color in theme.colors
                                            ? x.color
                                            : "indigo";

                                    return (
                                        <Card
                                            key={x.id}
                                            shadow="sm"
                                            padding="lg"
                                            radius="md"
                                            withBorder
                                            style={{
                                                borderTop: `3px solid var(--mantine-color-${color}-5)`,
                                                cursor: "pointer",
                                            }}
                                            onClick={() =>
                                                navigate(`/trackers/${x.id}`)
                                            }
                                        >
                                            <Group
                                                gap="md"
                                                align="center"
                                                wrap="nowrap"
                                            >
                                                <ThemeIcon
                                                    size={44}
                                                    radius="md"
                                                    variant="light"
                                                    color={color}
                                                    style={{ flexShrink: 0 }}
                                                >
                                                    {createElement(resolveTrackerIcon(x.icon), { size: 22 })}
                                                </ThemeIcon>
                                                <Stack
                                                    gap={4}
                                                    flex={1}
                                                    style={{ minWidth: 0 }}
                                                >
                                                    <Group
                                                        justify="space-between"
                                                        align="center"
                                                        wrap="nowrap"
                                                        gap="xs"
                                                    >
                                                        <Group
                                                            gap="xs"
                                                            align="center"
                                                            wrap="nowrap"
                                                            style={{ minWidth: 0 }}
                                                        >
                                                            <Title
                                                                order={4}
                                                                lineClamp={1}
                                                                className="wrapped-text"
                                                                style={{ minWidth: 0 }}
                                                            >
                                                                {x.name}
                                                            </Title>
                                                            {x.fields.length > 0 && (
                                                                <Badge
                                                                    size="sm"
                                                                    variant="light"
                                                                    color={color}
                                                                    style={{ flexShrink: 0 }}
                                                                >
                                                                    {x.fields.length}{" "}
                                                                    {x.fields.length === 1
                                                                        ? "field"
                                                                        : "fields"}
                                                                </Badge>
                                                            )}
                                                        </Group>
                                                        <Group
                                                            gap="xs"
                                                            wrap="nowrap"
                                                            style={{ flexShrink: 0 }}
                                                        >
                                                            {globalStore.currentUser?.id === x.ownerId ? (
                                                                <>
                                                                    {!isTemplates && hasInputtableFields(x) && (
                                                                        <ActionIcon
                                                                            size="lg"
                                                                            variant="outline"
                                                                            color={color}
                                                                            onClick={(e) => {
                                                                                e.stopPropagation();
                                                                                setSelectedTracker(x);
                                                                                setOpenDialogType(OpenDialogType.QuickAddEntry);
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
                                                                                    setSelectedTracker(x);
                                                                                    setOpenDialogType(OpenDialogType.UpdateTracker);
                                                                                }}
                                                                            >
                                                                                Edit
                                                                            </Menu.Item>
                                                                            <Menu.Item
                                                                                color="red"
                                                                                leftSection={<MdDelete size={16} />}
                                                                                onClick={(e) => {
                                                                                    e.stopPropagation();
                                                                                    setSelectedTracker(x);
                                                                                    setOpenDialogType(OpenDialogType.DeleteTracker);
                                                                                }}
                                                                            >
                                                                                Delete
                                                                            </Menu.Item>
                                                                        </Menu.Dropdown>
                                                                    </Menu>
                                                                </>
                                                            ) : x.currentUserCanEditData && hasInputtableFields(x) && !isTemplates ? (
                                                                <>
                                                                    <ActionIcon
                                                                        size="lg"
                                                                        variant="outline"
                                                                        color={color}
                                                                        onClick={(e) => {
                                                                            e.stopPropagation();
                                                                            setSelectedTracker(x);
                                                                            setOpenDialogType(OpenDialogType.QuickAddEntry);
                                                                        }}
                                                                    >
                                                                        <FiPlus size={18} />
                                                                    </ActionIcon>
                                                                    <Badge variant="outline">
                                                                        Owned by: {x.ownerName}
                                                                    </Badge>
                                                                </>
                                                            ) : (
                                                                <Badge variant="outline">
                                                                    Owned by: {x.ownerName}
                                                                </Badge>
                                                            )}
                                                        </Group>
                                                    </Group>
                                                    <Text
                                                        c="dimmed"
                                                        size="sm"
                                                        className="wrapped-text"
                                                        lineClamp={2}
                                                    >
                                                        {x.description ||
                                                            "No description"}
                                                    </Text>
                                                </Stack>
                                            </Group>
                                        </Card>
                                    );
                                })}
                            </Stack>
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
}
