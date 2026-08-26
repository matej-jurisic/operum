import { Button, Modal, Stack, TextInput } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useState } from "react";
import { useWidgets } from "../context/WidgetsContext";
import { EntriesWidgetDefinitionDto } from "../types/WidgetDto";

interface Props {
    entriesWidget: EntriesWidgetDefinitionDto;
    onClose: () => void;
}

interface FormValues {
    name: string;
}

/** The tracker an Entries widget reads from is fixed at creation -- only its name is
    editable afterwards. */
export default function RenameEntriesWidgetDialog({ entriesWidget, onClose }: Props) {
    const { updateEntriesWidget } = useWidgets();
    const [isSubmitting, setIsSubmitting] = useState(false);

    const form = useForm<FormValues>({
        initialValues: { name: entriesWidget.name },
        validate: {
            name: (value) => (value.length > 100 ? "Name cannot exceed 100 characters" : null),
        },
    });

    const handleSubmit = async (values: FormValues) => {
        setIsSubmitting(true);
        try {
            await updateEntriesWidget(entriesWidget.id, { name: values.name.trim() || undefined });
        } finally {
            setIsSubmitting(false);
        }
        onClose();
    };

    return (
        <Modal opened onClose={onClose} title="Edit entries table" centered>
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack gap="md">
                    <TextInput
                        label="Name"
                        description={`Left blank, the table falls back to "${entriesWidget.trackerName}"`}
                        maxLength={100}
                        autoFocus
                        {...form.getInputProps("name")}
                    />

                    <Button type="submit" mt="xs" loading={isSubmitting}>
                        Save
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
