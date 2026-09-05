import {
    ActionIcon,
    Button,
    Checkbox,
    Divider,
    Group,
    Modal,
    MultiSelect,
    NumberInput,
    Paper,
    SegmentedControl,
    Select,
    Stack,
    Switch,
    Text,
    Textarea,
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
import { NotificationPurposes } from "../constants/NotificationPurposes";
import { useNotifications } from "../context/NotificationsContext";
import { TrackerNotificationDto } from "../types/NotificationDto";
import { CreateTrackerNotificationDto } from "../types/requests/CreateTrackerNotificationDto";
import { buildNotificationSentence, displayValue } from "../utils/notificationSummary";

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
    viewId: string | null;
    messageTemplate: string;

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
    displayFieldIds: string[];

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
            viewId: null,
            messageTemplate: "",
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
            displayFieldIds: [],
            filters: [],
        };

        const ev = initialNotification.event ?? {};
        const cond = initialNotification.condition ?? {};

        return {
            name: initialNotification.name,
            isEnabled: initialNotification.isEnabled,
            viewId: initialNotification.viewId ?? null,
            messageTemplate: initialNotification.messageTemplate ?? "",
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
                (cond.purposeFields ?? [])
                    .filter((pf) => pf.purpose !== NotificationPurposes.Display)
                    .map((pf) => [pf.purpose, pf.fieldId])
            ),
            displayFieldIds: (cond.purposeFields ?? [])
                .filter((pf) => pf.purpose === NotificationPurposes.Display)
                .map((pf) => pf.fieldId),
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

    const isEntry = form.values.valueMode === "Entry";
    const isScheduled = form.values.eventType !== "Triggered";

    // --- Live "Notify me..." preview ---

    const sentence = useMemo(() => {
        const clauses = isEntry
            ? form.values.filters
                  .filter((f) => f.fieldId && f.operator)
                  .map((f) => ({
                      subject: fields.find((fd) => fd.id === f.fieldId)?.name ?? "field",
                      operator: f.operator,
                      value: displayValue(f.value),
                  }))
            : form.values.filters
                  .filter((f) => f.operator)
                  .map((f) => ({ subject: "", operator: f.operator, value: displayValue(f.value) }));

        const analyticSubject = selectedCode
            ? `the ${selectedCode.name}${mappedValueField ? ` of ${mappedValueField.name}` : ""}`
            : "the value";

        return buildNotificationSentence({
            valueMode: form.values.valueMode,
            isScheduled,
            event: form.values,
            analyticSubject,
            clauses,
        });
    }, [form.values, isEntry, isScheduled, fields, selectedCode, mappedValueField]);

    const handleSubmit = (values: FormValues) => {
        const viewId = values.viewId;

        const purposeFields = values.valueMode === "Analytic"
            ? selectedCode?.purposes.map((p) => ({
                  fieldId: values.fieldMappings[p.name] ?? "",
                  purpose: p.name,
              })).filter((f) => f.fieldId) ?? []
            : values.displayFieldIds.map((fieldId) => ({
                  fieldId,
                  purpose: NotificationPurposes.Display,
              }));

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
            viewId,
            messageTemplate: values.messageTemplate.trim() || null,
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
                purposeFields,
                filters,
            },
        };

        if (isEdit) {
            _updateNotification(initialNotification.id, dto).then(onClose);
        } else {
            _createNotification(dto).then(onClose);
        }
    };

    const addAnalyticFilter = () => {
        form.insertListItem("filters", { fieldId: "", operator: "", value: "" });
    };

    const messagePlaceholder = isEntry
        ? "e.g. \"{count} entries need review:\\n{fieldValueList}\" (defaults to \"{count} new entries match\")"
        : "e.g. \"Amount is now {value}\" (defaults to \"Condition met\")";

    const messageHint = isEntry
        ? "Available: {count}, {tracker}, {notification}, {fieldValueList}."
        : "Available: {value}, {tracker}, {notification}.";

    return (
        <Modal
            opened
            onClose={onClose}
            title={isEdit ? "Edit Notification" : "Create Notification"}
            centered
            size="lg"
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack gap="lg">
                    <Stack gap="md">
                        <TextInput label="Name" required {...form.getInputProps("name")} />
                        <Switch
                            label="Enabled"
                            checked={form.values.isEnabled}
                            onChange={(e) => form.setFieldValue("isEnabled", e.currentTarget.checked)}
                            color={tracker.color}
                        />
                    </Stack>

                    <Paper p="md" radius="md" withBorder bg="var(--mantine-color-default-hover)">
                        <Text size="sm" fs="italic">
                            "{sentence}"
                        </Text>
                    </Paper>

                    <Divider />

                    <Stack gap="sm">
                        <Text fw={600} size="sm">Watch</Text>
                        <SegmentedControl
                            fullWidth
                            data={[
                                { value: "Entry", label: "Entry records" },
                                { value: "Analytic", label: "Computed value" },
                            ]}
                            value={form.values.valueMode}
                            onChange={(v) => {
                                form.setFieldValue("valueMode", v);
                                form.setFieldValue("filters", []);
                                form.setFieldValue("analyticCode", "");
                                form.setFieldValue("fieldMappings", {});
                                form.setFieldValue("displayFieldIds", []);
                                setSelectedCode(undefined);
                            }}
                        />

                        {!isEntry && (
                            <Stack gap="md" mt="xs">
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
                        )}
                    </Stack>

                    <Divider />

                    <Stack gap="sm">
                        <Text fw={600} size="sm">Condition</Text>

                        {isEntry ? (
                            <EntryFilterListEditor fields={fields} form={form} color={tracker.color} />
                        ) : (
                            <Stack gap="md">
                                {form.values.filters.length === 0 && (
                                    <Text c="dimmed" size="sm">No conditions, fires on schedule.</Text>
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
                                                variant="outline"
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
                        )}

                        {isEntry && !isEdit && (
                            <Text c="dimmed" size="xs">
                                Entries that already match won't notify you when this is first created.
                            </Text>
                        )}
                    </Stack>

                    <Divider />

                    <Stack gap="sm">
                        <Text fw={600} size="sm">When</Text>
                        <SegmentedControl
                            fullWidth
                            data={[
                                { value: "Triggered", label: "On change" },
                                { value: "scheduled", label: "On a schedule" },
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

                        {!isScheduled && (
                            <Text c="dimmed" size="xs">
                                {isEntry
                                    ? "Only newly matching entries are reported, not ones that already matched."
                                    : "Notifies once when this turns true, and won't repeat until it becomes false again first."}
                            </Text>
                        )}

                        {isScheduled && (
                            <Stack gap="sm" mt="xs">
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

                                <TimePicker
                                    label="Time of Day"
                                    format="24h"
                                    {...form.getInputProps("timeOfDay")}
                                />

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

                                <Text c="dimmed" size="xs">
                                    {isEntry
                                        ? "Checks on this schedule and reports only entries that are newly matching since the last check."
                                        : "Checks on this schedule and notifies every time the condition is true."}
                                </Text>
                            </Stack>
                        )}
                    </Stack>

                    <Divider />

                    {isEntry && (
                        <MultiSelect
                            label="Fields to list"
                            description="Used by the {fieldValueList} token below, one line per entry."
                            placeholder="Select fields"
                            data={fields.map((f) => ({ value: f.id, label: f.name }))}
                            {...form.getInputProps("displayFieldIds")}
                            clearable
                        />
                    )}

                    <Textarea
                        label="Custom message"
                        placeholder={messagePlaceholder}
                        description={messageHint}
                        autosize
                        minRows={2}
                        maxLength={200}
                        {...form.getInputProps("messageTemplate")}
                    />

                    <Select
                        label="Scope to View"
                        placeholder="All entries (no filter)"
                        data={views.map((v) => ({ value: v.id, label: v.name }))}
                        {...form.getInputProps("viewId")}
                        clearable
                    />

                    <Button color={tracker.color} type="submit" size="md">
                        {isEdit ? "Save Changes" : "Create Notification"}
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
