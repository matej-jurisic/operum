import { LoadingOverlay } from "@mantine/core";

interface Props {
    visible: boolean;
}

export default function OperumLoader({ visible }: Props) {
    return (
        <LoadingOverlay
            visible={visible}
            overlayProps={{
                opacity: 0.5,
            }}
            loaderProps={{
                color: "gray",
            }}
        />
    );
}
