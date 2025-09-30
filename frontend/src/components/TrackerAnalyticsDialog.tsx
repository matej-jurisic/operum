import {
    ActionIcon,
    Badge,
    Button,
    Card,
    Group,
    Modal,
    Stack,
    Text,
} from "@mantine/core";
import { useEffect, useState } from "react";
import { MdDelete } from "react-icons/md";
import api from "../api/api";
import { useTracker } from "../context/TrackerContext";
import { AnalyticDto } from "../model/AnalyticDto";
import { DataTypeColor, FieldType } from "../model/constants/DataTypes";
import { TrackerAnalyticDto } from "../model/TrackerAnalyticDto";
import AnalyticSelectionDialog from "./AnalyticSelectionDialog";
import TrackerAnalyticFormDialog from "./TrackerAnalyticFormDialog";

const getTrackerAnalytics = async (trackerId: string) => {
    const response = await api.get(
        `/trackers/${trackerId}/analytic-configrations`
    );
    return response.data.data;
};

interface Props {
    onClose: () => void;
}

function isFieldType(value: string): value is FieldType {
    return value in DataTypeColor;
}

export default function TrackerAnalyticsDialog({ onClose }: Props) {
    const { tracker, RemoveAnalyticFromTracker } = useTracker();
    const [analytics, setAnalytics] = useState<TrackerAnalyticDto[]>([]);
    const [isSelectionOpen, setIsSelectionOpen] = useState(false);
    const [isMappingOpen, setIsMappingOpen] = useState(false);
    const [selectedAnalytic, setSelectedAnalytic] =
        useState<AnalyticDto | null>(null);

    const fetchAnalytics = async () => {
        const data = await getTrackerAnalytics(tracker.id);
        setAnalytics(data);
    };

    useEffect(() => {
        fetchAnalytics();
    }, [tracker.id]);

    const handleAddSuccess = () => {
        setIsMappingOpen(false);
        setSelectedAnalytic(null);
        fetchAnalytics();
    };

    const handleAnalyticSelect = (analytic: AnalyticDto) => {
        setSelectedAnalytic(analytic);
        setIsSelectionOpen(false);
        setIsMappingOpen(true);
    };

    const handleRemoveAnalytic = async (trackerAnalyticId: string) => {
        await RemoveAnalyticFromTracker(trackerAnalyticId);
        fetchAnalytics();
    };

    return (
        <>
            <Modal
                opened
                onClose={onClose}
                title="Tracker Analytics"
                centered
                size="lg"
            >
                <Stack>
                    {analytics.length === 0 ? (
                        <Text c="dimmed" ta="center" py="xl">
                            No analytics configured yet. Add one to get started.
                        </Text>
                    ) : (
                        <Stack gap="md">
                            {analytics.map((analytic) => (
                                <>
                                    <Card withBorder>
                                        <Group
                                            wrap="nowrap"
                                            justify="space-between"
                                            align="flex-start"
                                        >
                                            <Stack key={analytic.id}>
                                                <Text>{`${analytic.name}`}</Text>

                                                <Group
                                                    justify="space-between"
                                                    wrap="nowrap"
                                                >
                                                    <Group gap="md">
                                                        {analytic.trackerAnalyticFields.map(
                                                            (field) => (
                                                                <Group
                                                                    wrap="nowrap"
                                                                    gap="xs"
                                                                    key={
                                                                        field.id
                                                                    }
                                                                >
                                                                    <Badge
                                                                        size="sm"
                                                                        variant="light"
                                                                        color={
                                                                            isFieldType(
                                                                                field.type
                                                                            )
                                                                                ? DataTypeColor[
                                                                                      field
                                                                                          .type
                                                                                  ]
                                                                                : "gray"
                                                                        }
                                                                    >{`${field.purpose}`}</Badge>
                                                                    <Text size="sm">
                                                                        {
                                                                            field.fieldName
                                                                        }
                                                                    </Text>
                                                                </Group>
                                                            )
                                                        )}
                                                    </Group>
                                                </Group>
                                            </Stack>
                                            <ActionIcon
                                                color="red"
                                                variant="outline"
                                                onClick={() =>
                                                    handleRemoveAnalytic(
                                                        analytic.id
                                                    )
                                                }
                                            >
                                                <MdDelete size={18} />
                                            </ActionIcon>
                                        </Group>
                                    </Card>
                                </>
                            ))}
                        </Stack>
                    )}
                    <Button
                        color={tracker.color}
                        onClick={() => setIsSelectionOpen(true)}
                    >
                        Add New Analytic
                    </Button>
                </Stack>
            </Modal>

            {isSelectionOpen && (
                <AnalyticSelectionDialog
                    onClose={() => setIsSelectionOpen(false)}
                    onSelect={handleAnalyticSelect}
                />
            )}

            {isMappingOpen && selectedAnalytic && (
                <TrackerAnalyticFormDialog
                    onClose={handleAddSuccess}
                    selectedAnalytic={selectedAnalytic}
                />
            )}
        </>
    );
}
