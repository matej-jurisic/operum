import {
    Button,
    Group,
    Modal,
    Paper,
    SegmentedControl,
    Select,
    Stack,
    Text,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import DynamicDateValueInput from "../../../shared/components/DynamicDateValueInput";
import { FieldTypes } from "../../../shared/constants/DataTypes";
import { operatorsForFieldType } from "../../../shared/constants/DataTypesForSelect";
import { isDynamicDateToken } from "../../../shared/constants/dynamicDateTokens";
import {
    QueryKind,
    QueryKindLabel,
    QueryKinds,
} from "../../../shared/constants/QueryKinds";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { describeClause } from "../../../shared/utils/formatters/QueryFormatter";
import { GetStringValue } from "../../entries/components/EntryFormDialog";
import { useFields } from "../../fields/context/FieldsContext";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { QueryDto } from "../types/QueryDto";
import { CreateQueryDto } from "../types/requests/CreateQueryDto";

interface QueryFormValues {
    kind: QueryKind;
    fieldId: string;
    operator: string;
    value?: string | number | Date;
    descending: boolean;
}

interface Props {
    tracker: TrackerDto;
    /** Set when editing a saved query. */
    queryId?: string;
    initialQuery?: QueryDto;
    /** Set when editing a clause that is not saved yet (a view being authored). */
    initialClause?: CreateQueryDto;
    /**
     * When given, the dialog hands the clause back instead of saving it, which is how a
     * view being authored collects ad-hoc queries without creating them up front.
     */
    onSubmitClause?: (clause: CreateQueryDto) => void;
    onClose: () => void;
    onSaved?: (query: QueryDto) => void;
}

function getFormValue(type: string | undefined, storedValue: string | undefined | null) {
    if (!storedValue) return undefined;
    switch (type) {
        case FieldTypes.Date:
        case FieldTypes.DateTime:
            if (isDynamicDateToken(storedValue)) return storedValue;
            return new Date(storedValue);
        case FieldTypes.Number:
            return parseFloat(storedValue);
        case FieldTypes.Bool:
            return storedValue.toLowerCase();
        default:
            return storedValue;
    }
}

/** Create or edit a single query: one filter, or one sort. */
export default function QueryFormDialog({
    tracker,
    queryId,
    initialQuery,
    initialClause,
    onSubmitClause,
    onClose,
    onSaved,
}: Props) {
    const { fields } = useFields();
    const { createQuery, updateQuery } = useTrackerOperations();

    const getFieldById = (fieldId: string) =>
        fields.find((f) => f.id === fieldId);

    const initialValues = (): QueryFormValues => {
        if (initialQuery)
            return {
                kind: initialQuery.kind,
                fieldId: initialQuery.field.id,
                operator: initialQuery.operator ?? "",
                value: getFormValue(initialQuery.field.type, initialQuery.value),
                descending: initialQuery.descending,
            };
        if (initialClause)
            return {
                kind: initialClause.kind,
                fieldId: initialClause.fieldId,
                operator: initialClause.operator ?? "",
                value: getFormValue(
                    getFieldById(initialClause.fieldId)?.type,
                    initialClause.value,
                ),
                descending: initialClause.descending ?? false,
            };
        return {
            kind: QueryKinds.Filter,
            fieldId: "",
            operator: "",
            value: undefined,
            descending: false,
        };
    };

    const form = useForm<QueryFormValues>({
        initialValues: initialValues(),
        validate: {
            fieldId: (value) => (!value ? "Pick a field" : null),
            operator: (value, values) =>
                values.kind === QueryKinds.Filter && !value
                    ? "Pick an operator"
                    : null,
        },
    });

    const selectedField = getFieldById(form.values.fieldId);
    const isFilter = form.values.kind === QueryKinds.Filter;
    const operatorOptions = operatorsForFieldType(selectedField?.type);

    const toClause = (values: QueryFormValues): CreateQueryDto => {
        if (values.kind === QueryKinds.Sort)
            return {
                kind: QueryKinds.Sort,
                fieldId: values.fieldId,
                descending: values.descending,
            };

        const field = getFieldById(values.fieldId);
        return {
            kind: QueryKinds.Filter,
            fieldId: values.fieldId,
            operator: values.operator,
            value:
                values.value !== undefined && field
                    ? isDynamicDateToken(values.value)
                        ? (values.value as string)
                        : GetStringValue(field.type, values.value)
                    : undefined,
        };
    };

    const handleSubmit = async (values: QueryFormValues) => {
        const clause = toClause(values);

        if (onSubmitClause) {
            onSubmitClause(clause);
            onClose();
            return;
        }

        const saved = queryId
            ? await updateQuery(queryId, clause)
            : await createQuery(clause);
        onSaved?.(saved);
        onClose();
    };

    // What the clause reads as, kept in front of the user while they build it: a query
    // has no name, so this is the only handle they will ever have on it.
    const preview = describeClause({
        kind: form.values.kind,
        field: selectedField,
        operator: form.values.operator,
        value:
            isFilter && form.values.value !== undefined && selectedField
                ? isDynamicDateToken(form.values.value)
                    ? (form.values.value as string)
                    : GetStringValue(selectedField.type, form.values.value)
                : undefined,
        descending: form.values.descending,
    });

    return (
        <Modal
            opened
            centered
            onClose={onClose}
            title={queryId || initialClause ? "Edit Query" : "New Query"}
            size="lg"
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack gap="lg">
                    <SegmentedControl
                        fullWidth
                        color={tracker.color}
                        data={[
                            {
                                value: QueryKinds.Filter,
                                label: QueryKindLabel.filter,
                            },
                            {
                                value: QueryKinds.Sort,
                                label: QueryKindLabel.sort,
                            },
                        ]}
                        value={form.values.kind}
                        onChange={(kind) => {
                            form.setFieldValue("kind", kind as QueryKind);
                            form.setFieldValue("operator", "");
                            form.setFieldValue("value", undefined);
                        }}
                    />

                    <Select
                        label="Field"
                        placeholder="Select field"
                        allowDeselect={false}
                        searchable
                        data={fields.map((f) => ({
                            value: f.id,
                            label: f.name,
                        }))}
                        {...form.getInputProps("fieldId")}
                        onChange={(fieldId) => {
                            form.setFieldValue("fieldId", fieldId || "");
                            form.setFieldValue("operator", "");
                            form.setFieldValue("value", undefined);
                        }}
                    />

                    {isFilter ? (
                        <>
                            <Select
                                label="Operator"
                                placeholder="Select operator"
                                allowDeselect={false}
                                disabled={!selectedField}
                                data={operatorOptions}
                                {...form.getInputProps("operator")}
                            />
                            {selectedField && (
                                <Group align="flex-end">
                                    <DynamicDateValueInput
                                        isDateType={
                                            selectedField.type ===
                                                FieldTypes.Date ||
                                            selectedField.type ===
                                                FieldTypes.DateTime
                                        }
                                        value={form.values.value}
                                        onChange={(v) =>
                                            form.setFieldValue("value", v)
                                        }
                                        field={selectedField}
                                        form={form}
                                        fieldPath="value"
                                        label={selectedField.name}
                                    />
                                </Group>
                            )}
                            <Text c="dimmed" size="xs">
                                An empty value matches entries with no value.
                            </Text>
                        </>
                    ) : (
                        <SegmentedControl
                            data={[
                                { value: "asc", label: "Ascending" },
                                { value: "desc", label: "Descending" },
                            ]}
                            value={form.values.descending ? "desc" : "asc"}
                            onChange={(v) =>
                                form.setFieldValue("descending", v === "desc")
                            }
                        />
                    )}

                    <Paper p="sm" withBorder>
                        <Text size="sm" c={form.values.fieldId ? undefined : "dimmed"}>
                            {form.values.fieldId
                                ? preview
                                : "Pick a field to see what this query does."}
                        </Text>
                    </Paper>

                    <Button color={tracker.color} type="submit" size="md">
                        {queryId || initialClause ? "Save Query" : "Add Query"}
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
