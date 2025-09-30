import {
    ActionIcon,
    Button,
    Group,
    Modal,
    Paper,
    Select,
    Stack,
    Text,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import api from "../api/api";
import { fieldTypes } from "../model/constants/DataTypesForSelect";
import { AnalyticType } from "../model/enums/AnalyticTypeEnum";
import { CreateAnalyticDto } from "../model/requests/CreateAnalyticDto";

const analyticTypeOptions = [
    { value: AnalyticType.Draft.toString(), label: "Draft" },
    { value: AnalyticType.Public.toString(), label: "Public" },
];

const analyticResultTypeOptions = [
    { value: "SingleValue", label: "Single Value" },
    { value: "NumericChart", label: "Numeric Chart" },
];

const analyticCodeOptions = [
    { value: "Count", label: "Count" },
    { value: "Min", label: "Min" },
    { value: "Max", label: "Max" },
    { value: "Average", label: "Average" },
    { value: "Sum", label: "Sum" },
    { value: "StdDev", label: "StdDev" },
    { value: "TrueCount", label: "TrueCount" },
    { value: "FalseCount", label: "FalseCount" },
    { value: "TruePercentage", label: "TruePercentage" },
    { value: "AggregatedLineChart", label: "AggregatedLineChart" },
    { value: "CumulativeLineChart", label: "CumulativeLineChart" },
];

interface CreateAnalyticDialogProps {
    onClose: () => void;
    onFieldAdded?: () => void;
}

const MAX_ANALYTICS = 10;

export function CreateAnalyticDialog(props: CreateAnalyticDialogProps) {
    const form = useForm<CreateAnalyticDto>({
        initialValues: {
            name: "",
            description: "",
            resultType: "",
            analyticRequiredDataTypesList: [],
            analyticTypeId: AnalyticType.Draft.toString(),
            code: "",
        },

        validate: {
            name: (value) =>
                !value.trim()
                    ? "Analytic name is required"
                    : value.length > 100
                    ? "Analytic name must be at most 100 characters"
                    : null,
            description: (value) =>
                value && value.length > 500
                    ? "Description must be at most 500 characters"
                    : null,
            code: (value) =>
                !value.trim()
                    ? "Analytic code is required"
                    : value.length > 100
                    ? "Analytic code must be at most 100 characters"
                    : null,
            analyticTypeId: (value) =>
                !value ? "Analytic type is required" : null,
            resultType: (value) => (!value ? "Result type is required" : null),
            analyticRequiredDataTypesList: (value, values) => {
                if (!values.resultType) return null;
                if (value.length === 0)
                    return "At least one analytic configuration is required";

                for (let i = 0; i < value.length; i++) {
                    const config = value[i];

                    if (values.resultType === "SingleValue") {
                        if (config.length !== 1) {
                            return `Configuration ${
                                i + 1
                            }: Single Value requires exactly 1 field`;
                        }
                        if (!config[0].type) {
                            return `Configuration ${i + 1}: Type is required`;
                        }
                    } else if (values.resultType === "NumericChart") {
                        if (config.length !== 2) {
                            return `Configuration ${
                                i + 1
                            }: Numeric Chart requires exactly 2 fields`;
                        }
                        if (!config[0].type || !config[1].type) {
                            return `Configuration ${
                                i + 1
                            }: All fields must have a type`;
                        }
                    }
                }

                return null;
            },
        },
    });

    const handleSubmit = async (values: CreateAnalyticDto) => {
        await api.post(`/analytics`, values);
        props.onFieldAdded?.();
        form.reset();
        props.onClose();
    };

    const canAddAnalytic =
        form.values.analyticRequiredDataTypesList.length < MAX_ANALYTICS &&
        form.values.resultType !== "";

    const addAnalytic = () => {
        if (!canAddAnalytic) return;

        const initialConfig =
            form.values.resultType === "SingleValue"
                ? [{ type: "", purpose: "Value" }]
                : [
                      { type: "", purpose: "X-axis" },
                      { type: "", purpose: "Y-axis" },
                  ];

        form.insertListItem("analyticRequiredDataTypesList", initialConfig);
    };

    const removeAnalytic = (index: number) => {
        form.removeListItem("analyticRequiredDataTypesList", index);
    };

    return (
        <Modal
            opened
            centered
            onClose={props.onClose}
            title="Create Analytics"
            size="xl"
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack gap="lg">
                    {/* Basic Info Section */}
                    <Stack gap="md">
                        <TextInput
                            label="Analytic Name"
                            placeholder="Enter analytic name"
                            required
                            maxLength={100}
                            {...form.getInputProps("name")}
                        />
                        <TextInput
                            label="Description"
                            placeholder="Enter analytic description"
                            maxLength={500}
                            {...form.getInputProps("description")}
                        />
                        <Select
                            allowDeselect={false}
                            label="Code"
                            placeholder="Select code"
                            data={analyticCodeOptions}
                            required
                            {...form.getInputProps("code")}
                        />
                        <Select
                            label="Result Type"
                            placeholder="Select result type"
                            data={analyticResultTypeOptions}
                            allowDeselect={false}
                            required
                            {...form.getInputProps("resultType")}
                            onChange={(value) => {
                                form.setFieldValue("resultType", value || "");
                                form.setFieldValue(
                                    "analyticRequiredDataTypesList",
                                    []
                                );
                            }}
                        />
                        <Select
                            label="Analytic Type"
                            placeholder="Select analytic type"
                            data={analyticTypeOptions}
                            allowDeselect={false}
                            {...form.getInputProps("analyticTypeId")}
                        />
                    </Stack>

                    {/* Analytics Configurations Section */}
                    {form.values.resultType && (
                        <Stack gap="md">
                            <Group justify="space-between" align="center">
                                <Text fw={500} size="md">
                                    Analytic Configurations
                                    {form.values.analyticRequiredDataTypesList
                                        .length > 0 && (
                                        <Text span c="dimmed" size="sm" ml="xs">
                                            (
                                            {
                                                form.values
                                                    .analyticRequiredDataTypesList
                                                    .length
                                            }
                                            /{MAX_ANALYTICS})
                                        </Text>
                                    )}
                                </Text>
                                <Button
                                    variant="outline"
                                    leftSection={<FiPlus size={14} />}
                                    onClick={addAnalytic}
                                    size="sm"
                                    disabled={!canAddAnalytic}
                                >
                                    Add Configuration
                                </Button>
                            </Group>

                            {form.errors.analyticRequiredDataTypesList && (
                                <Text c="red" size="sm">
                                    {form.errors.analyticRequiredDataTypesList}
                                </Text>
                            )}

                            {form.values.analyticRequiredDataTypesList
                                .length === 0 ? (
                                <Paper p="md" withBorder>
                                    <Text c="dimmed" ta="center" size="sm">
                                        No configurations added yet. Click "Add
                                        Configuration" to start.
                                    </Text>
                                </Paper>
                            ) : (
                                <Stack gap="md">
                                    {form.values.analyticRequiredDataTypesList.map(
                                        (_, analyticIndex) => (
                                            <Paper
                                                key={analyticIndex}
                                                p="md"
                                                withBorder
                                            >
                                                <Group
                                                    justify="space-between"
                                                    align="flex-start"
                                                >
                                                    {form.values.resultType ===
                                                    "SingleValue" ? (
                                                        <Select
                                                            allowDeselect={
                                                                false
                                                            }
                                                            label="Value Type"
                                                            placeholder="Select type"
                                                            data={fieldTypes}
                                                            style={{
                                                                flex: 1,
                                                            }}
                                                            required
                                                            {...form.getInputProps(
                                                                `analyticRequiredDataTypesList.${analyticIndex}.0.type`
                                                            )}
                                                        />
                                                    ) : (
                                                        <Group
                                                            style={{
                                                                flex: 1,
                                                            }}
                                                            gap="md"
                                                        >
                                                            <Select
                                                                allowDeselect={
                                                                    false
                                                                }
                                                                label="X-axis Type"
                                                                placeholder="Select type"
                                                                data={
                                                                    fieldTypes
                                                                }
                                                                style={{
                                                                    flex: 1,
                                                                }}
                                                                required
                                                                {...form.getInputProps(
                                                                    `analyticRequiredDataTypesList.${analyticIndex}.0.type`
                                                                )}
                                                            />
                                                            <Select
                                                                allowDeselect={
                                                                    false
                                                                }
                                                                label="Y-axis Type"
                                                                placeholder="Select type"
                                                                data={
                                                                    fieldTypes
                                                                }
                                                                style={{
                                                                    flex: 1,
                                                                }}
                                                                required
                                                                {...form.getInputProps(
                                                                    `analyticRequiredDataTypesList.${analyticIndex}.1.type`
                                                                )}
                                                            />
                                                        </Group>
                                                    )}
                                                    <ActionIcon
                                                        color="red"
                                                        variant="outline"
                                                        onClick={() =>
                                                            removeAnalytic(
                                                                analyticIndex
                                                            )
                                                        }
                                                        aria-label="Remove configuration"
                                                        size="lg"
                                                        mt={24}
                                                    >
                                                        <MdDelete size={18} />
                                                    </ActionIcon>
                                                </Group>
                                            </Paper>
                                        )
                                    )}
                                </Stack>
                            )}
                        </Stack>
                    )}

                    {/* Submit Section */}
                    <Stack>
                        <Button type="submit" size="md">
                            Create Analytics
                        </Button>
                    </Stack>
                </Stack>
            </form>
        </Modal>
    );
}
