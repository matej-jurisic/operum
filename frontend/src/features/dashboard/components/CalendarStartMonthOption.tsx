import { Select } from "@mantine/core";
import { CalendarStartMonths } from "../../analytics/types/AnalyticDto";

const AUTOMATIC = "Automatic";

const options = [
    { value: AUTOMATIC, label: "Automatic" },
    { value: CalendarStartMonths.Current, label: "Current month" },
    { value: CalendarStartMonths.LatestPast, label: "Most recent entry" },
    { value: CalendarStartMonths.EarliestPast, label: "Oldest entry" },
    { value: CalendarStartMonths.NextUpcoming, label: "Next upcoming entry" },
    { value: CalendarStartMonths.LatestUpcoming, label: "Furthest upcoming entry" },
];

interface Props {
    value: string | null;
    onChange: (value: string | null) => void;
}

export function CalendarStartMonthOption({ value, onChange }: Props) {
    return (
        <Select
            label="Open on"
            description={
                value
                    ? undefined
                    : "The current month if it has entries, otherwise the next upcoming or most recent one."
            }
            data={options}
            value={value ?? AUTOMATIC}
            onChange={(selected) =>
                onChange(!selected || selected === AUTOMATIC ? null : selected)
            }
            allowDeselect={false}
        />
    );
}
