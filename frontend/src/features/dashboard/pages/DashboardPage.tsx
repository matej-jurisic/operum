import {
    ActionIcon,
    Button,
    Center,
    Divider,
    Group,
    Loader,
    Stack,
    Text,
    ThemeIcon,
    Title,
    useMantineTheme,
} from "@mantine/core";
import { createElement, useEffect, useState } from "react";
import { CiSettings } from "react-icons/ci";
import { FiPlus } from "react-icons/fi";
import { TbLayoutDashboard } from "react-icons/tb";
import { useParams } from "react-router-dom";
import Header from "../../../shared/components/Header";
import { resolveTrackerIcon as resolveIcon } from "../../../shared/constants/TrackerIcons";
import { AddDashboardItemModal } from "../components/AddDashboardItemModal";
import { DashboardGrid } from "../components/DashboardGrid";
import { DashboardProvider, useDashboard } from "../context/DashboardContext";

function DashboardContent() {
    const {
        dashboard,
        analytics,
        isLoading,
        refreshDashboard,
        refreshAnalytics,
        addItem,
        removeItem,
        reorderItems,
    } = useDashboard();
    const theme = useMantineTheme();
    const [isConfiguring, setIsConfiguring] = useState(false);
    const [isAddOpen, setIsAddOpen] = useState(false);

    useEffect(() => {
        refreshDashboard();
        refreshAnalytics();
    }, []);

    if (!dashboard && isLoading) {
        return (
            <Center style={{ flex: 1 }}>
                <Loader />
            </Center>
        );
    }

    if (!dashboard) return null;

    const color =
        dashboard.color && dashboard.color in theme.colors
            ? dashboard.color
            : theme.primaryColor;

    return (
        <Stack h="100%" gap="md">
            <Stack justify="space-between" w="100%">
                <Group w="100%" justify="flex-end">
                    <Header color={color} />
                </Group>
                <Group gap="sm" align="center">
                    {dashboard.icon && (
                        <ThemeIcon
                            size={36}
                            radius="md"
                            variant="light"
                            color={dashboard.color}
                        >
                            {createElement(resolveIcon(dashboard.icon), {
                                size: 20,
                            })}
                        </ThemeIcon>
                    )}
                    <Title order={2} c={color}>
                        {dashboard.name}
                    </Title>
                </Group>
                <Divider />
                <Group h={36} justify="flex-end">
                    {isConfiguring && (
                        <Button
                            variant="outline"
                            color={color}
                            leftSection={<FiPlus size={18} />}
                            onClick={() => setIsAddOpen(true)}
                        >
                            Add analytic
                        </Button>
                    )}
                    <ActionIcon
                        size="lg"
                        color={color}
                        variant={isConfiguring ? "filled" : "outline"}
                        onClick={() => setIsConfiguring((v) => !v)}
                    >
                        <CiSettings size={18} />
                    </ActionIcon>
                </Group>
            </Stack>

            {analytics.length === 0 && !isLoading ? (
                <Stack align="center" gap="md" py={80}>
                    <ThemeIcon
                        size={72}
                        radius="xl"
                        variant="light"
                        color={color}
                    >
                        <TbLayoutDashboard size={36} />
                    </ThemeIcon>
                    <Stack align="center" gap={4}>
                        <Text fw={700} size="xl">
                            No analytics added yet
                        </Text>
                        <Text size="sm" c="dimmed">
                            Add analytics from your trackers to display them
                            here
                        </Text>
                    </Stack>
                    <Button
                        color={color}
                        leftSection={<FiPlus size={16} />}
                        onClick={() => {
                            setIsConfiguring(true);
                            setIsAddOpen(true);
                        }}
                    >
                        Get Started
                    </Button>
                </Stack>
            ) : isLoading ? (
                <Center style={{ flex: 1 }}>
                    <Loader />
                </Center>
            ) : (
                <DashboardGrid
                    analytics={analytics}
                    color={color}
                    isConfiguring={isConfiguring}
                    onReorder={reorderItems}
                    onRemove={isConfiguring ? removeItem : undefined}
                />
            )}

            {isAddOpen && (
                <AddDashboardItemModal
                    onClose={() => setIsAddOpen(false)}
                    onAdd={addItem}
                />
            )}
        </Stack>
    );
}

export default function DashboardPage() {
    const { dashboardId } = useParams<{ dashboardId: string }>();

    if (!dashboardId) return null;

    return (
        <DashboardProvider dashboardId={dashboardId}>
            <DashboardContent />
        </DashboardProvider>
    );
}
