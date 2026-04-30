import {
    ActionIcon,
    Badge,
    Button,
    Card,
    Center,
    Group,
    Loader,
    Modal,
    Select,
    Stack,
    Stepper,
    Text,
    TextInput,
    Textarea,
    ThemeIcon,
    Title,
    UnstyledButton,
    useMantineTheme,
} from "@mantine/core";
import { createElement, useState } from "react";
import { FaCircle } from "react-icons/fa";
import { MdAdd, MdCheck, MdDelete } from "react-icons/md";
import { useNavigate } from "react-router-dom";
import { resolveTrackerIcon } from "../../../shared/constants/TrackerIcons";
import { fieldsController } from "../../fields/api/fieldsController";
import { trackersController } from "../api/trackersController";
import IconPicker from "./IconPicker";

interface Props {
    onClose: () => void;
}

interface WizardField {
    id: string;
    name: string;
    type: string;
}

const COLOR_OPTIONS = [
    "indigo",
    "blue",
    "cyan",
    "grape",
    "green",
    "lime",
    "orange",
    "pink",
    "red",
    "teal",
    "yellow",
    "violet",
];

const QUICK_FIELDS: { label: string; type: string }[] = [
    { label: "Date", type: "date" },
    { label: "Title", type: "string" },
    { label: "Notes", type: "string" },
    { label: "Amount", type: "number" },
    { label: "Rating", type: "number" },
    { label: "Duration", type: "timespan" },
    { label: "Done", type: "bool" },
    { label: "Category", type: "string" },
];

const FIELD_TYPE_OPTIONS = [
    { value: "string", label: "Text" },
    { value: "number", label: "Number" },
    { value: "bool", label: "Yes / No" },
    { value: "date", label: "Date" },
    { value: "datetime", label: "Date & Time" },
    { value: "timespan", label: "Duration" },
];

const FIELD_TYPE_LABEL: Record<string, string> = {
    string: "Text",
    number: "Number",
    bool: "Yes / No",
    date: "Date",
    datetime: "Date & Time",
    timespan: "Duration",
};

let nextId = 0;
const uid = () => String(++nextId);

