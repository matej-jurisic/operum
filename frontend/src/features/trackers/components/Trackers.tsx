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
    Title,
    useMantineTheme,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { FiPlus, FiPlusSquare } from "react-icons/fi";
import { MdDelete, MdEdit } from "react-icons/md";
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
import TrackerFormDialog from "./TrackerFormDialog";

enum OpenDialogType {
    CreateTracker,
    DeleteTracker,
    UpdateTracker,
    CreateFromTemplate,
}

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
    const theme = useMantineTheme();
    const navigate = useNavigate();

    const GetData = async () => {
        let response;
        if (isTemplates) {
            response = await trackersController.getAdminTemplateList();
        } else {
            response = await trackersController.getTrackerList();
        }
        setTrackerList(response.data);
    };

    useEffect(() => {
        GetData();
    }, []);

    return (
        <>
            <Stack gap="md" h={"100%"}>
                {!isTemplates && (
                    <Group w={"100%"} justify="space-between">
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
                            label={"Filter"}
                            data={TrackerFiltersForSelect}
                            allowDeselect={false}
                            onChange={async (e) => {
                                if (e) {
                                    setSelectedFilter(e);
                                    const response =
                                        await trackersController.getTrackerList(
                                            e
                                        );
                                    setTrackerList(response.data);
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
                                leftSection={<FiPlus size={16} />}
                                onClick={() => {
                                    setOpenDialogType(
                                        OpenDialogType.CreateTracker
                                    );
                                }}
                            >
                                Create New
                            </Menu.Item>
                            <Menu.Item
                                leftSection={<FiPlusSquare size={16} />}
                                onClick={() =>
                                    setOpenDialogType(
                                        OpenDialogType.CreateFromTemplate
                                    )
                                }
                            >
                                Create From Template
                            </Menu.Item>
                        </Menu.Dropdown>
                    </Menu>
                </Group>
                <ScrollArea flex={1}>
                    <Stack>
                        {trackerList.map((x) => {
                            const isValidColor =
                                x.color !== undefined &&
                                x.color in theme.colors;
                            const borderColor =
                                theme.colors[
                                    (isValidColor
                                        ? x.color
                                        : "indigo") as keyof typeof theme.colors
                                ][6];

                            return (
                                <Card
                                    key={x.id}
                                    shadow="sm"
                                    padding="lg"
                                    radius="md"
                                    withBorder
                                    style={{
                                        borderLeft: `6px solid ${borderColor}`,
                                        cursor: "pointer",
                                    }}
                                    onClick={() =>
                                        navigate(`/trackers/${x.id}`)
                                    }
                                >
                                    <Group
                                        justify="space-between"
                                        align="center"
                                        wrap="nowrap"
                                    >
                                        <Stack gap={"sm"}>
                                            <Title
                                                order={4}
                                                lineClamp={1}
                                                className="wrapped-text"
                                            >
                                                {x.name}
                                            </Title>
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
                                        <Group
                                            gap="xs"
                                            justify="flex-end"
                                            wrap="nowrap"
                                        >
                                            {globalStore.currentUser?.id ===
                                            x.ownerId ? (
                                                <>
                                                    <ActionIcon
                                                        size={"lg"}
                                                        variant="outline"
                                                        color="green"
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            setSelectedTracker(
                                                                x
                                                            );
                                                            setOpenDialogType(
                                                                OpenDialogType.UpdateTracker
                                                            );
                                                        }}
                                                    >
                                                        <MdEdit size={18} />
                                                    </ActionIcon>
                                                    <ActionIcon
                                                        size={"lg"}
                                                        variant="outline"
                                                        color="red"
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            setSelectedTracker(
                                                                x
                                                            );
                                                            setOpenDialogType(
                                                                OpenDialogType.DeleteTracker
                                                            );
                                                        }}
                                                    >
                                                        <MdDelete size={18} />
                                                    </ActionIcon>
                                                </>
                                            ) : (
                                                <Badge variant="outline">
                                                    Owned by: {x.ownerName}
                                                </Badge>
                                            )}
                                        </Group>
                                    </Group>
                                </Card>
                            );
                        })}
                    </Stack>
                </ScrollArea>
            </Stack>
            {openDialogType === OpenDialogType.CreateTracker && (
                <TrackerFormDialog
                    asTemplate={isTemplates}
                    onConfirm={() => GetData()}
                    onClose={() => {
                        setOpenDialogType(undefined);
                    }}
                />
            )}
            {openDialogType === OpenDialogType.CreateFromTemplate && (
                <TrackerFormDialog
                    asTemplate={isTemplates}
                    withTemplate
                    onConfirm={() => GetData()}
                    onClose={() => {
                        setOpenDialogType(undefined);
                    }}
                />
            )}
            {openDialogType === OpenDialogType.UpdateTracker &&
                selectedTracker && (
                    <TrackerFormDialog
                        asTemplate={isTemplates}
                        onConfirm={() => GetData()}
                        onClose={() => {
                            setOpenDialogType(undefined);
                        }}
                        // initialValues={selectedTracker}
                        trackerId={selectedTracker.id}
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
