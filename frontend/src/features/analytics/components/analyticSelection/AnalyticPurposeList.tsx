import { Badge, Box, Group, Text } from "@mantine/core";
import {
    DataTypeColor,
    FieldType,
} from "../../../../shared/constants/DataTypes";
import { PurposeDto } from "../../types/AnalyticConfigDto";

interface Props {
    purposes: PurposeDto[];
}

function isFieldType(value: string): value is FieldType {
    return value in DataTypeColor;
}

export function AnalyticPurposeList({ purposes }: Props) {
    return (
        <>
            {purposes.map((purpose) => (
                <Box key={purpose.name}>
                    <Text size="xs" c="dimmed" mb={4}>
                        {purpose.name}
                    </Text>
                    <Group gap={4} wrap="wrap">
                        {purpose.allowedDataTypes.map((type) => (
                            <Badge
                                key={type}
                                variant="outline"
                                size="xs"
                                color={
                                    isFieldType(type)
                                        ? DataTypeColor[type]
                                        : "gray"
                                }
                            >
                                {type}
                            </Badge>
                        ))}
                    </Group>
                </Box>
            ))}
        </>
    );
}
