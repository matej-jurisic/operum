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
}
