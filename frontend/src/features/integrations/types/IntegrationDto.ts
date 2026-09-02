import { FieldType } from "../../../shared/constants/DataTypes";

/** One value a provider can offer for mapping onto a tracker field. */
export interface SourceFieldDto {
    key: string;
    type: FieldType;
    label: string;
    description?: string | null;
}

export interface ProviderResourceDto {
    resourceType: string;
    fields: SourceFieldDto[];
}

export interface ProviderDto {
    key: string;
    displayName: string;
    supportsPull: boolean;
    supportsPush: boolean;
    /** Whether the connect form must ask for the user's own instance URL. */
    requiresBaseUrl: boolean;
    /**
     * True when this push provider mints the webhook secret itself and the user pastes it
     * into Operum (Firefly III). False when Operum generates it for the user to paste into
     * the provider.
     */
    providerSuppliesSecret: boolean;
    resources: ProviderResourceDto[];
}

export interface FieldMappingDto {
    sourceKey: string;
    fieldId: string;
    skipWhenNull: boolean;
}

export type SyncStatus = "Never" | "Ok" | "Error";
export type IntegrationMode = "Pull" | "Push";

export interface IntegrationTargetDto {
    id: string;
    trackerId: string;
    trackerName: string;
    resourceType: string;
    mode: IntegrationMode;
    isEnabled: boolean;
    backfillFrom: string;
    lastSyncedAt?: string | null;
    lastSyncStatus: SyncStatus;
    lastSyncError?: string | null;
    /** Where a push provider delivers. Null for a pull target. */
    webhookUrl?: string | null;
    /**
     * Only ever present on the response that created the target or issued a new Operum
     * secret. It is stored encrypted and cannot be shown again, so the UI has to make the
     * user copy it there and then. Always null for a provider that supplies its own secret.
     */
    webhookSecret?: string | null;
    /**
     * Whether a signing secret is set on this push target. False on a Firefly target
     * between creating it and pasting in the secret from Firefly. Null for a pull target.
     */
    hasWebhookSecret?: boolean | null;
    mappings: FieldMappingDto[];
}

export interface IntegrationDto {
    id: string;
    provider: string;
    externalAccountId?: string | null;
    baseUrl?: string | null;
    /** A suffix such as "…a91f". The credential itself never leaves the server. */
    maskedCredential: string;
    isEnabled: boolean;
    createdAt: string;
    targets: IntegrationTargetDto[];
}

export interface SyncResultDto {
    created: number;
    updated: number;
    deleted: number;
    skipped: number;
    errorCount: number;
    errors: string[];
}
