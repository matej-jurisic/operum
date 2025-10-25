// PrivateRoute.tsx
import { observer } from "mobx-react";
import { JSX } from "react";
import { Navigate } from "react-router-dom";
import globalStore from "../../stores/GlobalStore";
import OperumLoader from "../OperumLoader";
import GenericRoute from "./GenericRoute";

interface PrivateRouteProps {
    page: JSX.Element;
    allowedRoles?: string[];
    withNavigation?: boolean | undefined;
}

export const PrivateRoute = (props: PrivateRouteProps) => {
    if (globalStore.checkingAuth) return <OperumLoader visible />;
    if (!globalStore.currentUser) return <Navigate to="/home" />;

    if (props.allowedRoles && props.allowedRoles.length > 0) {
        const hasRole = props.allowedRoles.some((role) =>
            globalStore.userHasRole(role)
        );
        if (!hasRole) {
            return <Navigate to="/home" replace />;
        }
    }

    return <GenericRoute page={props.page} />;
};

const ObservedPrivateRoute = observer(PrivateRoute);
export default ObservedPrivateRoute;
