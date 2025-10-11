export interface ApiResponse<T = object> {
    data: T;
    isSuccess: boolean;
    messages: string[];
}
