import { makeAutoObservable } from "mobx";
import { AuhtResponseDto } from "../model/AuthResponseDto";

class GlobalStore {
    currentUser: AuhtResponseDto | undefined;
    checkingAuth: boolean = true;

    constructor() {
        makeAutoObservable(this);
    }

    setCurrentUser(user: AuhtResponseDto | undefined) {
        this.currentUser = user;
    }

    setCheckingAuth(value: boolean) {
        this.checkingAuth = value;
    }

    userHasRole(role: string) {
        if (
            this.currentUser === undefined ||
            this.currentUser.roles === undefined
        )
            return false;
        return (
            this.currentUser.roles.find(
                (x) => x.toLowerCase() === role.toLowerCase()
            ) !== undefined
        );
    }
}

const globalStore = new GlobalStore();
export default globalStore;
