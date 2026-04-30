import {
    Button,
    Divider,
    Modal,
    MultiSelect,
    Select,
    Stack,
    Switch,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useEffect, useMemo, useState } from "react";
import { analyticsController } from "../../analytics/api/analyticsController";
import { AnalyticConfigDto, CodeDto } from "../../analytics/types/AnalyticConfigDto";
import { GetStringValue } from "../../entries/components/EntryFormDialog";
import { useFields } from "../../fields/context/FieldsContext";
import { useTracker } from "../../trackers/context/TrackerContext";
import { useViews } from "../../views/context/ViewsContext";
import { isDynamicDateToken } from "../../../shared/constants/dynamicDateTokens";
import DynamicDateValueInput from "../../../shared/components/DynamicDateValueInput";
import { operatorTypes } from "../../../shared/constants/DataTypesForSelect";
import { useNotifications } from "../context/NotificationsContext";
import { TrackerNotificationDto } from "../types/NotificationDto";
import {
    CreateNotificationConditionDto,
    CreateTrackerNotificationDto,
} from "../types/requests/CreateTrackerNotificationDto";

// Codes whose output is always "number" regardless of the mapped field type
const ALWAYS_NUMBER_CODES = new Set([
    "Count",
    "Count Distinct",
    "True Count",
    "False Count",
    "True Percentage",
    "Standard Deviation",
]);

function getReturnType(code: string, mappedFieldType: string | undefined): string {
    if (ALWAYS_NUMBER_CODES.has(code)) return "number";
    return mappedFieldType ?? "number";
}

interface Props {
    onClose: () => void;
    initialNotification?: TrackerNotificationDto;
}

interface FormValues {
    name: string;
    isEnabled: boolean;
    viewIds: string[];
    conditionCode: string;
    conditionOperator: string;
    conditionValue: string | number | Date | undefined;
    fieldMappings: Record<string, string>;
}

