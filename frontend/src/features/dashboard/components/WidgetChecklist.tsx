import { Checkbox, Group, ScrollArea, Stack, Text, ThemeIcon } from "@mantine/core";
import { DashboardWidgetDto } from "../types/DashboardDto";
import { widgetIcon, widgetLabel } from "./widgetLabel";

interface Props {
    widgets: DashboardWidgetDto[];
    color: string;
    selected: Set<string>;
    onToggle: (id: string) => void;
}

/** A checkbox-per-row picker of board widgets, shared by the group-creation and
    edit-group dialogs. */
export function WidgetChecklist({ widgets, color, selected, onToggle }: Props) {
    return (
        <ScrollArea.Autosize mah={360} type="auto">
            <Stack gap={4}>
                {widgets.map((widget) => {
                    const Icon = widgetIcon(widget);
                    return (
                        <Checkbox.Card
                            key={widget.id}
                            checked={selected.has(widget.id)}
                            onClick={() => onToggle(widget.id)}
                            radius="md"
                            p="xs"
                        >
                            <Group gap="sm" wrap="nowrap">
                                <Checkbox.Indicator />
                                <ThemeIcon variant="light" color={color} radius="md">
                                    <Icon size={16} />
                                </ThemeIcon>
                                <Text size="sm" fw={500} lineClamp={1}>
                                    {widgetLabel(widget)}
                                </Text>
                            </Group>
                        </Checkbox.Card>
                    );
                })}
            </Stack>
        </ScrollArea.Autosize>
    );
}
