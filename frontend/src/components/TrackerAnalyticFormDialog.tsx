import { Button, Group, Modal, Select, Stack } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useEffect, useMemo, useState } from "react";
import api from "../api/api";
import { useTracker } from "../context/TrackerContext";
import { AnalyticDto } from "../model/AnalyticDto";
import { AddTrackerAnalyticDto } from "../model/requests/AddTrackerAnalyticDto";

const GetPublicAnalytics = async () => {
    const response = await api.get("/analytics");
    return response.data.data;
};

interface Props {
    onClose: () => void;
}

export default function TrackerAnalyticFormDialog({ onClose }: Props) {
    const { tracker, fields } = useTracker();
    const [analytics, setAnalytics] = useState<AnalyticDto[]>([]);
    const [selectedAnalyticId, setSelectedAnalyticId] = useState<string>();
    const [loading, setLoading] = useState(false);

    const form = useForm<AddTrackerAnalyticDto>({
        initialValues: {
            analyticId: "",
            trackerAnalyticFields: [],
        },
        validate: {
            analyticId: (value) =>
                !value ? "Please select an analytic" : null,
            trackerAnalyticFields: (value) =>
                value.length === 0 ? "Please map all required fields" : null,
        },
    });

    const fetchAnalytics = async () => {
        const data = await GetPublicAnalytics();
        setAnalytics(data);
    };

    useEffect(() => {
        fetchAnalytics();
    }, []);

    const analyticsForSelect = useMemo(() => {
        return analytics.map((analytic) => ({
            value: analytic.id,
            label: analytic.name,
        }));
    }, [analytics]);

    const fieldsForSelect = useMemo(() => {
        return fields.map((field) => ({
            value: field.id,
            label: field.name,
            type: field.type,
        }));
    }, [fields]);

    const selectedAnalytic = useMemo(() => {
        return analytics.find((a) => a.id === selectedAnalyticId);
    }, [selectedAnalyticId, analytics]);

    const handleAnalyticChange = (analyticId: string | null) => {
        setSelectedAnalyticId(analyticId ?? undefined);
        form.setFieldValue("analyticId", analyticId ?? "");
        form.setFieldValue("trackerAnalyticFields", []);
    };

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
        if (!selectedAnalytic) return;

        // Validate all required fields are mapped
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

        setLoading(true);
        try {
            await api.post(`/trackers/${tracker.id}/analytics`, values);
            onClose();
        } catch (error) {
            console.error("Failed to add analytic:", error);
            // Handle error (show notification, etc.)
        } finally {
            setLoading(false);
        }
    };

    return (
        <Modal
            opened
            onClose={onClose}
            title="Add Analytic to Tracker"
            centered
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack>
                    <Select
                        label="Analytic"
                        data={analyticsForSelect}
                        placeholder="Select an analytic"
                        value={selectedAnalyticId}
                        onChange={handleAnalyticChange}
                        error={form.errors.analyticId}
                        required
                    />
                    {selectedAnalytic && (
                        <>
                            {selectedAnalytic.analyticRequiredDataTypes.map(
                                (type) => {
                                    const currentValue =
                                        form.values.trackerAnalyticFields.find(
                                            (f) =>
                                                f.analyticRequiredDataTypeId ===
                                                type.id
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
                                                handleFieldChange(
                                                    type.id,
                                                    value
                                                )
                                            }
                                            required
                                        />
                                    );
                                }
                            )}
                        </>
                    )}
                    <Group justify="flex-end" mt="md">
                        <Button variant="subtle" onClick={onClose}>
                            Cancel
                        </Button>
                        <Button type="submit" loading={loading}>
                            Add Analytic
                        </Button>
                    </Group>
                </Stack>
            </form>
        </Modal>
    );
}
