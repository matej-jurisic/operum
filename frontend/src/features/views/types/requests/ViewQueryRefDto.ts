import { CreateQueryDto } from "../../../queries/types/requests/CreateQueryDto";

/**
 * One entry in a View's ordered Query list: either a reference to an existing, reusable
 * Query, or a brand-new one authored inline while building the View (still saved as a
 * real, independently reusable Query). Exactly one of the two must be set.
 */
export interface ViewQueryRefDto {
    queryId?: string;
    newQuery?: CreateQueryDto;
}
