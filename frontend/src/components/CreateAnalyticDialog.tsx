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

interface CreateAnalyticDialogProps {
    onClose: () => void;
    onFieldAdded?: () => void;
}

const MAX_DATA_TYPES = 5;

export function CreateAnalyticDialog(props: CreateAnalyticDialogProps) {
    const form = useForm<CreateAnalyticDto>({
        initialValues: {
            name: "",
            description: "",
            resultType: "",
            analyticRequiredDataTypes: [],
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
            analyticRequiredDataTypes: {
                type: (value) =>
                    value.trim().length === 0 ? "Type is required" : null,
                purpose: (value) =>
                    value.trim().length === 0 ? "Purpose is required" : null,
            },
        },
    });

    const handleSubmit = async (values: CreateAnalyticDto) => {
        await api.post(`/analytics`, values);
        props.onFieldAdded?.();
        form.reset();
        props.onClose();
    };

    const canAddDataType =
        form.values.analyticRequiredDataTypes.length < MAX_DATA_TYPES;

    const addDataType = () => {
        if (!canAddDataType) return;

        form.insertListItem("analyticRequiredDataTypes", {
            type: "",
            purpose: "",
        });
    };

    const removeDataType = (index: number) => {
        form.removeListItem("analyticRequiredDataTypes", index);
    };

    return (
        <Modal
            opened
            centered
            onClose={props.onClose}
            title="Create Analytic"
            size="lg"
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
                        <TextInput
                            label="Code"
                            placeholder="Enter analytic code"
                            required
                            maxLength={100}
                            {...form.getInputProps("code")}
                        />
                        <Select
                            label={`Result Type`}
                            placeholder={`Select result type`}
                            data={analyticResultTypeOptions}
                            allowDeselect={false}
                            {...form.getInputProps("resultType")}
                            value={form.values.resultType}
                        />
                        <Select
                            label={`Analytic Type`}
                            placeholder={`Select analytic type`}
                            data={analyticTypeOptions}
                            allowDeselect={false}
                            {...form.getInputProps("analyticTypeId")}
                            value={form.values.analyticTypeId?.toString()}
                        />
                    </Stack>

                    {/* Required Data Types Section */}
                    <Stack gap="md">
                        <Group justify="space-between" align="center">
                            <Text fw={500} size="md">
                                Required Data Types
                                {form.values.analyticRequiredDataTypes.length >
                                    0 && (
                                    <Text span c="dimmed" size="sm" ml="xs">
                                        (
                                        {
                                            form.values
                                                .analyticRequiredDataTypes
                                                .length
                                        }
                                        /{MAX_DATA_TYPES})
                                    </Text>
                                )}
                            </Text>
                            <Button
                                color="indigo"
                                variant="outline"
                                leftSection={<FiPlus size={14} />}
                                onClick={addDataType}
                                size="sm"
                                disabled={!canAddDataType}
                            >
                                Add
                            </Button>
                        </Group>

                        {form.values.analyticRequiredDataTypes.length === 0 ? (
                            <Paper p="md" withBorder>
                                <Text c="dimmed" ta="center" size="sm">
                                    No required data types added yet
                                </Text>
                            </Paper>
                        ) : (
                            <Stack gap="sm">
                                {form.values.analyticRequiredDataTypes.map(
                                    (_, index) => (
                                        <Paper key={index} p="md" withBorder>
                                            <Group gap="md" align="flex-end">
                                                <Select
                                                    allowDeselect={false}
                                                    label="Type"
                                                    placeholder="Select type"
                                                    data={fieldTypes}
                                                    flex={1}
                                                    required
                                                    {...form.getInputProps(
                                                        `analyticRequiredDataTypes.${index}.type`
                                                    )}
                                                />
                                                <TextInput
                                                    label="Purpose"
                                                    placeholder="Enter purpose"
                                                    flex={1}
                                                    required
                                                    {...form.getInputProps(
                                                        `analyticRequiredDataTypes.${index}.purpose`
                                                    )}
                                                />

                                                <ActionIcon
                                                    color="red"
                                                    variant="outline"
                                                    onClick={() =>
                                                        removeDataType(index)
                                                    }
                                                    aria-label="Remove data type"
                                                    size="lg"
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

                    {/* Submit Section */}
                    <Stack>
                        <Button color="indigo" type="submit" size="md">
                            Create Analytic
                        </Button>
                    </Stack>
                </Stack>
            </form>
        </Modal>
    );
}
