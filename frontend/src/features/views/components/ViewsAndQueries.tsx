import { SegmentedControl } from "@mantine/core";
import Queries from "../../queries/components/Queries";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import Views from "./Views";

interface Props {
    tracker: TrackerDto;
    activeSubTab: string;
    onSubTabChange: (value: string) => void;
}

// Views and the queries they are built from live under one tab: a query only ever exists
// to be combined into a view, so it does not earn a tab of its own next to Entries. The
// switch is handed to whichever list is showing so it shares that list's action row rather
// than spending a second row of its own.
export default function ViewsAndQueries(props: Props) {
    const subTabs = (
        <SegmentedControl
            color={props.tracker.color}
            radius="md"
            data={[
                { value: "views", label: "Views" },
                { value: "queries", label: "Queries" },
            ]}
            value={props.activeSubTab}
            onChange={props.onSubTabChange}
        />
    );

    return props.activeSubTab === "queries" ? (
        <Queries tracker={props.tracker} headerSection={subTabs} />
    ) : (
        <Views tracker={props.tracker} headerSection={subTabs} />
    );
}
