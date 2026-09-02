import { FieldMappingDto, IntegrationMode } from "../IntegrationDto";

export interface ConnectIntegrationDto {
    provider: string;
    credential?: string;
    baseUrl?: string;
}

export interface SaveIntegrationTargetDto {
    trackerId: string;
    resourceType: string;
    mode?: IntegrationMode;
    isEnabled: boolean;
    /** ISO date. Omitted on update leaves whatever the target already had. */
    backfillFrom?: string;
    mappings: FieldMappingDto[];
    /**
     * For a push provider that mints its own secret (Firefly III): the value copied from the
     * provider. Usually set afterward through setWebhookSecret, since the webhook URL is
     * needed first. Ignored for a provider Operum generates the secret for.
     */
    webhookSecret?: string;
}