export default function TrackerWizard({ onClose }: Props) {
    const theme = useMantineTheme();
    const navigate = useNavigate();

    const [step, setStep] = useState(0);
    const [name, setName] = useState("");
    const [color, setColor] = useState("indigo");
    const [icon, setIcon] = useState<string | undefined>();
    const [description, setDescription] = useState("");
    const [fields, setFields] = useState<WizardField[]>([]);
    const [nameError, setNameError] = useState<string | null>(null);
    const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
    const [isCreating, setIsCreating] = useState(false);

    const validateStep1 = () => {
        if (!name.trim()) {
            setNameError("Tracker name is required");
            return false;
        }
        if (name.length > 30) {
            setNameError("Tracker name must be at most 30 characters");
            return false;
        }
        setNameError(null);
        return true;
    };

    const validateStep2 = () => {
        const errors: Record<string, string> = {};
        const seen = new Set<string>();
        for (const f of fields) {
            if (!f.name.trim()) {
                errors[f.id] = "Name required";
            } else if (seen.has(f.name.trim().toLowerCase())) {
                errors[f.id] = "Duplicate name";
            } else {
                seen.add(f.name.trim().toLowerCase());
            }
        }
        setFieldErrors(errors);
        return Object.keys(errors).length === 0;
    };

    const addQuickField = (label: string, type: string) => {
        setFields((prev) => [...prev, { id: uid(), name: label, type }]);
    };

    const updateField = (id: string, patch: Partial<WizardField>) => {
        setFields((prev) =>
            prev.map((f) => (f.id === id ? { ...f, ...patch } : f)),
        );
        if (patch.name !== undefined) {
            setFieldErrors((prev) => {
                const next = { ...prev };
                delete next[id];
                return next;
            });
        }
    };

    const removeField = (id: string) => {
        setFields((prev) => prev.filter((f) => f.id !== id));
        setFieldErrors((prev) => {
            const next = { ...prev };
            delete next[id];
            return next;
        });
    };

    const handleCreate = async () => {
        setIsCreating(true);
        try {
            const trackerRes = await trackersController.createTracker({
                name: name.trim(),
                description: description.trim() || undefined,
                color,
                icon,
            });
            const trackerId = (trackerRes as { data: { id: string } }).data?.id;
            if (!trackerId) return;

            for (const f of fields) {
                await fieldsController.createField(trackerId, {
                    name: f.name.trim(),
                    type: f.type,
                    required: false,
                    isCalculated: false,
                });
            }

            onClose();
            navigate(`/trackers/${trackerId}`);
        } finally {
            setIsCreating(false);
        }
    };

    const goNext = () => {
        if (step === 0 && !validateStep1()) return;
        if (step === 1 && !validateStep2()) return;
        if (step === 2) {
            handleCreate();
            return;
        }
        setStep((s) => s + 1);
    };

    const goBack = () => setStep((s) => s - 1);

    const accentColor = theme.colors[color]?.[6] ?? theme.colors.indigo[6];

    return (
        <Modal
            opened
            onClose={onClose}
            title="Create a new tracker"
            size="lg"
            centered
        >
            <Stack gap="xl">
                <Stepper active={step} color={color} size="sm">
                    <Stepper.Step label="Basics" />
                    <Stepper.Step label="Fields" />
                    <Stepper.Step label="Review" />
                </Stepper>

                {step === 0 && (
                    <Stack gap="md">
                        <TextInput
                            label="Tracker name"
                            placeholder="e.g. Workout log, Monthly budget, Reading list"
                            value={name}
                            onChange={(e) => {
                                setName(e.currentTarget.value);
                                setNameError(null);
                            }}
                            error={nameError}
                            maxLength={30}
                            required
                            autoFocus
                        />
                        <Textarea
                            label="Description"
                            placeholder="What will you track? (optional)"
                            value={description}
                            onChange={(e) =>
                                setDescription(e.currentTarget.value)
                            }
                            maxLength={500}
                            autosize
                            minRows={2}
                        />
                        <Stack gap="xs">
                            <Text size="sm" fw={500}>
                                Color
                            </Text>
                            <Group gap="xs">
                                {COLOR_OPTIONS.map((c) => (
                                    <UnstyledButton
                                        key={c}
                                        onClick={() => setColor(c)}
                                        style={{
                                            borderRadius: "50%",
                                            padding: 2,
                                            border:
                                                color === c
                                                    ? `2px solid ${theme.colors[c]?.[6]}`
                                                    : "2px solid transparent",
                                            lineHeight: 0,
                                        }}
                                    >
                                        <FaCircle
                                            size={22}
                                            color={theme.colors[c]?.[6]}
                                        />
                                    </UnstyledButton>
                                ))}
                            </Group>
                        </Stack>
                        <IconPicker
                            value={icon}
                            onChange={setIcon}
                            color={color}
                        />
                    </Stack>
                )}

                {step === 1 && (
                    <Stack gap="md">
                        <Stack gap="xs">
                            <Text size="sm" fw={500}>
                                Quick add
                            </Text>
                            <Text size="xs" c="dimmed">
                                Click to add common fields to your tracker
                            </Text>
                            <Group gap="xs">
                                {QUICK_FIELDS.map((qf) => (
                                    <Button
                                        key={qf.label}
                                        variant="light"
                                        color={color}
                                        size="xs"
                                        leftSection={<MdAdd size={14} />}
                                        onClick={() =>
                                            addQuickField(qf.label, qf.type)
                                        }
                                    >
                                        {qf.label}
                                    </Button>
                                ))}
                            </Group>
                        </Stack>

                        <Stack gap="xs">
                            <Text size="sm" fw={500}>
                                Your fields{" "}
                                {fields.length > 0 && (
                                    <Text span c="dimmed" size="xs">
                                        ({fields.length})
                                    </Text>
                                )}
                            </Text>

                            {fields.length === 0 ? (
                                <Text size="sm" c="dimmed" py="xs">
                                    No fields yet. Use quick add above or add a
                                    custom field below.
                                </Text>
                            ) : (
                                <Stack gap="xs">
                                    {fields.map((f) => (
                                        <Group
                                            key={f.id}
                                            gap="xs"
                                            wrap="nowrap"
                                        >
                                            <TextInput
                                                placeholder="Field name"
                                                value={f.name}
                                                onChange={(e) =>
                                                    updateField(f.id, {
                                                        name: e.currentTarget
                                                            .value,
                                                    })
                                                }
                                                error={fieldErrors[f.id]}
                                                maxLength={30}
                                                flex={1}
                                                size="sm"
                                            />
                                            <Select
                                                data={FIELD_TYPE_OPTIONS}
                                                value={f.type}
                                                onChange={(v) =>
                                                    v &&
                                                    updateField(f.id, {
                                                        type: v,
                                                    })
                                                }
                                                allowDeselect={false}
                                                w={130}
                                                size="sm"
                                            />
                                            <ActionIcon
                                                variant="subtle"
                                                color="red"
                                                onClick={() =>
                                                    removeField(f.id)
                                                }
                                                mt={fieldErrors[f.id] ? -20 : 0}
                                            >
                                                <MdDelete size={16} />
                                            </ActionIcon>
                                        </Group>
                                    ))}
                                </Stack>
                            )}

                            <Button
                                variant="subtle"
                                color={color}
                                size="xs"
                                leftSection={<MdAdd size={14} />}
                                onClick={() =>
                                    setFields((prev) => [
                                        ...prev,
                                        { id: uid(), name: "", type: "string" },
                                    ])
                                }
                                w="fit-content"
                            >
                                Add custom field
                            </Button>
                        </Stack>
                    </Stack>
                )}

                {step === 2 && (
                    <Stack gap="md">
                        <Text size="sm" c="dimmed">
                            Review your tracker before creating it.
                        </Text>
                        <Card
                            withBorder
                            radius="md"
                            p="md"
                            style={{ borderTop: `3px solid ${accentColor}` }}
                        >
                            <Group gap="md" align="flex-start">
                                <ThemeIcon
                                    size={44}
                                    radius="md"
                                    variant="light"
                                    color={color}
                                    style={{ flexShrink: 0 }}
                                >
                                    {createElement(resolveTrackerIcon(icon), {
                                        size: 22,
                                    })}
                                </ThemeIcon>
                                <Stack gap={4} flex={1}>
                                    <Title order={4}>{name}</Title>
                                    {description && (
                                        <Text size="sm" c="dimmed">
                                            {description}
                                        </Text>
                                    )}
                                    {fields.length > 0 ? (
                                        <Group gap="xs" mt={4} wrap="wrap">
                                            {fields.map((f) => (
                                                <Badge
                                                    key={f.id}
                                                    size="sm"
                                                    variant="light"
                                                    color={color}
                                                >
                                                    {f.name} ·{" "}
                                                    {FIELD_TYPE_LABEL[f.type] ??
                                                        f.type}
                                                </Badge>
                                            ))}
                                        </Group>
                                    ) : (
                                        <Text size="xs" c="dimmed" mt={4}>
                                            No fields — you can add them later
                                        </Text>
                                    )}
                                </Stack>
                            </Group>
                        </Card>

                        {isCreating && (
                            <Center py="xs">
                                <Loader color={color} size="sm" />
                            </Center>
                        )}
                    </Stack>
                )}

                <Group justify="space-between">
                    <Button
                        variant="subtle"
                        onClick={step === 0 ? onClose : goBack}
                        disabled={isCreating}
                    >
                        {step === 0 ? "Cancel" : "Back"}
                    </Button>
                    <Button
                        color={color}
                        onClick={goNext}
                        loading={isCreating}
                        rightSection={
                            step === 2 ? <MdCheck size={18} /> : undefined
                        }
                    >
                        {step === 2 ? "Create Tracker" : "Next"}
                    </Button>
                </Group>
            </Stack>
        </Modal>
    );
}
