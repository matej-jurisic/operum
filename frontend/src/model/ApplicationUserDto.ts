export interface ApplicationUserDto {
    id: string;
    userName: string;
    email?: string;
    tokenExpiry?: Date;
    roles: string[];
}
