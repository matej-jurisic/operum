import { Modal, Table, Text } from "@mantine/core";
import { useMemo } from "react";
import { renderValue } from "../../../shared/utils/formatters/ValueRenderer";
import { DonutChartAnaylticDto } from "../types/AnalyticDto";
import { getAxisFormatter } from "./ChartFormatters";

interface Props {
    analytic: DonutChartAnaylticDto;
    /** Firefly-style data whose categories are all outflows: the ring shows magnitudes,
        so shares are taken against the total magnitude too. */
    isAbsolute: boolean;
    opened: boolean;
    onClose: () => void;
}

export function DonutValuesModal({ analytic, isAbsolute, opened, onClose }: Props) {
    const rows = useMemo(() => {
        const format = getAxisFormatter(analytic.valueField.type);
        const magnitude = (v: number) => (isAbsolute ? Math.abs(v) : v);
        const total = analytic.points.reduce(
            (sum, p) => sum + Math.max(magnitude(p.value ?? 0), 0),
            0,
        );

        return [...analytic.points]
            .sort((a, b) => magnitude(b.value ?? 0) - magnitude(a.value ?? 0))
            .map((p) => {
                const value = p.value ?? 0;
                const share =
                    total > 0 && magnitude(value) > 0
                        ? magnitude(value) / total
                        : null;
                return {
                    name: renderValue(analytic.nameField.type, p.name) || "Unknown",
                    value: format(value),
                    share: share === null ? "" : `${(share * 100).toFixed(1)}%`,
                };
            });
    }, [analytic, isAbsolute]);

    return (
        <Modal opened={opened} onClose={onClose} title={analytic.name} size="md">
            <Table>
                <Table.Thead>
                    <Table.Tr>
                        <Table.Th>{analytic.nameField.name}</Table.Th>
                        <Table.Th ta="right">{analytic.valueField.name}</Table.Th>
                        <Table.Th ta="right">Share</Table.Th>
                    </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                    {rows.map((row, index) => (
                        <Table.Tr key={index}>
                            <Table.Td>{row.name}</Table.Td>
                            <Table.Td ta="right">{row.value}</Table.Td>
                            <Table.Td ta="right">
                                <Text size="sm" c="dimmed">
                                    {row.share}
                                </Text>
                            </Table.Td>
                        </Table.Tr>
                    ))}
                </Table.Tbody>
            </Table>
        </Modal>
    );
}
