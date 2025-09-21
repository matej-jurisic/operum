import {
    ActionIcon,
    Button,
    Card,
    Group,
    Menu,
    ScrollArea,
    Stack,
    Text,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { FiPlus, FiPlusSquare } from "react-icons/fi";
import { MdDelete, MdEdit } from "react-icons/md";
import { useNavigate } from "react-router-dom";
import api from "../api/api";
import { TrackerDto } from "../model/TrackerDto";
import ConfirmationDialog from "./ConfirmationDialog";
import Header from "./Header";
import TrackerFormDialog from "./TrackerFormDialog";

const GetAdminTemplateList = async () => {
    const response = await api.get("/trackers/admin-templates");
    return response.data.data;
};

const GetTrackerList = async () => {
    const response = await api.get("/trackers");
    return response.data.data;
};

const DeleteTracker = async (trackerId: string) => {
    await api.delete(`/trackers/${trackerId}`);
};

enum OpenDialogType {
    CreateTracker,
    DeleteTracker,
    UpdateTracker,
    CreateFromTemplate,
}

interface Props {
    isTemplates?: boolean;
}

export default function TrackerList({ isTemplates = false }: Props) {
    const [trackerList, setTrackerList] = useState<TrackerDto[]>([]);
    const [selectedTracker, setSelectedTracker] = useState<TrackerDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();
    const theme = useMantineTheme();
    const navigate = useNavigate();

    const GetData = async () => {
        if (isTemplates) {
            setTrackerList(await GetAdminTemplateList());
        } else {
            setTrackerList(await GetTrackerList());
        }
    };

    useEffect(() => {
        GetData();
    }, []);

    return (
        <>
            <Stack gap="md" h={"100%"}>
                <Group w={"100%"} justify="space-between">
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
                    {!isTemplates && <Header />}
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
                                            <ActionIcon
                                                size={"lg"}
                                                variant="outline"
                                                color="green"
                                                onClick={(e) => {
                                                    e.stopPropagation();
                                                    setSelectedTracker(x);
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
                                                    setSelectedTracker(x);
                                                    setOpenDialogType(
                                                        OpenDialogType.DeleteTracker
                                                    );
                                                }}
                                            >
                                                <MdDelete size={18} />
                                            </ActionIcon>
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
                        initialValues={selectedTracker}
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
                            await DeleteTracker(selectedTracker.id);
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
