export interface AuhtResponseDto {
    id: string;
    userName: string;
    email?: string;
    tokenExpiry?: Date;
    roles: string[];
}
