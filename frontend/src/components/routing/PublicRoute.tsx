import { observer } from "mobx-react";
import { JSX } from "react";
import { Navigate } from "react-router-dom";
import globalStore from "../../stores/GlobalStore";
import GenericRoute from "./GenericRoute";

interface PublicRouteProps {
    page: JSX.Element;
}

export const PublicRoute = (props: PublicRouteProps) => {
    if (globalStore.currentUser) return <Navigate to="/home" />;

    return <GenericRoute page={props.page} />;
};

const ObservedPublicRoute = observer(PublicRoute);
export default ObservedPublicRoute;
