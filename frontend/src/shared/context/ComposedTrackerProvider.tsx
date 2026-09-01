import React from "react";
import { ConstantsProvider } from "../../features/constants/context/ConstantsContext";
import { EntriesProvider } from "../../features/entries/context/EntriesContext";
import { FieldsProvider } from "../../features/fields/context/FieldsContext";
import { NotificationsProvider } from "../../features/notifications/context/NotificationsContext";
import { TrackerProvider } from "../../features/trackers/context/TrackerContext";
import { TrackerDto } from "../../features/trackers/types/TrackerDto";
import { ViewsProvider } from "../../features/views/context/ViewsContext";

export const ComposedTrackerProvider: React.FC<{
    initialTracker: TrackerDto;
    children: React.ReactNode;
}> = ({ initialTracker, children }) => {
    return (
        <TrackerProvider initialTracker={initialTracker}>
            <FieldsProvider>
                <ConstantsProvider>
                    <EntriesProvider>
                        <ViewsProvider>
                            <NotificationsProvider>{children}</NotificationsProvider>
                        </ViewsProvider>
                    </EntriesProvider>
                </ConstantsProvider>
            </FieldsProvider>
        </TrackerProvider>
    );
};
