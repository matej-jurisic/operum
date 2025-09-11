import { Select } from "@mantine/core";
import { useEffect, useMemo } from "react";
import { FaFilter } from "react-icons/fa";
import { useTracker } from "../context/TrackerContext";
import { SelectData } from "../model/common/SelectData";
import { ViewDto } from "../model/ViewDto";

interface Props {
    color?: string;
}

export default function SelectView(props: Props) {
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
            maw={130}
            clearable
            leftSection={<FaFilter color={props.color} size={16} />}
            leftSectionPointerEvents="none"
            allowDeselect={false}
            data={selectViewList}
            onChange={(value) => setSelectedViewId(value ?? undefined)}
            value={selectedViewId ?? null}
        />
    );
}
