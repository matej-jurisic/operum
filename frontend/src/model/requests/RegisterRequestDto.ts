export interface RegisterRequestDto {
    email: string;
    userName: string;
    password: string;
    confirmPassword?: string;
}
