import { Select } from "@mantine/core";
import { useEffect, useMemo } from "react";
import { useTracker } from "../context/TrackerContext";
import { SelectData } from "../model/common/SelectData";
import { ViewDto } from "../model/ViewDto";

export default function SelectView() {
    const { views, selectedViewId, setSelectedViewId, refreshViewsIfDirty } =
        useTracker();

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
            label="View"
            maw={130}
            clearable
            allowDeselect={false}
            data={selectViewList}
            onChange={(value) => setSelectedViewId(value ?? undefined)}
            value={selectedViewId ?? null}
        />
    );
}
