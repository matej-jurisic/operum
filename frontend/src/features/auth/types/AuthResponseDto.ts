export interface AuthResponseDto {
    id: string;
    userName: string;
    email?: string;
    tokenExpiry?: Date;
    roles: string[];
    timeZone?: string | null;
}
