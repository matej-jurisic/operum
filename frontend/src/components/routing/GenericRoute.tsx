import { JSX } from "react";

interface GenericRouteProps {
    page: JSX.Element;
}

export default function GenericRoute(props: GenericRouteProps) {
    return props.page;
}
