import {
    Alert,
    Button,
    Group,
    Modal,
    Select,
    Stack,
    Text,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useState } from "react";
import { FiAlertTriangle } from "react-icons/fi";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { ExtractFieldsResultDto } from "../types/ExtractFieldsDto";
import { FieldDto } from "../types/FieldDto";

interface ExtractFieldsDialogProps {
    tracker: TrackerDto;
    fields: FieldDto[];
    onClose: () => void;
    onExtracted: (result: ExtractFieldsResultDto) => void;
}

export function ExtractFieldsDialog(props: ExtractFieldsDialogProps) {
    const { extractFields } = useTrackerOperations();
    const [submitting, setSubmitting] = useState(false);

    const form = useForm({
        initialValues: {
            newTrackerName: "",
            referenceFieldName: "",
            displayFieldId: props.fields[0]?.id ?? "",
        },
        validate: {
            newTrackerName: (value) =>
                value.trim().length === 0
                    ? "Tracker name is required"
                    : value.length > 100
                    ? "Tracker name must be at most 100 characters"
                    : null,
            referenceFieldName: (value) =>
                value.trim().length === 0
                    ? "Field name is required"
                    : value.length > 30
                    ? "Field name must be at most 30 characters"
                    : null,
        },
    });

    const handleSubmit = async (values: typeof form.values) => {
        setSubmitting(true);
        try {
            const result = await extractFields({
                fieldIds: props.fields.map((f) => f.id),
                newTrackerName: values.newTrackerName.trim(),
                referenceFieldName: values.referenceFieldName.trim(),
                displayFieldId: values.displayFieldId || undefined,
            });
            props.onExtracted(result);
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <Modal opened onClose={props.onClose} title="Extract to new tracker" centered>
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack>
                    <Text size="sm" c="dimmed">
                        Extracting: {props.fields.map((f) => f.name).join(", ")}
                    </Text>

                    <TextInput
                        label="New tracker name"
                        placeholder="Vendors"
                        maxLength={100}
                        {...form.getInputProps("newTrackerName")}
                    />

                    <TextInput
                        label="Reference field name"
                        placeholder="Vendor"
                        maxLength={30}
                        {...form.getInputProps("referenceFieldName")}
                    />

                    <Select
                        label="Label field"
                        description="Shown as the link text on each row."
                        clearable
                        data={props.fields.map((f) => ({
                            value: f.id,
                            label: f.name,
                        }))}
                        {...form.getInputProps("displayFieldId")}
                    />

                    <Alert
                        color="yellow"
                        icon={<FiAlertTriangle size={18} />}
                        variant="light"
                    >
                        These fields move to the new tracker. Rows here link to
                        it instead. Views and widgets that use these fields lose
                        them.
                    </Alert>

                    <Group justify="flex-end">
                        <Button variant="default" onClick={props.onClose}>
                            Cancel
                        </Button>
                        <Button
                            color={props.tracker.color}
                            type="submit"
                            loading={submitting}
                        >
                            Extract
                        </Button>
                    </Group>
                </Stack>
            </form>
        </Modal>
    );
}
