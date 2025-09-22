import api from "../api/api";
import { AuhtResponseDto } from "../model/AuthResponseDto";
import { GoogleLoginRequestDto } from "../model/GoogleLoginRequestDto";
import { LoginRequestDto } from "../model/requests/LoginRequestDto";
import { RegisterRequestDto } from "../model/requests/RegisterRequestDto";
import globalStore from "../stores/GlobalStore";

const USERNAME_KEY = "username";
const ID_KEY = "id";
const ROLES_KEY = "roles";
const EXP_KEY = "exp";

const useAuth = () => {
    const handleUserLoggedInCheck = async () => {
        globalStore.setCheckingAuth(true);
        const username = localStorage.getItem(USERNAME_KEY);
        const id = localStorage.getItem(ID_KEY);
        const roles = localStorage.getItem(ROLES_KEY);
        const exp = localStorage.getItem(EXP_KEY);

        if (
            username !== null &&
            id !== null &&
            exp !== null &&
            roles !== null
        ) {
            if (Date.now() > parseInt(exp, 10)) {
                await getUser();
            } else {
                globalStore.setCurrentUser({
                    userName: username,
                    id: id,
                    roles: JSON.parse(roles),
                });
            }
        } else {
            globalStore.setCurrentUser(undefined);
        }
        globalStore.setCheckingAuth(false);
    };

    const setUserData = (user: AuhtResponseDto) => {
        globalStore.setCurrentUser({
            userName: user.userName,
            id: user.id,
            roles: user.roles,
        });
        localStorage.setItem(USERNAME_KEY, user.userName);
        localStorage.setItem(ID_KEY, user.id);
        localStorage.setItem(ROLES_KEY, JSON.stringify(user.roles));
        if (user.tokenExpiry) {
            const expiryDate = new Date(user.tokenExpiry);
            localStorage.setItem(
                EXP_KEY,
                (expiryDate.getTime() + 1000 * 60 * 2).toString()
            );
        }
    };

    const clearUserData = () => {
        globalStore.setCurrentUser(undefined);
        localStorage.removeItem(USERNAME_KEY);
        localStorage.removeItem(ID_KEY);
        localStorage.removeItem(EXP_KEY);
        localStorage.removeItem(ROLES_KEY);
    };

    const login = async (loginRequest: LoginRequestDto) => {
        try {
            const user = await api.post("/auth/login", loginRequest);
            setUserData(user.data.data);
        } catch (error) {
            console.error("Login failed:", error);
            throw error;
        }
    };

    const register = async (registerRequest: RegisterRequestDto) => {
        try {
            await api.post("/auth/register", registerRequest);
        } catch (error) {
            console.error("Registration failed:", error);
            throw error;
        }
    };

    const loginWithGoogle = async (credential: string) => {
        try {
            const googleLoginRequest: GoogleLoginRequestDto = {
                credential: credential,
            };

            const user = await api.post("/auth/google", googleLoginRequest);
            setUserData(user.data.data);
        } catch (error) {
            console.error("Google login failed:", error);
            throw error;
        }
    };

    const getUser = async () => {
        try {
            const user = await api.get("/auth/me");
            setUserData(user.data.data);
        } catch (error) {
            console.error("Get user failed:", error);
            clearUserData();
            throw error;
        }
    };

    const refresh = async () => {
        try {
            const user = await api.post("/auth/refresh");
            setUserData(user.data.data);
        } catch (error) {
            console.error("Token refresh failed:", error);
            clearUserData();
            throw error;
        }
    };

    const logout = async () => {
        try {
            await api.post("/auth/logout");
            clearUserData();
        } catch (error) {
            console.error("Logout failed:", error);
            // Clear local data even if server request fails
            clearUserData();
        }
    };

    return {
        login,
        logout,
        handleUserLoggedInCheck,
        refresh,
        register,
        loginWithGoogle,
    };
};

export default useAuth;
