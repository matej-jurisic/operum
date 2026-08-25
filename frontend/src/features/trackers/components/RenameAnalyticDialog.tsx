import { Button, Modal, Stack, TextInput } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useState } from "react";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { AnalyticDto } from "../../analytics/types/AnalyticDto";
import { useTracker } from "../context/TrackerContext";

interface Props {
    analytic: AnalyticDto;
    onClose: () => void;
}

interface FormValues {
    name: string;
}

export default function RenameAnalyticDialog({ analytic, onClose }: Props) {
    const { tracker } = useTracker();
    const { updateAnalytic } = useTrackerOperations();
    const [isSubmitting, setIsSubmitting] = useState(false);

    const form = useForm<FormValues>({
        initialValues: {
            name: analytic.name,
        },
        validate: {
            name: (value) =>
                value.length > 100
                    ? "Name cannot exceed 100 characters"
                    : null,
        },
    });

    const handleSubmit = async (values: FormValues) => {
        setIsSubmitting(true);
        await updateAnalytic(analytic.id, {
            name: values.name.trim() || undefined,
        });
        setIsSubmitting(false);
        onClose();
    };

    return (
        <Modal opened onClose={onClose} title="Rename Analytic" centered>
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack gap="md">
                    <TextInput
                        label="Name"
                        description="Left blank, the card falls back to the calculation's default label"
                        maxLength={100}
                        autoFocus
                        {...form.getInputProps("name")}
                    />

                    <Button
                        color={tracker.color}
                        type="submit"
                        mt="xs"
                        loading={isSubmitting}
                    >
                        Save
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