export default function NotificationFormDialog({ onClose, initialNotification }: Props) {
    const { tracker } = useTracker();
    const { fields } = useFields();
    const { views, refreshViewsIfDirty } = useViews();
    const { _createNotification, _updateNotification } = useNotifications();

    const [config, setConfig] = useState<AnalyticConfigDto>();
    const [selectedCode, setSelectedCode] = useState<CodeDto>();

    const isEdit = !!initialNotification;

    useEffect(() => {
        refreshViewsIfDirty();
        analyticsController.getAnalyticsConfig().then((r) => setConfig(r.data));
    }, []);

    const singleValueCodes = useMemo(() => {
        if (!config) return [];
        return config.resultTypes.find((rt) => rt.name === "Single Value")?.codes ?? [];
    }, [config]);

    const fieldsByType = useMemo(() => {
        const grouped: Record<string, Array<{ value: string; label: string }>> = {};
        fields.forEach((f) => {
            if (f.type) {
                if (!grouped[f.type]) grouped[f.type] = [];
                grouped[f.type].push({ value: f.id, label: f.name });
            }
        });
        return grouped;
    }, [fields]);

    const initialFieldMappings: Record<string, string> = {};
    if (initialNotification) {
        initialNotification.condition.conditionFields.forEach((cf) => {
            initialFieldMappings[cf.purpose] = cf.fieldId;
        });
    }

    function parseInitialValue(raw: string): string | number | Date | undefined {
        if (!raw) return undefined;
        if (isDynamicDateToken(raw)) return raw;
        const n = parseFloat(raw);
        if (!isNaN(n)) return n;
        const d = new Date(raw);
        if (!isNaN(d.getTime())) return d;
        return raw;
    }

    const form = useForm<FormValues>({
        initialValues: {
            name: initialNotification?.name ?? "",
            isEnabled: initialNotification?.isEnabled ?? true,
            viewIds: initialNotification?.viewIds ?? [],
            conditionCode: initialNotification?.condition.code ?? "",
            conditionOperator: initialNotification?.condition.operator ?? "",
            conditionValue: initialNotification
                ? parseInitialValue(initialNotification.condition.value)
                : undefined,
            fieldMappings: initialFieldMappings,
        },
        validate: {
            name: (v) => !v.trim() ? "Name is required" : null,
            conditionCode: (v) => !v ? "Select an analytic" : null,
            conditionOperator: (v) => !v ? "Select an operator" : null,
        },
    });

    // Sync selectedCode once config loads (needed for edit mode)
    useEffect(() => {
        if (!initialNotification || singleValueCodes.length === 0) return;
        const found = singleValueCodes.find((c) => c.code === initialNotification.condition.code);
        setSelectedCode(found);
    }, [singleValueCodes]);

    const handleCodeChange = (code: string | null) => {
        form.setFieldValue("conditionCode", code ?? "");
        form.setFieldValue("fieldMappings", {});
        form.setFieldValue("conditionValue", undefined);
        setSelectedCode(singleValueCodes.find((c) => c.code === code));
    };

    // Determine the return type of the selected analytic
    const mappedValueFieldId = form.values.fieldMappings["Value"];
    const mappedValueField = fields.find((f) => f.id === mappedValueFieldId);
    const returnType = selectedCode
        ? getReturnType(form.values.conditionCode, mappedValueField?.type)
        : "number";

    const isDateReturn = returnType === "date" || returnType === "datetime";

    // Virtual field for FieldValueInput
    const virtualField = useMemo(() => ({
        id: "__condition__",
        name: "Value",
        type: returnType,
        required: false,
        description: undefined,
        selectOptions: undefined,
        order: 0,
        isCalculated: false,
    }), [returnType]);

    const handleSubmit = async (values: FormValues) => {
        const conditionFields = selectedCode?.purposes.map((p) => ({
            fieldId: values.fieldMappings[p.name] ?? "",
            purpose: p.name,
        })).filter((f) => f.fieldId) ?? [];

        const rawValue = isDynamicDateToken(values.conditionValue)
            ? (values.conditionValue as string)
            : GetStringValue(returnType, values.conditionValue);

        const condition: CreateNotificationConditionDto = {
            code: values.conditionCode,
            operator: values.conditionOperator,
            value: rawValue,
            conditionFields,
        };

        const dto: CreateTrackerNotificationDto = {
            name: values.name,
            isEnabled: values.isEnabled,
            viewIds: values.viewIds,
            condition,
        };

        if (isEdit) {
            await _updateNotification(initialNotification.id, dto);
        } else {
            await _createNotification(dto);
        }
        onClose();
    };

    return (
        <Modal
            opened
            onClose={onClose}
            title={isEdit ? "Edit Notification" : "Create Notification"}
            centered
            size="lg"
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack gap="md">
                    <TextInput label="Name" required {...form.getInputProps("name")} />

                    <Switch
                        label="Enabled"
                        checked={form.values.isEnabled}
                        onChange={(e) => form.setFieldValue("isEnabled", e.currentTarget.checked)}
                        color={tracker.color}
                    />

                    <MultiSelect
                        label="Scope to Views"
                        placeholder="All entries (no filter)"
                        data={views.map((v) => ({ value: v.id, label: v.name }))}
                        {...form.getInputProps("viewIds")}
                        clearable
                    />

                    <Divider label="Trigger Condition" labelPosition="left" />

                    <Select
                        label="Analytic"
                        placeholder="Select a single-value analytic"
                        data={singleValueCodes.map((c) => ({ value: c.code, label: c.name }))}
                        value={form.values.conditionCode || null}
                        onChange={handleCodeChange}
                        error={form.errors.conditionCode}
                        required
                        searchable
                    />

                    {selectedCode && selectedCode.purposes.map((purpose) => (
                        <Select
                            key={purpose.name}
                            label={purpose.name}
                            placeholder={`Select field (${purpose.allowedDataTypes.join(", ")})`}
                            data={purpose.allowedDataTypes.flatMap((type) => fieldsByType[type] || [])}
                            value={form.values.fieldMappings[purpose.name] || null}
                            onChange={(value) => {
                                form.setFieldValue(`fieldMappings.${purpose.name}`, value ?? "");
                                form.setFieldValue("conditionValue", undefined);
                            }}
                            required
                            clearable
                        />
                    ))}

                    {selectedCode && (
                        <>
                            <Select
                                label="Operator"
                                placeholder="Select operator"
                                allowDeselect={false}
                                data={operatorTypes}
                                {...form.getInputProps("conditionOperator")}
                                required
                            />

                            <DynamicDateValueInput
                                isDateType={isDateReturn}
                                value={form.values.conditionValue}
                                onChange={(v) => form.setFieldValue("conditionValue", v)}
                                field={virtualField as any}
                                form={form}
                                fieldPath="conditionValue"
                                label="Value"
                            />
                        </>
                    )}

                    <Button color={tracker.color} type="submit" mt="xs">
                        {isEdit ? "Save Changes" : "Create Notification"}
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
