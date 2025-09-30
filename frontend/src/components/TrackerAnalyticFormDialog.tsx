import { Button, Group, Modal, Select, Stack, Text } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useMemo } from "react";
import { useTracker } from "../context/TrackerContext";
import { AnalyticDto } from "../model/AnalyticDto";
import { AddTrackerAnalyticDto } from "../model/requests/AddTrackerAnalyticDto";

interface Props {
    onClose: () => void;
    selectedAnalytic: AnalyticDto;
}

export default function TrackerAnalyticFormDialog({
    onClose,
    selectedAnalytic,
}: Props) {
    const { tracker, fields, AddAnalyticToTracker } = useTracker();

    const form = useForm<AddTrackerAnalyticDto>({
        initialValues: {
            analyticId: selectedAnalytic.id,
            trackerAnalyticFields: [],
        },
        validate: {
            trackerAnalyticFields: (value) =>
                value.length !==
                selectedAnalytic.analyticRequiredDataTypes.length
                    ? "Please map all required fields"
                    : null,
        },
    });

    const fieldsForSelect = useMemo(
        () => fields.map((f) => ({ value: f.id, label: f.name, type: f.type })),
        [fields]
    );

    const handleFieldChange = (
        analyticRequiredDataTypeId: string,
        fieldId: string | null
    ) => {
        const currentFields = form.values.trackerAnalyticFields.filter(
            (f) => f.analyticRequiredDataTypeId !== analyticRequiredDataTypeId
        );
        if (fieldId) {
            form.setFieldValue("trackerAnalyticFields", [
                ...currentFields,
                { fieldId, analyticRequiredDataTypeId },
            ]);
        } else {
            form.setFieldValue("trackerAnalyticFields", currentFields);
        }
    };

    const handleSubmit = async (values: AddTrackerAnalyticDto) => {
        if (
            values.trackerAnalyticFields.length !==
            selectedAnalytic.analyticRequiredDataTypes.length
        ) {
            form.setFieldError(
                "trackerAnalyticFields",
                "Please map all required fields"
            );
            return;
        }
        await AddAnalyticToTracker(values);
        onClose();
    };

    return (
        <Modal
            opened
            onClose={onClose}
            title="Map Analytic Fields"
            centered
            size={"lg"}
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack justify="stretch">
                    <Group justify="flex-start" align="center">
                        <Text c="dimmed">Selected:</Text>
                        <Text>
                            <strong>{selectedAnalytic.name}</strong>
                        </Text>
                    </Group>

                    {selectedAnalytic.analyticRequiredDataTypes.map((type) => {
                        const currentValue =
                            form.values.trackerAnalyticFields.find(
                                (f) => f.analyticRequiredDataTypeId === type.id
                            )?.fieldId;

                        return (
                            <Select
                                label={type.purpose}
                                key={type.id}
                                data={fieldsForSelect.filter(
                                    (x) => x.type === type.type
                                )}
                                placeholder={`Select ${type.type}`}
                                value={currentValue}
                                onChange={(value) =>
                                    handleFieldChange(type.id, value)
                                }
                                required
                            />
                        );
                    })}

                    <Button color={tracker.color} type="submit">
                        Add Analytic
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
