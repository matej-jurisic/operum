import { makeAutoObservable } from "mobx";
import { ApplicationUserDto } from "../model/ApplicationUserDto";

class GlobalStore {
    pageIsLoading = 0;
    currentUser: ApplicationUserDto | undefined;
    isAuthResolved = false;

    constructor() {
        makeAutoObservable(this);
    }

    incrementPageLoading() {
        this.pageIsLoading++;
    }

    decrementPageLoading() {
        this.pageIsLoading = Math.max(...[0, this.pageIsLoading - 1]);
    }

    get isPageLoading() {
        return this.pageIsLoading > 0;
    }

    setCurrentUser(user: ApplicationUserDto | undefined) {
        this.isAuthResolved = true;
        this.currentUser = user;
    }
}

const globalStore = new GlobalStore();
export default globalStore;
