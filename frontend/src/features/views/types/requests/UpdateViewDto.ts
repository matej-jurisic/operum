import { CreateViewFilterDto, CreateViewSortDto } from "./CreateViewDto";

export interface UpdateViewDto {
    name: string;
    description?: string;
    sorts: CreateViewSortDto[];
    filters: CreateViewFilterDto[];
}
