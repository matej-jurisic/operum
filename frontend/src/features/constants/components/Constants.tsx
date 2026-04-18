import {
    ActionIcon,
    Badge,
    Button,
    Card,
    Group,
    Paper,
    ScrollArea,
    Stack,
    Text,
    Title,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete, MdEdit } from "react-icons/md";
import { RiFileListFill } from "react-icons/ri";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { useConstants } from "../context/ConstantsContext";
import { TrackerConstantDto } from "../types/TrackerConstantDto";
import ConstantDetailsDialog from "./ConstantDetailsDialog";
import { ConstantFormDialog } from "./ConstantFormDialog";

interface ConstantsProps {
    tracker: TrackerDto;
}

enum OpenDialogType {
    View,
    Create,
    Edit,
    Delete,
}

export default function Constants(props: ConstantsProps) {
    const { constants, refreshConstantsIfDirty, _createConstant, _updateConstant, _deleteConstant } =
        useConstants();
    const [selectedConstant, setSelectedConstant] = useState<TrackerConstantDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    useEffect(() => {
        refreshConstantsIfDirty();
    }, []);

    return (
        <>
            <Stack gap="md" h="100%">
                <Group justify="space-between" w="100%">
                    <Button
                        color={props.tracker.color}
                        variant="outline"
                        onClick={() => setOpenDialogType(OpenDialogType.Create)}
                        leftSection={<FiPlus size={18} />}
                    >
                        Create
                    </Button>
                </Group>

                <ScrollArea flex={1} mih={0}>
                    {constants.length > 0 ? (
                        <Stack gap="md">
                            {constants.map((constant) => (
                                <Card key={constant.id} p="md" radius="md" withBorder>
                                    <Group justify="space-between" wrap="nowrap">
                                        <Stack gap="xs" flex={1}>
                                            <Title order={4} lineClamp={1}>
                                                {constant.name}
                                            </Title>
                                            <Group wrap="wrap">
                                                <Badge variant="light" color="blue" size="sm">
                                                    {constant.type}
                                                </Badge>
                                                <Text size="sm" c="dimmed">
                                                    {constant.value}
                                                </Text>
                                                {constant.values?.length > 0 && (
                                                    <Badge variant="light" color="grape" size="sm">
                                                        {constant.values.length} conditional
                                                    </Badge>
                                                )}
                                            </Group>
                                        </Stack>
                                        <Group gap="xs" wrap="nowrap">
                                            <ActionIcon
                                                variant="outline"
                                                color={props.tracker.color}
                                                size="lg"
                                                onClick={() => {
                                                    setSelectedConstant(constant);
                                                    setOpenDialogType(OpenDialogType.View);
                                                }}
                                            >
                                                <RiFileListFill size={16} />
                                            </ActionIcon>
                                            <ActionIcon
                                                variant="outline"
                                                color="green"
                                                size="lg"
                                                onClick={() => {
                                                    setSelectedConstant(constant);
                                                    setOpenDialogType(OpenDialogType.Edit);
                                                }}
                                            >
                                                <MdEdit size={16} />
                                            </ActionIcon>
                                            <ActionIcon
                                                variant="outline"
                                                color="red"
                                                size="lg"
                                                onClick={() => {
                                                    setSelectedConstant(constant);
                                                    setOpenDialogType(OpenDialogType.Delete);
                                                }}
                                            >
                                                <MdDelete size={16} />
                                            </ActionIcon>
                                        </Group>
                                    </Group>
                                </Card>
                            ))}
                        </Stack>
                    ) : (
                        <Paper withBorder p="xl" radius="md">
                            <Stack gap="md" align="center">
                                <Text size="lg" fw={500} c="dimmed">
                                    No Constants
                                </Text>
                                <Text ta="center" c="dimmed">
                                    Create constants to use in calculated field formulas.
                                </Text>
                            </Stack>
                        </Paper>
                    )}
                </ScrollArea>
            </Stack>

            {openDialogType === OpenDialogType.View && selectedConstant && (
                <ConstantDetailsDialog
                    constant={selectedConstant}
                    tracker={props.tracker}
                    onClose={() => {
                        setOpenDialogType(undefined);
                        setSelectedConstant(undefined);
                    }}
                />
            )}

            {openDialogType === OpenDialogType.Create && (
                <ConstantFormDialog
                    tracker={props.tracker}
                    onClose={() => setOpenDialogType(undefined)}
                    onCreate={_createConstant}
                    onUpdate={_updateConstant}
                />
            )}

            {openDialogType === OpenDialogType.Edit && selectedConstant && (
                <ConstantFormDialog
                    tracker={props.tracker}
                    constantId={selectedConstant.id}
                    initialValues={selectedConstant}
                    onClose={() => {
                        setOpenDialogType(undefined);
                        setSelectedConstant(undefined);
                    }}
                    onCreate={_createConstant}
                    onUpdate={_updateConstant}
                />
            )}

            {openDialogType === OpenDialogType.Delete && selectedConstant && (
                <ConfirmationDialog
                    isOpen
                    onClose={() => {
                        setSelectedConstant(undefined);
                        setOpenDialogType(undefined);
                    }}
                    onConfirm={async () => {
                        await _deleteConstant(selectedConstant.id);
                        setSelectedConstant(undefined);
                        setOpenDialogType(undefined);
                    }}
                    severity="important"
                    message="Deleting this constant will break any formulas that reference it."
                />
            )}
        </>
    );
}
