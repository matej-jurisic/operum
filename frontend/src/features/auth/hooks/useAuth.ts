import globalStore from "../../../shared/stores/GlobalStore";
import { authController } from "../api/authenticationController";
import { AuthResponseDto } from "../types/AuthResponseDto";
import { GoogleLoginDto } from "../types/requests/GoogleLoginDto";
import { LoginRequestDto } from "../types/requests/LoginDto";
import { RegisterDto } from "../types/requests/RegisterDto";

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

    const setUserData = (user: AuthResponseDto) => {
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
        const user = await authController.login(loginRequest);
        setUserData(user.data);
    };

    const register = async (registerRequest: RegisterDto) => {
        await authController.register(registerRequest);
    };

    const loginWithGoogle = async (idToken: string) => {
        const googleLoginRequest: GoogleLoginDto = {
            idToken: idToken,
        };

        const user = await authController.googleLogin(googleLoginRequest);
        setUserData(user.data);
    };

    const getUser = async () => {
        const user = await authController.me();
        setUserData(user.data);
    };

    const refresh = async () => {
        const user = await authController.refreshToken();
        setUserData(user.data);
    };

    const logout = async () => {
        await authController.logout();
        clearUserData();
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
