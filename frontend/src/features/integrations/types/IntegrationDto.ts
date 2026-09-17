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
    /** True when the provider mints the webhook secret itself (Firefly III), not Operum. */
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
    /** Only present on the response that created the target or issued a new Operum secret; never shown again after. */
    webhookSecret?: string | null;
    /** Null for a pull target. */
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
