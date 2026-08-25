import {
    ActionIcon,
    Badge,
    Button,
    Card,
    Group,
    Menu,
    Paper,
    ScrollArea,
    Stack,
    Text,
} from "@mantine/core";
import { ReactNode, useEffect, useState } from "react";
import { CiFilter } from "react-icons/ci";
import { FiPlus, FiPlusSquare } from "react-icons/fi";
import { MdDelete, MdEdit, MdSort } from "react-icons/md";
import ConfirmationDialog from "../../../shared/components/ConfirmationDialog";
import {
    QueryKindColor,
    QueryKindLabel,
    QueryKinds,
} from "../../../shared/constants/QueryKinds";
import { useTrackerOperations } from "../../../shared/hooks/useTrackerOperations";
import { describeQuery } from "../../../shared/utils/formatters/QueryFormatter";
import { useFields } from "../../fields/context/FieldsContext";
import { useTracker } from "../../trackers/context/TrackerContext";
import { TrackerDto } from "../../trackers/types/TrackerDto";
import { useViews } from "../../views/context/ViewsContext";
import { useQueries } from "../context/QueriesContext";
import { QueryDto } from "../types/QueryDto";
import { CreateQueryDto } from "../types/requests/CreateQueryDto";
import QueryFormDialog from "./QueryFormDialog";
import QueryTemplateDialog from "./QueryTemplateDialog";

interface Props {
    tracker: TrackerDto;
    // Rendered in the action row right of Create, so the Views/Queries switch shares it.
    headerSection?: ReactNode;
}

const MAX_QUERIES = 50;

enum OpenDialogType {
    CreateQuery,
    CreateFromTemplate,
    EditQuery,
    DeleteQuery,
}

