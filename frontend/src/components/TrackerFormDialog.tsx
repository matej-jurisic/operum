import {
    Button,
    Group,
    Modal,
    Select,
    SelectProps,
    Stack,
    Text,
    TextInput,
    useMantineTheme,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { FaCheck, FaCircle } from "react-icons/fa";
import api from "../api/api";
import { TrackerUpsertDto } from "../model/requests/CreateTrackerDto";

export interface TrackerFormDialogProps {
    onClose: () => void;
    onConfirm?: () => void;
    trackerId?: string;
    initialValues?: TrackerUpsertDto;
}

const colorOptions = [
    { value: "indigo", label: "Indigo" },
    { value: "blue", label: "Blue" },
    { value: "cyan", label: "Cyan" },
    { value: "dark", label: "Dark" },
    { value: "grape", label: "Grape" },
    { value: "gray", label: "Gray" },
    { value: "green", label: "Green" },
    { value: "lime", label: "Lime" },
    { value: "orange", label: "Orange" },
    { value: "pink", label: "Pink" },
    { value: "red", label: "Red" },
    { value: "teal", label: "Teal" },
    { value: "yellow", label: "Yellow" },
    { value: "violet", label: "Violet" },
];

const renderColorOption = (
    theme: ReturnType<typeof useMantineTheme>
): SelectProps["renderOption"] => {
    return ({ option, checked }) => (
        <Group wrap="nowrap" gap="sm">
            <FaCircle
                size={16}
                color={theme.colors[option.value]?.[6] ?? option.value}
            />
            <Text>{option.label}</Text>
            {checked && <FaCheck color="gray" />}
        </Group>
    );
};

export default function TrackerFormDialog(props: TrackerFormDialogProps) {
    const theme = useMantineTheme();

    const form = useForm<TrackerUpsertDto>({
        initialValues: props.initialValues || {
            name: "",
            description: "",
            color: "indigo",
        },
        validate: {
            name: (value) =>
                value.trim().length === 0
                    ? "Field name is required"
                    : value.length > 30
                    ? "Name must be shorter than 50 characters"
                    : null,
            description: (value) =>
                value && value.length > 500
                    ? "Description must be at most 500 characters"
                    : null,
        },
    });

    const handleSubmit = async (values: TrackerUpsertDto) => {
        if (props.trackerId) {
            await api.put(`/trackers/${props.trackerId}`, values);
        } else {
            await api.post("/trackers", values);
        }
        props.onClose();
        props.onConfirm?.();
        form.reset();
    };

    return (
        <Modal opened onClose={props.onClose} title="Create Tracker" centered>
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack>
                    <TextInput
                        label="Tracker Name"
                        placeholder="Enter tracker name"
                        maxLength={30}
                        {...form.getInputProps("name")}
                    />
                    <TextInput
                        label="Description"
                        placeholder="Enter tracker description"
                        maxLength={500}
                        {...form.getInputProps("description")}
                    />
                    <Select
                        label="Color"
                        placeholder="Select tracker color"
                        data={colorOptions}
                        allowDeselect={false}
                        renderOption={renderColorOption(theme)}
                        {...form.getInputProps("color")}
                    />
                    <Button type="submit">
                        {props.trackerId ? "Update Tracker" : "Create Tracker"}
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
