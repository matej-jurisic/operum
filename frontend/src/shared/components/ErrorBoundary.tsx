import { Button, Stack, Text } from "@mantine/core";
import { Component, ErrorInfo, ReactNode } from "react";

interface Props {
    children: ReactNode;
    /** Clears a caught error when it changes, so navigating elsewhere recovers without a reload. */
    resetKey?: string;
}

interface State {
    error: Error | null;
}

/** Catches a render crash below it and shows a reload screen instead of an unmounted blank page. */
export default class ErrorBoundary extends Component<Props, State> {
    state: State = { error: null };

    static getDerivedStateFromError(error: Error): State {
        return { error };
    }

    componentDidCatch(error: Error, info: ErrorInfo) {
        console.error(error, info.componentStack);
    }

    componentDidUpdate(prevProps: Props) {
        if (this.state.error && prevProps.resetKey !== this.props.resetKey) {
            this.setState({ error: null });
        }
    }

    render() {
        if (!this.state.error) return this.props.children;

        return (
            <Stack align="center" gap="md" py={80}>
                <Text fw={700} size="xl">
                    Something went wrong
                </Text>
                <Button onClick={() => window.location.reload()}>Reload</Button>
            </Stack>
        );
    }
}
