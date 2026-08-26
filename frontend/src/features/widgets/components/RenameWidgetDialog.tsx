import { Button, Modal, Stack, Textarea, TextInput } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useState } from "react";
import { useWidgets } from "../context/WidgetsContext";
import { WidgetDto } from "../types/WidgetDto";

interface Props {
    widget: WidgetDto;
    onClose: () => void;
}

interface FormValues {
    name: string;
    description: string;
}

/** Only the widget's name and description are editable after creation -- the definition
    (result type, code, sources, field mapping) is fixed. See CreateWidgetForm to build a
    different chart instead. */
export default function RenameWidgetDialog({ widget, onClose }: Props) {
    const { updateWidget } = useWidgets();
    const [isSubmitting, setIsSubmitting] = useState(false);

    const form = useForm<FormValues>({
        initialValues: {
            name: widget.name,
            description: widget.description ?? "",
        },
        validate: {
            name: (value) => (value.length > 100 ? "Name cannot exceed 100 characters" : null),
            description: (value) =>
                value.length > 500 ? "Description cannot exceed 500 characters" : null,
        },
    });

    const handleSubmit = async (values: FormValues) => {
        setIsSubmitting(true);
        try {
            await updateWidget(widget.id, {
                name: values.name.trim() || undefined,
                description: values.description.trim() || undefined,
            });
        } finally {
            setIsSubmitting(false);
        }
        onClose();
    };

    return (
        <Modal opened onClose={onClose} title="Edit widget" centered>
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack gap="md">
                    <TextInput
                        label="Name"
                        description="Left blank, the widget falls back to its calculation's default label"
                        maxLength={100}
                        autoFocus
                        {...form.getInputProps("name")}
                    />
                    <Textarea
                        label="Description"
                        maxLength={500}
                        autosize
                        minRows={2}
                        {...form.getInputProps("description")}
                    />

                    <Button type="submit" mt="xs" loading={isSubmitting}>
                        Save
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
