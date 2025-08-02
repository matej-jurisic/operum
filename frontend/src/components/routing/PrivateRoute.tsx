import { observer } from "mobx-react";
import { JSX } from "react";
import { Navigate } from "react-router-dom";
import globalStore from "../../stores/GlobalStore";
import GenericRoute from "./GenericRoute";

interface PrivateRouteProps {
    page: JSX.Element;
    withNavigation?: boolean | undefined;
}

export const PrivateRoute = (props: PrivateRouteProps) => {
    if (!globalStore.currentUser) return <Navigate to="/auth" />;

    return <GenericRoute page={props.page} />;
};

const ObservedPrivateRoute = observer(PrivateRoute);
export default ObservedPrivateRoute;
