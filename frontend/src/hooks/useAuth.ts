import api from "../api/api";
import { ApplicationUserDto } from "../model/ApplicationUserDto";
import { LoginRequestDto } from "../model/requests/LoginRequestDto";
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
                refresh();
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
        localStorage.setItem("exp", (Date.now() + 1000 * 60 * 2).toString());
    };

    const clearUserData = () => {
        globalStore.setCurrentUser(undefined);
        localStorage.removeItem(USERNAME_KEY);
        localStorage.removeItem(ID_KEY);
        localStorage.removeItem("exp");
    };

    const authenticate = async (loginRequest: LoginRequestDto) => {
        const user = await api.post("/auth/login", loginRequest);
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
        authenticate,
        logout,
        handleUserLoggedInCheck,
        refresh,
    };
};

export default useAuth;
