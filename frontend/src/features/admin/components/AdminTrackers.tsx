import { ScrollArea, Stack } from "@mantine/core";
import { useEffect, useState } from "react";
import EmptyState from "../../../shared/components/EmptyState";
import { adminController } from "../api/adminController";
import { AdminTrackerDto } from "../types/AdminTrackerDto";
import AdminTrackerCard from "./AdminTrackerCard";

export default function AdminTrackers() {
    const [trackers, setTrackers] = useState<AdminTrackerDto[]>([]);
    const [loaded, setLoaded] = useState(false);

    useEffect(() => {
        const load = async () => {
            const response = await adminController.getAllTrackers();
            setTrackers(response.data);
            setLoaded(true);
        };
        load();
    }, []);

    return (
        // The global request loader already covers the first fetch, so this stays empty
        // until the trackers arrive.
        <ScrollArea h="100%">
            {!loaded ? null : trackers.length === 0 ? (
                <EmptyState
                    title="No trackers yet"
                    hint="Trackers created by any user will appear here."
                />
            ) : (
                <Stack gap="md" pb="md">
                    {trackers.map((tracker) => (
                        <AdminTrackerCard key={tracker.id} tracker={tracker} />
                    ))}
                </Stack>
            )}
        </ScrollArea>
    );
}
