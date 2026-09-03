import {
    ActionIcon,
    Divider,
    Group,
    Paper,
    Select,
    Stack,
    Text,
} from "@mantine/core";
import { UseFormReturnType } from "@mantine/form";
import { MdDelete } from "react-icons/md";
import { PurposeDto } from "../../analytics/types/AnalyticConfigDto";
import { FieldDto } from "../../fields/types/FieldDto";
import { ViewDto } from "../../views/types/ViewDto";
import EntryFilterListEditor from "../../views/components/EntryFilterListEditor";

interface SourceValue {
    trackerId: string | null;
    fieldByPurpose: Record<string, string>;
    viewId: string | null;
}

interface Props {
    index: number;
    source: SourceValue;
    fields: FieldDto[];
    views: ViewDto[];
    purposes: PurposeDto[];
    /** "X axis" / "Y axis" for a paired correlation, a numbered label for a merge, none
        for a single source. */
    heading?: string;
    canRemove: boolean;
    onRemove: () => void;
    trackerOptions: { value: string; label: string }[];
    trackerColor?: string;
    onTrackerChange: (trackerId: string | null) => void;
    /** The options a purpose select offers for this source, already narrowed. */
    fieldOptionsFor: (
        purpose: PurposeDto,
    ) => { value: string; label: string }[];
    /** The purpose whose type later sources are pinned to the first's, and that type. */
    narrowPurpose?: string;
    narrowType?: string;
    onFieldChange: (purposeName: string, fieldId: string) => void;
    onViewChange: (viewId: string | null) => void;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    form: UseFormReturnType<any>;
    filtersPath: string;
}

/** One tracker's row in the Explore form: its tracker, the field for each purpose the
    calculation needs, an optional starting view, and inline filters. */
export function SourcePanel({
    index,
    source,
    fields,
    views,
    purposes,
    heading,
    canRemove,
    onRemove,
    trackerOptions,
    trackerColor,
    onTrackerChange,
    fieldOptionsFor,
    narrowPurpose,
    narrowType,
    onFieldChange,
    onViewChange,
    form,
    filtersPath,
}: Props) {
    return (
        <Paper withBorder p="sm" radius="md">
            <Stack gap="sm">
                {heading && (
                    <Group justify="space-between">
                        <Text size="sm" fw={600}>
                            {heading}
                        </Text>
                        {canRemove && (
                            <ActionIcon
                                size="sm"
                                variant="outline"
                                color="red"
                                onClick={onRemove}
                                aria-label="Remove tracker"
                            >
                                <MdDelete size={14} />
                            </ActionIcon>
                        )}
                    </Group>
                )}

                <Select
                    label="Tracker"
                    placeholder="Select a tracker"
                    data={trackerOptions}
                    value={source.trackerId}
                    onChange={onTrackerChange}
                    searchable
                />

                {purposes.map((purpose) => (
                    <Select
                        key={purpose.name}
                        label={purpose.name}
                        placeholder={`Select field (${purpose.allowedDataTypes.join(
                            ", ",
                        )})`}
                        data={fieldOptionsFor(purpose)}
                        value={source.fieldByPurpose[purpose.name] || null}
                        onChange={(value) =>
                            onFieldChange(purpose.name, value ?? "")
                        }
                        disabled={!source.trackerId}
                        clearable
                        description={
                            index > 0 &&
                            purpose.name === narrowPurpose &&
                            narrowType
                                ? `Limited to ${narrowType} fields so the trackers line up.`
                                : undefined
                        }
                    />
                ))}

                {views.length > 0 && (
                    <Select
                        label="Start from view"
                        placeholder="No view"
                        data={views.map((v) => ({
                            value: v.id,
                            label: v.name,
                        }))}
                        value={source.viewId}
                        onChange={onViewChange}
                        clearable
                    />
                )}

                {source.trackerId && fields.length > 0 && (
                    <>
                        <Divider />
                        <EntryFilterListEditor
                            fields={fields}
                            form={form}
                            filtersPath={filtersPath}
                            color={trackerColor}
                        />
                    </>
                )}
            </Stack>
        </Paper>
    );
}
