export interface UserDto {
    id: string;
    userName: string;
    email?: string;
    roles: string[];
    mailConfirmed: boolean;
}
