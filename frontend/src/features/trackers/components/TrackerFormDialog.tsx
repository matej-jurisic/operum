import {
    Button,
    Group,
    Modal,
    Select,
    SelectProps,
    Stack,
    Text,
    Textarea,
    TextInput,
    useMantineTheme,
} from "@mantine/core";
import { useForm } from "@mantine/form";
import { useEffect, useMemo, useState } from "react";
import { FaCheck, FaCircle } from "react-icons/fa";
import { PublicityEnum } from "../../../shared/enums/PublicityEnum";
import { trackersController } from "../api/trackersController";
import { CreateTrackerDto } from "../types/requests/CreateTrackerDto";
import { UpdateTrackerDto } from "../types/requests/UpdateTrackerDto";
import { TrackerDto } from "../types/TrackerDto";

export interface TrackerFormDialogProps {
    onClose: () => void;
    onConfirm?: () => void;
    trackerId?: string;
    initialValues?: UpdateTrackerDto | CreateTrackerDto;
    asTemplate?: boolean;
    withTemplate?: boolean;
}

interface TemplateItem {
    value: string;
    label: string;
    description?: string;
}

const colorOptions = [
    { value: "indigo", label: "Indigo" },
    { value: "blue", label: "Blue" },
    { value: "cyan", label: "Cyan" },
    // { value: "dark", label: "Dark" },
    { value: "grape", label: "Grape" },
    // { value: "gray", label: "Gray" },
    { value: "green", label: "Green" },
    { value: "lime", label: "Lime" },
    { value: "orange", label: "Orange" },
    { value: "pink", label: "Pink" },
    { value: "red", label: "Red" },
    { value: "teal", label: "Teal" },
    { value: "yellow", label: "Yellow" },
    { value: "violet", label: "Violet" },
];

const trackerTypeOptions = [
    { value: PublicityEnum.Draft.toString(), label: "Template Draft" },
    { value: PublicityEnum.Public.toString(), label: "Public Template" },
];

const renderColorOption = (
    theme: ReturnType<typeof useMantineTheme>
): SelectProps["renderOption"] => {
    return ({ option, checked }) => (
        <Group wrap="nowrap" gap="sm">
            <FaCircle
                size={16}
                color={theme.colors[option.value]?.[6] ?? option.value}
            />
            <Text>{option.label}</Text>
            {checked && <FaCheck color="gray" />}
        </Group>
    );
};

const renderTemplateOption: SelectProps["renderOption"] = ({
    option,
    checked,
}) => {
    const o = option as TemplateItem;
    return (
        <Group gap="xs" wrap="nowrap">
            <Stack gap={0} flex={1}>
                <Text>{o.label}</Text>
                {o.description && (
                    <Text size="xs" c="dimmed">
                        {o.description}
                    </Text>
                )}
            </Stack>
            {checked && <FaCheck color="gray" />}
        </Group>
    );
};

export default function TrackerFormDialog(props: TrackerFormDialogProps) {
    const theme = useMantineTheme();
    const [templateList, setTemplateList] = useState<TrackerDto[]>([]);

    // pick the right labels based on template/tracker
    const entityName = props.asTemplate ? "Template" : "Tracker";

    const form = useForm<UpdateTrackerDto & CreateTrackerDto>({
        initialValues: props.initialValues || {
            name: "",
            description: "",
            color: "indigo",
            trackerTypeId: props.asTemplate
                ? PublicityEnum.Draft.toString()
                : undefined,
            templateTrackerId: undefined,
        },
        validate: {
            name: (value) =>
                value.trim().length === 0
                    ? `${entityName} name is required`
                    : value.length > 30
                    ? `${entityName} name must be shorter than 30 characters`
                    : null,
            description: (value) =>
                value && value.length > 500
                    ? `${entityName} description must be at most 500 characters`
                    : null,
            templateTrackerId: (value) =>
                props.withTemplate && !value ? "Template is required" : null,
        },
    });

    const handleSubmit = async (
        values: UpdateTrackerDto & CreateTrackerDto
    ) => {
        if (props.trackerId) {
            await trackersController.updateTracker(props.trackerId, values);
        } else {
            await trackersController.createTracker(values);
        }
        props.onClose();
        props.onConfirm?.();
        form.reset();
    };

    useEffect(() => {
        const GetData = async () => {
            const response = await trackersController.getPublicTemplates();
            setTemplateList(response.data);
        };

        if (props.withTemplate) GetData();
    }, []);

    const trackerOptions: TemplateItem[] = useMemo(() => {
        return templateList.map((t) => ({
            label: t.name,
            value: t.id.toString(),
            description: t.description,
        }));
    }, [templateList]);

    return (
        <Modal
            opened
            onClose={props.onClose}
            title={
                props.trackerId
                    ? `Update ${entityName}`
                    : `Create ${entityName}`
            }
            centered
        >
            <form onSubmit={form.onSubmit(handleSubmit)}>
                <Stack>
                    {props.withTemplate && !props.trackerId && (
                        <Select
                            label={`From Template`}
                            placeholder={"Select template"}
                            data={trackerOptions}
                            renderOption={renderTemplateOption}
                            {...form.getInputProps("templateTrackerId")}
                            value={form.values.templateTrackerId}
                        />
                    )}
                    <TextInput
                        label={`${entityName} Name`}
                        placeholder={`Enter ${entityName.toLowerCase()} name`}
                        maxLength={30}
                        {...form.getInputProps("name")}
                    />
                    <Textarea
                        label={`${entityName} Description`}
                        autosize
                        placeholder={`Enter ${entityName.toLowerCase()} description`}
                        maxLength={500}
                        {...form.getInputProps("description")}
                    />
                    <Select
                        label={`${entityName} Color`}
                        placeholder={`Select ${entityName.toLowerCase()} color`}
                        data={colorOptions}
                        allowDeselect={false}
                        renderOption={renderColorOption(theme)}
                        {...form.getInputProps("color")}
                    />
                    {props.asTemplate && props.trackerId && (
                        <Select
                            label={`${entityName} Type`}
                            placeholder={`Select ${entityName.toLowerCase()} type`}
                            data={trackerTypeOptions}
                            allowDeselect={false}
                            {...form.getInputProps("trackerTypeId")}
                            value={form.values.trackerTypeId?.toString()}
                        />
                    )}
                    <Button type="submit">
                        {props.trackerId
                            ? `Update ${entityName}`
                            : `Create ${entityName}`}
                    </Button>
                </Stack>
            </form>
        </Modal>
    );
}
