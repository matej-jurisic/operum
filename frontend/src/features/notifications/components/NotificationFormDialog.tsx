import {
    ActionIcon,
    Button,
    Checkbox,
    Divider,
    Group,
    Modal,
    MultiSelect,
    NumberInput,
    SegmentedControl,
    Select,
    Stack,
    Stepper,
    Switch,
    Text,
    TextInput,
} from "@mantine/core";
import { TimePicker } from "@mantine/dates";
import { useForm } from "@mantine/form";
import { useEffect, useMemo, useRef, useState } from "react";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import { analyticsController } from "../../analytics/api/analyticsController";
import { AnalyticConfigDto, CodeDto } from "../../analytics/types/AnalyticConfigDto";
import { GetStringValue } from "../../entries/components/EntryFormDialog";
import { useFields } from "../../fields/context/FieldsContext";
import { useTracker } from "../../trackers/context/TrackerContext";
import { useViews } from "../../views/context/ViewsContext";
import DynamicDateValueInput from "../../../shared/components/DynamicDateValueInput";
import { isDynamicDateToken } from "../../../shared/constants/dynamicDateTokens";
import { operatorTypes } from "../../../shared/constants/DataTypesForSelect";
import EntryFilterListEditor from "../../views/components/EntryFilterListEditor";
import { useNotifications } from "../context/NotificationsContext";
import { TrackerNotificationDto } from "../types/NotificationDto";
import {
    CreateTrackerNotificationDto,
} from "../types/requests/CreateTrackerNotificationDto";

const ALWAYS_NUMBER_CODES = new Set([
    "Count", "Count Distinct", "True Count", "False Count",
    "True Percentage", "Standard Deviation",
]);

function getReturnType(code: string, mappedFieldType: string | undefined): string {
    if (ALWAYS_NUMBER_CODES.has(code)) return "number";
    return mappedFieldType ?? "number";
}

const DAYS_OF_WEEK = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

function getFormValue(type: string, storedValue: string | undefined) {
    if (!storedValue) return undefined;
    switch (type) {
        case "date":
        case "datetime":
            if (isDynamicDateToken(storedValue)) return storedValue;
            return new Date(storedValue);
        case "number":
            return parseFloat(storedValue);
        case "bool":
            return storedValue.toLowerCase();
        default:
            return storedValue;
    }
}

interface FilterRow {
    fieldId: string;
    operator: string;
    value: string | number | Date | undefined;
}

interface FormValues {
    name: string;
    isEnabled: boolean;
    viewIds: string[];

    // Event
    eventType: string;
    timeOfDay: string;
    intervalDays: number;
    skipWeekendsDay: boolean;
    intervalWeeks: number;
    daysOfWeek: string[];
    dayOfMonth: number;
    lastDayOfMonth: boolean;
    skipWeekendsMonth: boolean;

    // Value
    valueMode: string;
    analyticCode: string;
    fieldMappings: Record<string, string>;

    // Condition filters
    filters: FilterRow[];
}

interface Props {
    onClose: () => void;
    initialNotification?: TrackerNotificationDto;
}

