import { Select, useMantineTheme } from "@mantine/core";
import { useEffect, useMemo } from "react";
import { FaFilter } from "react-icons/fa";
import { useTracker } from "../context/TrackerContext";
import { SelectData } from "../model/common/SelectData";
import { ViewDto } from "../model/ViewDto";

export default function SelectView() {
    const { views, selectedViewId, setSelectedViewId, refreshViewsIfDirty } =
        useTracker();
    const theme = useMantineTheme();

    useEffect(() => {
        refreshViewsIfDirty();
    }, []);

    const selectViewList = useMemo(() => {
        return [
            ...views.map(
                (v: ViewDto) =>
                    ({
                        value: v.id,
                        label: v.name,
                    } as SelectData)
            ),
        ];
    }, [views]);

    return (
        <Select
            maw={130}
            clearable
            leftSection={<FaFilter color={theme.primaryColor} size={16} />}
            leftSectionPointerEvents="none"
            allowDeselect={false}
            data={selectViewList}
            onChange={(value) => setSelectedViewId(value ?? undefined)}
            value={selectedViewId ?? null}
        />
    );
}
