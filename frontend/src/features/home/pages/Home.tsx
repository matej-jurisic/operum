import {
    Box,
    Card,
    Container,
    Grid,
    ScrollArea,
    Stack,
    Text,
    ThemeIcon,
    Title,
} from "@mantine/core";
import {
    MdBarChart,
    MdFilterAlt,
    MdLineAxis,
    MdTableChart,
} from "react-icons/md";
import Header from "../../../shared/components/Header";

export default function Home() {
    return (
        <>
            <Header />
            <ScrollArea h={"100%"} mah={"calc(100% - 66px)"}>
                <Box>
                    {/* Hero Section */}
                    <Container size="lg" py={100}>
                        <Stack
                            align="center"
                            gap="xl"
                            justify="center"
                            h={"100%"}
                        >
                            <Title order={1} size={40} fw={900} ta="center">
                                Track Anything, Your Way
                            </Title>
                            <Text size="xl" c="dimmed" maw={600} ta="center">
                                Operum is a flexible data tracking application
                                that adapts to your needs. Create custom
                                trackers, analyze your data, and gain insights
                                effortlessly.
                            </Text>
                        </Stack>
                    </Container>

                    {/* Features Section */}
                    <Container size="lg">
                        <Stack gap="xl">
                            <Title order={3} ta="center">
                                Everything You Need to Track Your Data
                            </Title>

                            <Grid>
                                <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
                                    <Card
                                        shadow="sm"
                                        padding="lg"
                                        radius="md"
                                        withBorder
                                        h="100%"
                                    >
                                        <Stack gap="md">
                                            <ThemeIcon
                                                size={60}
                                                radius="md"
                                                variant="light"
                                            >
                                                <MdTableChart size={30} />
                                            </ThemeIcon>
                                            <Title order={3} size="h4">
                                                Custom Trackers
                                            </Title>
                                            <Text size="sm" c="dimmed">
                                                Define your own fields with
                                                multiple data types.
                                            </Text>
                                        </Stack>
                                    </Card>
                                </Grid.Col>

                                <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
                                    <Card
                                        shadow="sm"
                                        padding="lg"
                                        radius="md"
                                        withBorder
                                        h="100%"
                                    >
                                        <Stack gap="md">
                                            <ThemeIcon
                                                size={60}
                                                radius="md"
                                                variant="light"
                                                color="teal"
                                            >
                                                <MdBarChart size={30} />
                                            </ThemeIcon>
                                            <Title order={3} size="h4">
                                                Advanced Analytics
                                            </Title>
                                            <Text size="sm" c="dimmed">
                                                Visualize your data with charts
                                                and statistics.
                                            </Text>
                                        </Stack>
                                    </Card>
                                </Grid.Col>

                                <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
                                    <Card
                                        shadow="sm"
                                        padding="lg"
                                        radius="md"
                                        withBorder
                                        h="100%"
                                    >
                                        <Stack gap="md">
                                            <ThemeIcon
                                                size={60}
                                                radius="md"
                                                variant="light"
                                                color="grape"
                                            >
                                                <MdFilterAlt size={30} />
                                            </ThemeIcon>
                                            <Title order={3} size="h4">
                                                Custom Views
                                            </Title>
                                            <Text size="sm" c="dimmed">
                                                Create filtered and sorted views
                                                to focus on what matters most.
                                            </Text>
                                        </Stack>
                                    </Card>
                                </Grid.Col>

                                <Grid.Col span={{ base: 12, sm: 6, md: 3 }}>
                                    <Card
                                        shadow="sm"
                                        padding="lg"
                                        radius="md"
                                        withBorder
                                        h="100%"
                                    >
                                        <Stack gap="md">
                                            <ThemeIcon
                                                size={60}
                                                radius="md"
                                                variant="light"
                                                color="orange"
                                            >
                                                <MdLineAxis size={30} />
                                            </ThemeIcon>
                                            <Title order={3} size="h4">
                                                Templates
                                            </Title>
                                            <Text size="sm" c="dimmed">
                                                Quick start your tracking with
                                                pre-built templates.
                                            </Text>
                                        </Stack>
                                    </Card>
                                </Grid.Col>
                            </Grid>
                        </Stack>
                    </Container>

                    {/* CTA Section */}
                    {/* <Container size="lg" py={60}>
                        <Card shadow="md" padding="xl" radius="md" withBorder>
                            <Stack align="center" gap="lg">
                                <Title order={2} ta="center">
                                    Ready to Start Tracking?
                                </Title>
                                <Text
                                    size="lg"
                                    c="dimmed"
                                    ta="center"
                                    maw={500}
                                >
                                    Join users who are already managing their
                                    data more effectively with Operum.
                                </Text>
                                <Button size="lg" variant="filled">
                                    Create Your First Tracker
                                </Button>
                            </Stack>
                        </Card>
                    </Container> */}
                </Box>
            </ScrollArea>
        </>
    );
}
