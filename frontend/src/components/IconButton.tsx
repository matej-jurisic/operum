import { Button, ButtonProps } from "@mantine/core";
import { cloneElement, ReactElement } from "react";

interface IconWithSizeProps {
    size?: number;
}

interface IconButtonProps extends ButtonProps {
    icon: ReactElement<IconWithSizeProps>;
    iconSize?: number;
}

export default function IconButton({
    icon,
    iconSize = 18,
    ...props
}: IconButtonProps) {
    const sizedIcon = cloneElement(icon, {
        size: iconSize,
    });

    return <Button {...props}>{sizedIcon}</Button>;
}
