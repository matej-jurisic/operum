import {
    ActionIcon,
    Button,
    Group,
    Paper,
    SegmentedControl,
    Select,
    Stack,
    Text,
} from "@mantine/core";
import { UseFormReturnType } from "@mantine/form";
import { FiPlus } from "react-icons/fi";
import { MdDelete } from "react-icons/md";
import { FieldDto } from "../../fields/types/FieldDto";
import { getPathValue } from "../../../shared/utils/getPathValue";

interface Props {
    fields: FieldDto[];
    form: UseFormReturnType<any>;
    sortsPath?: string;
    color?: string;
    maxSorts?: number;
}

export default function SortListEditor({
    fields,
    form,
    sortsPath = "sorts",
    color,
    maxSorts = 3,
}: Props) {
    const sorts: Array<{ fieldId: string; descending: boolean }> =
        getPathValue(form.values, sortsPath) ?? [];
    const canAdd = sorts.length < maxSorts;

    const fieldOptions = fields.map((field) => ({
        value: field.id,
        label: field.name,
    }));

    const addSort = () => {
        if (!canAdd) return;
        form.insertListItem(sortsPath, { fieldId: "", descending: false });
    };

    return (
        <Stack gap="md">
            <Group justify="space-between" align="center">
                <Text fw={500} size="md">
                    Sorting Rules
                    {sorts.length > 0 && (
                        <Text span c="dimmed" size="sm" ml="xs">
                            ({sorts.length}/{maxSorts})
                        </Text>
                    )}
                </Text>
                <Button
                    color={color}
                    variant="outline"
                    leftSection={<FiPlus size={14} />}
                    onClick={addSort}
                    size="sm"
                    disabled={!canAdd}
                >
                    Add
                </Button>
            </Group>

            {sorts.length === 0 ? (
                <Paper p="md" withBorder>
                    <Text c="dimmed" ta="center" size="sm">
                        No sorting rules added yet
                    </Text>
                </Paper>
            ) : (
                <Stack gap="sm">
                    {sorts.map((sort, index) => (
                        <Paper key={index} p="md" withBorder>
                            <Group gap="md" wrap="nowrap">
                                <Select
                                    placeholder="Select field"
                                    allowDeselect={false}
                                    data={fieldOptions}
                                    {...form.getInputProps(
                                        `${sortsPath}.${index}.fieldId`,
                                    )}
                                    flex={1}
                                />

                                <SegmentedControl
                                    data={[
                                        { value: "asc", label: "Asc" },
                                        { value: "desc", label: "Desc" },
                                    ]}
                                    value={sort.descending ? "desc" : "asc"}
                                    onChange={(value) =>
                                        form.setFieldValue(
                                            `${sortsPath}.${index}.descending`,
                                            value === "desc",
                                        )
                                    }
                                />

                                <ActionIcon
                                    color="red"
                                    variant="outline"
                                    onClick={() =>
                                        form.removeListItem(sortsPath, index)
                                    }
                                    aria-label="Remove sort"
                                    size="lg"
                                >
                                    <MdDelete size={18} />
                                </ActionIcon>
                            </Group>
                        </Paper>
                    ))}
                </Stack>
            )}
        </Stack>
    );
}
