import { ActionIcon, Group, Stack, Text, Tooltip } from "@mantine/core";
import { createElement } from "react";
import { MdClose } from "react-icons/md";
import { CURATED_ICONS } from "../../../shared/constants/TrackerIcons";

interface Props {
    value?: string;
    onChange: (icon: string | undefined) => void;
    color?: string;
}

export default function IconPicker({ value, onChange, color = "indigo" }: Props) {
    return (
        <Stack gap="xs">
            <Text size="sm" fw={500}>Icon <Text span size="xs" c="dimmed">(optional)</Text></Text>
            <Group gap={4} wrap="wrap">
                <Tooltip label="No icon" withArrow>
                    <ActionIcon
                        size={34}
                        radius="md"
                        variant={!value ? "filled" : "light"}
                        color={!value ? color : "gray"}
                        onClick={() => onChange(undefined)}
                    >
                        <MdClose size={16} />
                    </ActionIcon>
                </Tooltip>
                {CURATED_ICONS.map((def) => (
                    <Tooltip key={def.name} label={def.label} withArrow>
                        <ActionIcon
                            size={34}
                            radius="md"
                            variant={value === def.name ? "filled" : "light"}
                            color={value === def.name ? color : "gray"}
                            onClick={() => onChange(def.name)}
                        >
                            {createElement(def.icon, { size: 18 })}
                        </ActionIcon>
                    </Tooltip>
                ))}
            </Group>
        </Stack>
    );
}
