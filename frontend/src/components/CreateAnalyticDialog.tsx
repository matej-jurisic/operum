import {
    Box,
    Button,
    Group,
    Modal,
    Select,
    Stack,
    Text,
    TextInput,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import api from "../api/api";
import {
    dataPurposes,
    fieldTypes,
} from "../model/constants/DataTypesForSelect";
import { CreateAnalyticDto } from "../model/requests/CreateAnalyticDto";

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
            analyticRequiredDataTypes: [],
        },

        validate: {
            name: (value) =>
                value.trim().length === 0 ? "Field name is required" : null,
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

    const dataTypeFields = form.values.analyticRequiredDataTypes.map(
        (_, index) => (
            <Group key={index} mt="xs">
                <Select
                    allowDeselect={false}
                    label="Type"
                    data={fieldTypes}
                    flex={1}
                    required
                    {...form.getInputProps(
                        `analyticRequiredDataTypes.${index}.type`
                    )}
                />
                <Select
                    allowDeselect={false}
                    label="Purpose"
                    data={dataPurposes}
                    flex={1}
                    required
                    {...form.getInputProps(
                        `analyticRequiredDataTypes.${index}.purpose`
                    )}
                />
                <Button
                    color="red"
                    variant="outline"
                    onClick={() =>
                        form.removeListItem("analyticRequiredDataTypes", index)
                    }
                    style={{ alignSelf: "flex-end" }}
                >
                    <MdDelete size={18} />
                </Button>
            </Group>
        )
    );

    const isLimitReached =
        form.values.analyticRequiredDataTypes.length >= MAX_DATA_TYPES;

    return (
        <Modal opened onClose={props.onClose} title="Add Analytic" centered>
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack align="stretch">
                    <TextInput
                        label="Name"
                        placeholder="Enter analytic name"
                        {...form.getInputProps("name")}
                    />

                    <TextInput
                        label="Description"
                        placeholder="Enter analytic description"
                        {...form.getInputProps("description")}
                    />

                    <Box>
                        <Group justify="space-between" mb="xs">
                            {!isLimitReached && (
                                <Button
                                    color="gray"
                                    onClick={() =>
                                        form.insertListItem(
                                            "analyticRequiredDataTypes",
                                            { type: "", purpose: "" }
                                        )
                                    }
                                    leftSection={<FiPlus size={16} />}
                                    variant="outline"
                                >
                                    Add new
                                </Button>
                            )}
                        </Group>
                        {dataTypeFields.length > 0 ? (
                            dataTypeFields
                        ) : (
                            <Text c="dimmed" size="sm">
                                No required data types added yet.
                            </Text>
                        )}
                    </Box>

                    <Button color={"indigo"} type="submit">
                        Create
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
