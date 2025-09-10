import { Container, MantineSpacing } from "@mantine/core";
import { ReactNode } from "react";

interface StickyContainerProps {
    children: ReactNode;
    top?: MantineSpacing | number;
}

export default function StickyContainer({
    children,
    top = 0,
}: StickyContainerProps) {
    return (
        <Container
            fluid
            px={0}
            style={{
                position: "sticky",
                top,
                zIndex: 100,
                backgroundColor: "var(--mantine-color-body)",
            }}
        >
            {children}
        </Container>
    );
}
