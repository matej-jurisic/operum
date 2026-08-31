import {
    readDefaultPage,
    writeDefaultPage,
} from "../../../shared/constants/defaultPage";
import globalStore from "../../../shared/stores/GlobalStore";
import navigationStore from "../../../shared/stores/NavigationStore";
import { authController } from "../api/authenticationController";
import { AuthResponseDto } from "../types/AuthResponseDto";
import { profileController } from "../../profile/api/profileController";

const USERNAME_KEY = "username";
const ID_KEY = "id";
const ROLES_KEY = "roles";
const EXP_KEY = "exp";

const useAuth = () => {
    const handleUserLoggedInCheck = async () => {
        globalStore.setCheckingAuth(true);
        try {
            const username = localStorage.getItem(USERNAME_KEY);
            const id = localStorage.getItem(ID_KEY);
            const roles = localStorage.getItem(ROLES_KEY);
            const exp = localStorage.getItem(EXP_KEY);

            if (username && id && exp && roles) {
                if (Date.now() > parseInt(exp, 10)) {
                    await getUser();
                } else {
                    globalStore.setCurrentUser({
                        userName: username,
                        id: id,
                        roles: JSON.parse(roles),
                        defaultPage: readDefaultPage(),
                    });
                }
            } else {
                globalStore.setCurrentUser(undefined);
            }
        } catch {
            globalStore.setCurrentUser(undefined);
        } finally {
            globalStore.setCheckingAuth(false);
        }
    };


    const setUserData = (user: AuthResponseDto) => {
        globalStore.setCurrentUser({
            userName: user.userName,
            id: user.id,
            roles: user.roles,
            timeZone: user.timeZone,
            defaultPage: user.defaultPage,
        });
        // Partial calls (e.g. the profile page after a username change) omit this;
        // only mirror it to storage when the caller actually carried a value.
        if (user.defaultPage !== undefined) writeDefaultPage(user.defaultPage);
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
        navigationStore.clear();
        writeDefaultPage(null);
        localStorage.removeItem(USERNAME_KEY);
        localStorage.removeItem(ID_KEY);
        localStorage.removeItem(EXP_KEY);
        localStorage.removeItem(ROLES_KEY);
    };

    const getUser = async () => {
        const user = await authController.me();
        if (user.isSuccess) {
            setUserData(user.data);
            if (!user.data.timeZone) {
                globalStore.ensureTimezoneCaptured((tz) =>
                    profileController.updateTimezone(tz).then(() => {})
                );
            }
        } else clearUserData();
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
        logout,
        handleUserLoggedInCheck,
        refresh,
        setUserData,
        clearUserData,
    };
};

export default useAuth;
