import { SimpleGrid, Stack } from "@mantine/core";
import {
    NumericChartAnalyticResultDto,
    ScatterChartAnalyticResultDto,
    SingleValueAnalyticResultDto,
} from "../model/AnalyticResultDto";
import { TrackerAnalyticsResponseDto } from "../model/TrackerAnalyticsResponseDto";
import { ChartCard } from "./ChartCard";
import { ScatterChartCard } from "./ScatterChartCard";
import { StatCard } from "./StatCard";

interface AnalyticsGridProps {
    analytics: TrackerAnalyticsResponseDto;
    isConfiguring: boolean;
    onEntryClick: (entryId: string) => void;
}

export function AnalyticsGrid({
    analytics,
    isConfiguring,
    onEntryClick,
}: AnalyticsGridProps) {
    return (
        <Stack gap="md">
            {/* Single value analytics */}
            {analytics.singleValueAnalytics.length > 0 && (
                <SimpleGrid
                    cols={{ base: 1, xs: 2, sm: 3, md: 3, lg: 4 }}
                    spacing="md"
                >
                    {analytics.singleValueAnalytics.map(
                        (analytic: SingleValueAnalyticResultDto) => (
                            <StatCard
                                key={analytic.trackerAnalyticId}
                                analytic={analytic}
                                onEntryClick={onEntryClick}
                                isConfiguring={isConfiguring}
                            />
                        )
                    )}
                </SimpleGrid>
            )}

            {/* Line/Bar charts */}
            {analytics.numericChartAnalytics.length > 0 && (
                <SimpleGrid cols={{ base: 1, md: 2, xl: 3 }} spacing="md">
                    {analytics.numericChartAnalytics.map(
                        (analytic: NumericChartAnalyticResultDto) => (
                            <ChartCard
                                key={analytic.trackerAnalyticId}
                                analytic={analytic}
                                isConfiguring={isConfiguring}
                            />
                        )
                    )}
                </SimpleGrid>
            )}

            {/* Scatter charts */}
            {analytics.scatterChartAnalytics.length > 0 && (
                <SimpleGrid cols={{ base: 1, md: 2, xl: 3 }} spacing="md">
                    {analytics.scatterChartAnalytics.map(
                        (analytic: ScatterChartAnalyticResultDto) => (
                            <ScatterChartCard
                                key={analytic.trackerAnalyticId}
                                analytic={analytic}
                                isConfiguring={isConfiguring}
                            />
                        )
                    )}
                </SimpleGrid>
            )}
        </Stack>
    );
}
