import { Button, Modal, Select, Stack, Text } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useMemo } from "react";
import { useTracker } from "../context/TrackerContext";
import { CodeDto, ResultTypeDto } from "../model/AnalyticDto";
import { AddAnalyticDto } from "../model/requests/AddAnalyticDto";

interface Props {
    onClose: () => void;
    selectedAnalytic: {
        code: CodeDto;
        resultType: ResultTypeDto;
    };
}

interface FormValues {
    fieldMappings: Record<string, string>;
}

export default function TrackerAnalyticFormDialog({
    onClose,
    selectedAnalytic,
}: Props) {
    const { tracker, fields, AddAnalyticToTracker } = useTracker();
    const { code, resultType } = selectedAnalytic;

    const form = useForm<FormValues>({
        initialValues: {
            fieldMappings: {},
        },
        validate: {
            fieldMappings: (value) => {
                const mappedCount = Object.keys(value).filter(
                    (k) => value[k]
                ).length;
                return mappedCount !== code.purposes.length
                    ? "Please map all required fields"
                    : null;
            },
        },
    });

    const fieldsByType = useMemo(() => {
        const grouped: Record<
            string,
            Array<{ value: string; label: string }>
        > = {};
        fields.forEach((f) => {
            if (f.type) {
                if (!grouped[f.type]) {
                    grouped[f.type] = [];
                }
                grouped[f.type].push({ value: f.id, label: f.name });
            }
        });
        return grouped;
    }, [fields]);

    const handleSubmit = async (values: FormValues) => {
        const analyticFields = Object.entries(values.fieldMappings)
            .filter(([_, fieldId]) => fieldId)
            .map(([purpose, fieldId]) => ({
                fieldId,
                purpose,
            }));

        const dto: AddAnalyticDto = {
            code: code.name,
            resultType: resultType.name,
            analyticFields,
        };

        await AddAnalyticToTracker(dto);
        onClose();
    };

    return (
        <Modal
            opened
            onClose={onClose}
            title="Map Analytic Fields"
            centered
            size="lg"
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack gap="md">
                    <Text size="sm">
                        <Text span c="dimmed">
                            Analytic:{" "}
                        </Text>
                        <Text span fw={500}>
                            {code.name}
                        </Text>
                    </Text>

                    {code.purposes.map((purpose) => (
                        <Select
                            key={purpose.name}
                            label={purpose.name}
                            placeholder={`Select field (${purpose.allowedDataTypes.join(
                                ", "
                            )})`}
                            data={purpose.allowedDataTypes.flatMap(
                                (type) => fieldsByType[type] || []
                            )}
                            value={
                                form.values.fieldMappings[purpose.name] || null
                            }
                            onChange={(value) =>
                                form.setFieldValue(
                                    `fieldMappings.${purpose.name}`,
                                    value || ""
                                )
                            }
                            required
                            clearable
                        />
                    ))}

                    <Button color={tracker.color} type="submit" mt="xs">
                        Add Analytic
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
