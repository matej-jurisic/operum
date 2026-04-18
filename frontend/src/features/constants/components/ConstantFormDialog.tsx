import { Button, Modal, Select, Stack, TextInput } from "@mantine/core";
import { useForm } from "@mantine/form";
import { calculatedFieldTypes } from "../../../shared/constants/DataTypesForSelect";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { CreateTrackerConstantDto, TrackerConstantDto } from "../types/TrackerConstantDto";

interface ConstantFormDialogProps {
    tracker: TrackerDto;
    constantId?: string;
    initialValues?: TrackerConstantDto;
    onClose: () => void;
    onCreate: (values: CreateTrackerConstantDto) => Promise<void>;
    onUpdate: (constantId: string, values: CreateTrackerConstantDto) => Promise<void>;
}

function validateValueForType(value: string, type: string): string | null {
    if (!value.trim()) return "Value is required";
    if (type === "number" && isNaN(Number(value))) return "Value must be a valid number";
    if (type === "bool" && value !== "true" && value !== "false")
        return "Value must be 'true' or 'false'";
    if (type === "timespan") {
        const parts = value.split(":");
        if (
            parts.length < 2 ||
            parts.length > 3 ||
            parts.some((p) => isNaN(Number(p)))
        )
            return "Value must be a valid timespan (e.g. 01:30:00)";
    }
    return null;
}

export function ConstantFormDialog(props: ConstantFormDialogProps) {
    const form = useForm<CreateTrackerConstantDto>({
        initialValues: props.initialValues
            ? {
                  name: props.initialValues.name,
                  type: props.initialValues.type,
                  value: props.initialValues.value,
              }
            : { name: "", type: "number", value: "" },

        validate: {
            name: (value) =>
                !value.trim()
                    ? "Name is required"
                    : value.length > 30
                    ? "Name cannot exceed 30 characters"
                    : null,
            type: (value) => (!value ? "Type is required" : null),
            value: (value, values) => validateValueForType(value, values.type),
        },
    });

    const handleSubmit = async (values: CreateTrackerConstantDto) => {
        if (props.constantId) {
            await props.onUpdate(props.constantId, values);
        } else {
            await props.onCreate(values);
        }
        props.onClose();
        form.reset();
    };

    return (
        <Modal
            opened
            onClose={props.onClose}
            title={props.constantId ? "Edit Constant" : "Create Constant"}
            centered
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack align="stretch">
                    <TextInput
                        label="Name"
                        placeholder="e.g. TAX_RATE"
                        maxLength={30}
                        {...form.getInputProps("name")}
                    />

                    <Select
                        allowDeselect={false}
                        label="Type"
                        placeholder="Choose type"
                        data={calculatedFieldTypes}
                        required
                        {...form.getInputProps("type")}
                    />

                    <TextInput
                        label="Value"
                        placeholder={
                            form.values.type === "bool"
                                ? "true or false"
                                : form.values.type === "timespan"
                                ? "e.g. 01:30:00"
                                : "e.g. 3.14"
                        }
                        {...form.getInputProps("value")}
                    />

                    <Button color={props.tracker.color} type="submit">
                        {props.constantId ? "Update" : "Create"}
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
