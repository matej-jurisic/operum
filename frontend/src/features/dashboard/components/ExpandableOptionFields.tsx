import { Checkbox, Stack } from "@mantine/core";

interface Props {
    expandable: boolean;
    mobileExpandable: boolean;
    onExpandableChange: (value: boolean) => void;
    onMobileExpandableChange: (value: boolean) => void;
}

/**
 * The two checkboxes every Analytic/Entries widget's create/edit form offers: whether the
 * widget collapses to a button that opens the real thing in a modal, set independently for
 * the wide grid and the narrow one a phone renders. Shared rather than repeated across
 * CustomAnalyticForm, ExistingAnalyticForm, EntriesWidgetForm and EditWidgetModal, which
 * otherwise differ in everything else about how the widget is built.
 */
export function ExpandableOptionFields({
    expandable,
    mobileExpandable,
    onExpandableChange,
    onMobileExpandableChange,
}: Props) {
    return (
        <Stack gap="xs">
            <Checkbox
                label="Expandable on desktop"
                description="Show as a small button that opens the full widget in a popup"
                checked={expandable}
                onChange={(event) => onExpandableChange(event.currentTarget.checked)}
            />
            <Checkbox
                label="Expandable on mobile"
                description="Show as a small button that opens the full widget in a popup"
                checked={mobileExpandable}
                onChange={(event) => onMobileExpandableChange(event.currentTarget.checked)}
            />
        </Stack>
    );
}
