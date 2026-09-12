import { areIntegrationsEnabled } from "../../integrations/config/integrationsFeature";
import { areNotificationsEnabled } from "../../notifications/config/notificationsFeature";

export interface HomeSectionEntry {
    id: string;
    /** The navbar link's text; a section without one isn't linked. */
    navLabel?: string;
}

// The homepage's sections below the hero, in page order, minus those for features this
// build hides. The page body and the navbar's links are both built from this list, so
// they cannot drift apart.
export const HOME_SECTIONS: HomeSectionEntry[] = [
    { id: "trackers", navLabel: "Trackers" },
    { id: "views", navLabel: "Views" },
    { id: "widgets", navLabel: "Widgets" },
    ...(areIntegrationsEnabled
        ? [{ id: "integrations", navLabel: "Integrations" }]
        : []),
    ...(areNotificationsEnabled
        ? [{ id: "notifications", navLabel: "Notifications" }]
        : []),
    { id: "collaboration", navLabel: "Collaboration" },
    { id: "more" },
];