export default function NotificationFormDialog({ onClose, initialNotification }: Props) {
    const { tracker } = useTracker();
    const { fields, refreshFieldsIfDirty } = useFields();
    const { views, refreshViewsIfDirty } = useViews();
    const { _createNotification, _updateNotification } = useNotifications();

    const [active, setActive] = useState(0);
    const [config, setConfig] = useState<AnalyticConfigDto>();
    const [selectedCode, setSelectedCode] = useState<CodeDto>();

    const isEdit = !!initialNotification;
    const filtersInitialized = useRef(false);

    useEffect(() => {
        refreshViewsIfDirty();
        refreshFieldsIfDirty();
        analyticsController.getAnalyticsConfig().then((r) => setConfig(r.data));
    }, []);

    // Re-convert Entry-mode filter values from stored strings once fields load
    useEffect(() => {
        if (!initialNotification || fields.length === 0 || filtersInitialized.current) return;
        if (initialNotification.condition.valueMode !== "Entry") return;
        filtersInitialized.current = true;
        const converted = (initialNotification.condition.filters ?? []).map((f) => {
            const field = fields.find((fd) => fd.id === f.fieldId);
            return {
                fieldId: f.fieldId ?? "",
                operator: f.operator ?? "",
                value: field ? getFormValue(field.type, f.value ?? undefined) : (f.value ?? ""),
            };
        });
        form.setFieldValue("filters", converted);
    }, [fields]);

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

    const buildInitialValues = (): FormValues => {
        if (!initialNotification) return {
            name: "",
            isEnabled: true,
            viewIds: [],
            eventType: "Triggered",
            timeOfDay: "09:00",
            intervalDays: 1,
            skipWeekendsDay: false,
            intervalWeeks: 1,
            daysOfWeek: ["Mon", "Tue", "Wed", "Thu", "Fri"],
            dayOfMonth: 1,
            lastDayOfMonth: false,
            skipWeekendsMonth: false,
            valueMode: "Analytic",
            analyticCode: "",
            fieldMappings: {},
            filters: [],
        };

        const ev = initialNotification.event ?? {};
        const cond = initialNotification.condition ?? {};

        return {
            name: initialNotification.name,
            isEnabled: initialNotification.isEnabled,
            viewIds: initialNotification.viewIds ?? [],
            eventType: ev.eventType ?? "Triggered",
            timeOfDay: ev.timeOfDay ?? "09:00",
            intervalDays: ev.intervalDays ?? 1,
            skipWeekendsDay: ev.skipWeekendsDay ?? false,
            intervalWeeks: ev.intervalWeeks ?? 1,
            daysOfWeek: ev.daysOfWeek ?? [],
            dayOfMonth: ev.dayOfMonth ?? 1,
            lastDayOfMonth: ev.lastDayOfMonth ?? false,
            skipWeekendsMonth: ev.skipWeekendsMonth ?? false,
            valueMode: cond.valueMode ?? "Analytic",
            analyticCode: cond.analyticCode ?? "",
            fieldMappings: Object.fromEntries(
                (cond.purposeFields ?? []).map((pf) => [pf.purpose, pf.fieldId])
            ),
            filters: (cond.filters ?? []).map((f) => ({
                fieldId: f.fieldId ?? "",
                operator: f.operator ?? "",
                value: f.value ?? "",
            })),
        };
    };

    const form = useForm<FormValues>({
        initialValues: buildInitialValues(),
        validate: {
            name: (v) => !v.trim() ? "Name is required" : null,
        },
    });

    // Sync selectedCode once config loads (edit mode)
    useEffect(() => {
        if (!initialNotification || singleValueCodes.length === 0) return;
        const found = singleValueCodes.find((c) => c.code === initialNotification.condition.analyticCode);
        setSelectedCode(found);
    }, [singleValueCodes]);

    const handleCodeChange = (code: string | null) => {
        form.setFieldValue("analyticCode", code ?? "");
        form.setFieldValue("fieldMappings", {});
        form.setFieldValue("filters", []);
        setSelectedCode(singleValueCodes.find((c) => c.code === code));
    };

    const mappedValueFieldId = form.values.fieldMappings["Value"];
    const mappedValueField = fields.find((f) => f.id === mappedValueFieldId);
    const returnType = selectedCode
        ? getReturnType(form.values.analyticCode, mappedValueField?.type)
        : "number";

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

    const handleSubmit = (values: FormValues) => {
        const viewIds = values.viewIds;

        const purposeFields = selectedCode?.purposes.map((p) => ({
            fieldId: values.fieldMappings[p.name] ?? "",
            purpose: p.name,
        })).filter((f) => f.fieldId) ?? [];

        const filters = values.filters.map((f) => {
            if (values.valueMode === "Entry") {
                const field = fields.find((fd) => fd.id === f.fieldId);
                return {
                    fieldId: f.fieldId || null,
                    operator: f.operator,
                    value: field ? GetStringValue(field.type, f.value) : String(f.value ?? ""),
                };
            }
            // Analytic mode
            const isDateReturn = returnType === "date" || returnType === "datetime";
            const raw = isDynamicDateToken(f.value)
                ? (f.value as string)
                : isDateReturn
                  ? GetStringValue(returnType, f.value)
                  : String(f.value ?? "");
            return { fieldId: null, operator: f.operator, value: raw };
        });

        const dto: CreateTrackerNotificationDto = {
            name: values.name,
            isEnabled: values.isEnabled,
            viewIds,
            event: {
                eventType: values.eventType,
                timeOfDay: values.eventType !== "Triggered" ? values.timeOfDay : null,
                intervalDays: values.eventType === "Day" ? values.intervalDays : null,
                skipWeekendsDay: values.eventType === "Day" ? values.skipWeekendsDay : null,
                intervalWeeks: values.eventType === "Week" ? values.intervalWeeks : null,
                daysOfWeek: values.eventType === "Week" ? values.daysOfWeek : null,
                dayOfMonth: values.eventType === "Month" && !values.lastDayOfMonth ? values.dayOfMonth : null,
                lastDayOfMonth: values.eventType === "Month" ? values.lastDayOfMonth : null,
                skipWeekendsMonth: values.eventType === "Month" ? values.skipWeekendsMonth : null,
            },
            condition: {
                valueMode: values.valueMode,
                analyticCode: values.valueMode === "Analytic" ? values.analyticCode : null,
                purposeFields: values.valueMode === "Analytic" ? purposeFields : [],
                filters,
            },
        };

        if (isEdit) {
            _updateNotification(initialNotification.id, dto).then(onClose);
        } else {
            _createNotification(dto).then(onClose);
        }
    };

    // --- Step renderers ---

    const isScheduled = form.values.eventType !== "Triggered";

    const EventStep = () => (
        <Stack gap="md" mt="md">
            <SegmentedControl
                fullWidth
                data={[
                    { value: "scheduled", label: "Repeating" },
                    { value: "Triggered", label: "On Change" },
                ]}
                value={isScheduled ? "scheduled" : "Triggered"}
                onChange={(v) => {
                    if (v === "Triggered") {
                        form.setFieldValue("eventType", "Triggered");
                    } else {
                        form.setFieldValue("eventType", "Day");
                    }
                }}
            />

            {isScheduled && (
                <SegmentedControl
                    fullWidth
                    data={[
                        { value: "Day", label: "Daily" },
                        { value: "Week", label: "Weekly" },
                        { value: "Month", label: "Monthly" },
                    ]}
                    value={form.values.eventType}
                    onChange={(v) => form.setFieldValue("eventType", v)}
                />
            )}

            {isScheduled && (
                <TimePicker
                    label="Time of Day"
                    format="24h"
                    {...form.getInputProps("timeOfDay")}
                />
            )}

            {form.values.eventType === "Day" && (
                <Stack gap="sm">
                    <NumberInput
                        label="Every N days"
                        min={1} max={365}
                        {...form.getInputProps("intervalDays")}
                    />
                    <Checkbox
                        label="Skip weekends"
                        {...form.getInputProps("skipWeekendsDay", { type: "checkbox" })}
                    />
                </Stack>
            )}

            {form.values.eventType === "Week" && (
                <Stack gap="sm">
                    <NumberInput
                        label="Every N weeks"
                        min={1} max={52}
                        {...form.getInputProps("intervalWeeks")}
                    />
                    <MultiSelect
                        label="Days of week"
                        data={DAYS_OF_WEEK}
                        {...form.getInputProps("daysOfWeek")}
                    />
                </Stack>
            )}

            {form.values.eventType === "Month" && (
                <Stack gap="sm">
                    <Checkbox
                        label="Last day of month"
                        {...form.getInputProps("lastDayOfMonth", { type: "checkbox" })}
                    />
                    {!form.values.lastDayOfMonth && (
                        <NumberInput
                            label="Day of month"
                            min={1} max={31}
                            {...form.getInputProps("dayOfMonth")}
                        />
                    )}
                    <Checkbox
                        label="Skip weekends"
                        {...form.getInputProps("skipWeekendsMonth", { type: "checkbox" })}
                    />
                </Stack>
            )}
        </Stack>
    );

    const ValueStep = () => (
        <Stack gap="md" mt="md">
            <SegmentedControl
                fullWidth
                data={[
                    { value: "Analytic", label: "Computed value" },
                    { value: "Entry", label: "Entry records" },
                ]}
                value={form.values.valueMode}
                onChange={(v) => {
                    form.setFieldValue("valueMode", v);
                    form.setFieldValue("filters", []);
                    form.setFieldValue("analyticCode", "");
                    form.setFieldValue("fieldMappings", {});
                    setSelectedCode(undefined);
                }}
            />

            {form.values.valueMode === "Analytic" ? (
                <Stack gap="md">
                    <Text size="sm" c="dimmed">
                        Pick an analytic — the condition in the next step checks its computed result.
                    </Text>
                    <Select
                        label="Analytic"
                        placeholder="Select a single-value analytic"
                        data={singleValueCodes.map((c) => ({ value: c.code, label: c.name }))}
                        value={form.values.analyticCode || null}
                        onChange={handleCodeChange}
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
                                form.setFieldValue("filters", []);
                            }}
                            clearable
                        />
                    ))}
                </Stack>
            ) : (
                <Text size="sm" c="dimmed">
                    The condition in the next step filters entries directly — the notification fires when new entries match.
                </Text>
            )}
        </Stack>
    );

    const ConditionStep = () => {
        const isEntry = form.values.valueMode === "Entry";

        const addAnalyticFilter = () => {
            form.insertListItem("filters", {
                fieldId: "",
                operator: "",
                value: "",
            });
        };

        if (isEntry) {
            return (
                <Stack gap="md" mt="md">
                    <EntryFilterListEditor
                        fields={fields}
                        form={form}
                        color={tracker.color}
                    />
                </Stack>
            );
        }

        return (
            <Stack gap="md" mt="md">
                {form.values.filters.length === 0 && (
                    <Text c="dimmed" size="sm">No conditions — always fires on schedule.</Text>
                )}

                {form.values.filters.map((filter, i) => {
                    const isDateFilter = returnType === "date" || returnType === "datetime";

                    return (
                        <Group key={i} align="flex-end" gap="xs" wrap="nowrap">
                            <Select
                                label="Operator"
                                placeholder="Op"
                                allowDeselect={false}
                                data={operatorTypes}
                                value={filter.operator || null}
                                onChange={(v) => form.setFieldValue(`filters.${i}.operator`, v ?? "")}
                                style={{ flex: 1 }}
                            />
                            <DynamicDateValueInput
                                isDateType={isDateFilter}
                                value={form.values.filters[i]?.value}
                                onChange={(v) => form.setFieldValue(`filters.${i}.value`, v)}
                                field={{ ...virtualField, type: returnType } as any}
                                form={form}
                                fieldPath={`filters.${i}.value`}
                                label="Value"
                            />
                            <ActionIcon
                                color="red"
                                variant="subtle"
                                onClick={() => form.removeListItem("filters", i)}
                                mt="lg"
                            >
                                <MdDelete size={16} />
                            </ActionIcon>
                        </Group>
                    );
                })}

                <Button
                    variant="subtle"
                    leftSection={<FiPlus size={14} />}
                    onClick={addAnalyticFilter}
                    size="sm"
                >
                    Add condition
                </Button>
            </Stack>
        );
    };

    const eventLabel: Record<string, string> = {
        Day: "Daily", Week: "Weekly", Month: "Monthly", Triggered: "On Change",
    };

    const valueModeLabel: Record<string, string> = {
        Analytic: "Computed value",
        Entry: "Entry records",
    };

    const steps = [
        { label: "Event", description: eventLabel[form.values.eventType] ?? form.values.eventType },
        { label: "Evaluate", description: valueModeLabel[form.values.valueMode] ?? form.values.valueMode },
        { label: "Condition", description: `${form.values.filters.length} filters` },
    ];

    return (
        <Modal
            opened
            onClose={onClose}
            title={isEdit ? "Edit Notification" : "Create Notification"}
            centered
            size="lg"
        >
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

                    <Divider />

                    <Stepper active={active} onStepClick={setActive} size="sm">
                        {steps.map((s, i) => (
                            <Stepper.Step key={i} label={s.label} description={s.description}>
                                {i === 0 && EventStep()}
                                {i === 1 && ValueStep()}
                                {i === 2 && ConditionStep()}
                            </Stepper.Step>
                        ))}
                    </Stepper>

                    <Group justify="space-between" mt="xs">
                        <Button
                            variant="default"
                            onClick={() => setActive((p) => Math.max(0, p - 1))}
                            disabled={active === 0}
                        >
                            Back
                        </Button>
                        {active < steps.length - 1 ? (
                            <Button
                                color={tracker.color}
                                onClick={() => setActive((p) => p + 1)}
                            >
                                Next
                            </Button>
                        ) : (
                            <Button
                                color={tracker.color}
                                onClick={() => form.onSubmit(handleSubmit)()}
                            >
                                {isEdit ? "Save Changes" : "Create Notification"}
                            </Button>
                        )}
                    </Group>
                </Stack>
        </Modal>
    );
}
