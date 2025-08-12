import { makeAutoObservable } from "mobx";
import { ApplicationUserDto } from "../model/ApplicationUserDto";

class GlobalStore {
    currentUser: ApplicationUserDto | undefined;

    constructor() {
        makeAutoObservable(this);
    }

    setCurrentUser(user: ApplicationUserDto | undefined) {
        this.currentUser = user;
    }
}

const globalStore = new GlobalStore();
export default globalStore;