export default function Queries({ tracker, headerSection }: Props) {
    const { queries, refreshQueriesIfDirty } = useQueries();
    const { views, refreshViewsIfDirty } = useViews();
    const { fields } = useFields();
    const { canEditSchema } = useTracker();
    const { createQuery, deleteQuery } = useTrackerOperations();

    const [selectedQuery, setSelectedQuery] = useState<QueryDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    useEffect(() => {
        refreshQueriesIfDirty();
        refreshViewsIfDirty();
    }, []);

    // A query has no name, so the only way a board or a view can be traced back to it is
    // through the views that use it.
    const viewsUsing = (queryId: string) =>
        views.filter((v) => v.queries.some((q) => q.id === queryId));

    const addFromTemplate = async (clauses: CreateQueryDto[]) => {
        for (const clause of clauses) await createQuery(clause);
    };

    const canAdd =
        canEditSchema && fields.length > 0 && queries.length < MAX_QUERIES;

    return (
        <>
            <Stack gap="md" h="100%">
                {(canEditSchema || headerSection) && (
                    <Group justify="space-between" w="100%">
                        <Group gap="sm">
                            {canEditSchema && (
                                <Menu position="bottom-start">
                                    <Menu.Target>
                                        <Button
                                            color={tracker.color}
                                            variant="outline"
                                            leftSection={<FiPlus size={18} />}
                                            disabled={!canAdd}
                                        >
                                            Create
                                        </Button>
                                    </Menu.Target>
                                    <Menu.Dropdown>
                                        <Menu.Item
                                            leftSection={<CiFilter size={16} />}
                                            onClick={() => {
                                                setSelectedQuery(undefined);
                                                setOpenDialogType(
                                                    OpenDialogType.CreateQuery,
                                                );
                                            }}
                                        >
                                            Filter or sort
                                        </Menu.Item>
                                        <Menu.Item
                                            leftSection={
                                                <FiPlusSquare size={14} />
                                            }
                                            onClick={() =>
                                                setOpenDialogType(
                                                    OpenDialogType.CreateFromTemplate,
                                                )
                                            }
                                        >
                                            From a template
                                        </Menu.Item>
                                    </Menu.Dropdown>
                                </Menu>
                            )}
                            {headerSection}
                        </Group>
                        {canEditSchema && (
                            <Text c="dimmed" size="sm">
                                {queries.length}/{MAX_QUERIES}
                            </Text>
                        )}
                    </Group>
                )}

                <ScrollArea flex={1} mih={0}>
                    {queries.length > 0 ? (
                        <Stack gap="md">
                            {queries.map((query) => {
                                const used = viewsUsing(query.id);
                                return (
                                    <Card
                                        key={query.id}
                                        p="md"
                                        radius="md"
                                        withBorder
                                    >
                                        <Group
                                            align="flex-start"
                                            justify="space-between"
                                            wrap="nowrap"
                                        >
                                            <Stack gap="xs" flex={1} miw={0}>
                                                <Group gap="xs" wrap="nowrap">
                                                    <Badge
                                                        variant="light"
                                                        color={
                                                            QueryKindColor[
                                                                query.kind
                                                            ]
                                                        }
                                                        size="sm"
                                                        leftSection={
                                                            query.kind ===
                                                            QueryKinds.Sort ? (
                                                                <MdSort
                                                                    size={12}
                                                                />
                                                            ) : (
                                                                <CiFilter
                                                                    size={12}
                                                                />
                                                            )
                                                        }
                                                    >
                                                        {
                                                            QueryKindLabel[
                                                                query.kind
                                                            ]
                                                        }
                                                    </Badge>
                                                    <Text
                                                        fw={500}
                                                        className="wrapped-text"
                                                    >
                                                        {describeQuery(query)}
                                                    </Text>
                                                </Group>
                                                <Text c="dimmed" size="sm">
                                                    {used.length === 0
                                                        ? "Not used by any view"
                                                        : `Used by ${used
                                                              .map(
                                                                  (v) => v.name,
                                                              )
                                                              .join(", ")}`}
                                                </Text>
                                            </Stack>
                                            {canEditSchema && (
                                                <Group gap="xs" wrap="nowrap">
                                                    <ActionIcon
                                                        variant="outline"
                                                        color="green"
                                                        size="lg"
                                                        onClick={() => {
                                                            setSelectedQuery(
                                                                query,
                                                            );
                                                            setOpenDialogType(
                                                                OpenDialogType.EditQuery,
                                                            );
                                                        }}
                                                        aria-label="Edit query"
                                                    >
                                                        <MdEdit size={16} />
                                                    </ActionIcon>
                                                    <ActionIcon
                                                        variant="outline"
                                                        color="red"
                                                        size="lg"
                                                        onClick={() => {
                                                            setSelectedQuery(
                                                                query,
                                                            );
                                                            setOpenDialogType(
                                                                OpenDialogType.DeleteQuery,
                                                            );
                                                        }}
                                                        aria-label="Delete query"
                                                    >
                                                        <MdDelete size={16} />
                                                    </ActionIcon>
                                                </Group>
                                            )}
                                        </Group>
                                    </Card>
                                );
                            })}
                        </Stack>
                    ) : (
                        <Paper withBorder p="xl" radius="md">
                            <Stack gap="md" align="center">
                                <Text size="lg" fw={500} c="dimmed">
                                    No Queries Available
                                </Text>
                                <Text ta="center" c="dimmed">
                                    A query is a single filter or sort,
                                    combined into views.
                                </Text>
                            </Stack>
                        </Paper>
                    )}
                </ScrollArea>
            </Stack>

            {openDialogType === OpenDialogType.CreateQuery && (
                <QueryFormDialog
                    tracker={tracker}
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}

            {openDialogType === OpenDialogType.CreateFromTemplate && (
                <QueryTemplateDialog
                    remainingSlots={MAX_QUERIES - queries.length}
                    onSubmitClauses={addFromTemplate}
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}

            {selectedQuery && openDialogType === OpenDialogType.EditQuery && (
                <QueryFormDialog
                    tracker={tracker}
                    queryId={selectedQuery.id}
                    initialQuery={selectedQuery}
                    onClose={() => {
                        setOpenDialogType(undefined);
                        setSelectedQuery(undefined);
                    }}
                />
            )}

            {selectedQuery && openDialogType === OpenDialogType.DeleteQuery && (
                <ConfirmationDialog
                    isOpen
                    onClose={() => setOpenDialogType(undefined)}
                    onConfirm={async () => {
                        await deleteQuery(selectedQuery.id);
                        setOpenDialogType(undefined);
                        setSelectedQuery(undefined);
                    }}
                    severity="warning"
                    message={
                        viewsUsing(selectedQuery.id).length > 0
                            ? `Delete "${describeQuery(selectedQuery)}"? It will be removed from ${
                                  viewsUsing(selectedQuery.id).length
                              } view(s) that use it.`
                            : `Are you sure you want to delete "${describeQuery(selectedQuery)}"?`
                    }
                />
            )}
        </>
    );
}
