import { makeAutoObservable } from "mobx";
import { ApplicationUserDto } from "../model/ApplicationUserDto";

class GlobalStore {
    currentUser: ApplicationUserDto | undefined;
    checkingAuth: boolean = true;

    constructor() {
        makeAutoObservable(this);
    }

    setCurrentUser(user: ApplicationUserDto | undefined) {
        this.currentUser = user;
    }

    setCheckingAuth(value: boolean) {
        this.checkingAuth = value;
    }
}

const globalStore = new GlobalStore();
export default globalStore;
