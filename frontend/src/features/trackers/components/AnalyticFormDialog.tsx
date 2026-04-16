import { Button, Modal, Select, Stack, Text } from "@mantine/core";
import { useForm } from "@mantine/form";
import { useMemo } from "react";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import {
    CodeDto,
    ResultTypeDto,
} from "../../analytics/types/AnalyticConfigDto";
import { CreateAnalyticDto } from "../../analytics/types/requests/CreateAnalyticDto";
import { useFields } from "../../fields/context/FieldsContext";
import { useTracker } from "../context/TrackerContext";

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

export default function AnalyticFormDialog({
    onClose,
    selectedAnalytic,
}: Props) {
    const { tracker } = useTracker();
    const { fields } = useFields();
    const { addAnalytic } = useTrackerOperations();
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

        const dto: CreateAnalyticDto = {
            code: code.code,
            type: resultType.name,
            analyticFields,
        };

        await addAnalytic(dto);
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
