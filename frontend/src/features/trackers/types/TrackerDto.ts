import { FieldDto } from "../../fields/types/FieldDto";

export interface TrackerDto {
    id: string;
    name: string;
    description?: string;
    color?: string;
    icon?: string;
    fields: FieldDto[];
    trackerTypeId?: number;
    ownerId?: string;
    ownerName?: string;
    defaultViewIds?: string[];
    trackerTypeName?: string;
    currentUserCanEditData: boolean;
    currentUserCanEditSchema: boolean;
}
