import api from "../api/api";
import { ApplicationUserDto } from "../model/ApplicationUserDto";
import { LoginRequestDto } from "../model/requests/LoginRequestDto";
import { RegisterRequestDto } from "../model/requests/RegisterRequestDto";
import globalStore from "../stores/GlobalStore";

const USERNAME_KEY = "username";
const ID_KEY = "id";

const useAuth = () => {
    const handleUserLoggedInCheck = () => {
        const username = localStorage.getItem(USERNAME_KEY);
        const id = localStorage.getItem(ID_KEY);
        const exp = localStorage.getItem("exp");

        if (username !== null && id !== null && exp !== null) {
            if (Date.now() > parseInt(exp, 10)) {
                getUser();
                return;
            }
            globalStore.setCurrentUser({
                userName: username,
                id: id,
            });
        } else {
            globalStore.setCurrentUser(undefined);
        }
    };

    const setUserData = (user: ApplicationUserDto) => {
        globalStore.setCurrentUser({
            userName: user.userName,
            id: user.id,
        });
        localStorage.setItem(USERNAME_KEY, user.userName);
        localStorage.setItem(ID_KEY, user.id);
        if (user.tokenExpiry) {
            const expiryDate = new Date(user.tokenExpiry);
            localStorage.setItem(
                "exp",
                (expiryDate.getTime() + 1000 * 60 * 2).toString()
            );
        }
    };

    const clearUserData = () => {
        globalStore.setCurrentUser(undefined);
        localStorage.removeItem(USERNAME_KEY);
        localStorage.removeItem(ID_KEY);
        localStorage.removeItem("exp");
    };

    const login = async (loginRequest: LoginRequestDto) => {
        const user = await api.post("/auth/login", loginRequest);
        setUserData(user.data.data);
    };

    const register = async (registerRequest: RegisterRequestDto) => {
        await api.post("/auth/register", registerRequest);
    };

    const getUser = async () => {
        const user = await api.get("/auth/me");
        setUserData(user.data.data);
    };

    const refresh = async () => {
        const user = await api.get("/auth/refresh");
        setUserData(user.data.data);
    };

    const logout = async () => {
        await api.post("/auth/logout");
        clearUserData();
    };

    return {
        login,
        logout,
        handleUserLoggedInCheck,
        refresh,
        register,
    };
};

export default useAuth;
