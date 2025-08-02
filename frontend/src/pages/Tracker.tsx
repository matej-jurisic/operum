import { Button, Group, Input, Stack, Text } from "@mantine/core";
import { useEffect, useState } from "react";
import { CiFolderOn } from "react-icons/ci";
import { IoMdReturnLeft } from "react-icons/io";
import { useNavigate, useParams } from "react-router-dom";
import api from "../api/api";
import { AddFieldDialog } from "../components/AddFieldDialog";
import CreateEntryDialog from "../components/CreateEntryDialog";
import ViewFieldsDialog from "../components/ViewFieldsDialog";
import { EntryDto } from "../model/EntryDto";
import { FieldValueDto } from "../model/FieldValueDto";
import { TrackerDto } from "../model/TrackerDto";

const GetEntries = async (trackerId: string) => {
    const response = await api.get(`/trackers/${trackerId}/entries`);
    return response.data.data;
};

const GetTracker = async (trackerId: string) => {
    const response = await api.get(`/trackers/${trackerId}`);
    return response.data.data;
};

enum OpenDialogType {
    CreateEntry,
    AddField,
    ViewFields,
}

const renderValue = (v: FieldValueDto) => {
    if (v.value === null) {
        return "Value not set.";
    }
    if (typeof v.value === "string") {
        if (v.fieldType === "date") {
            return new Date(v.value).toLocaleDateString();
        }
        if (v.fieldType === "datetime") {
            return new Date(v.value).toLocaleString();
        }
        if (v.fieldType === "time") {
            return new Date(`1970-01-01T${v.value}`).toLocaleTimeString();
        }
        return v.value;
    }
    if (typeof v.value === "number") {
        return v.value;
    }
    if (typeof v.value === "boolean") {
        return v.value.toString();
    }
    return "Unexpected data type";
};

export default function Tracker() {
    const { trackerId } = useParams();
    const navigate = useNavigate();

    const [entries, setEntries] = useState<EntryDto[]>([]);
    const [tracker, setTracker] = useState<TrackerDto>();
    const [openDialogType, setOpenDialogType] = useState<OpenDialogType>();

    useEffect(() => {
        const GetData = async () => {
            if (trackerId) {
                setTracker(await GetTracker(trackerId));
                setEntries(await GetEntries(trackerId));
            }
        };

        GetData();
    }, [trackerId]);

    if (!tracker) {
        return <></>;
    }

    return (
        <>
            <Stack style={{ padding: "20px" }} align="flex-end">
                <Group style={{ width: "100%" }} justify="space-between">
                    <Group>
                        <Button onClick={() => navigate("/")} variant="subtle">
                            <Group gap={10}>
                                <IoMdReturnLeft fontSize={22} />
                                <p>Back</p>
                            </Group>
                        </Button>
                    </Group>
                    <Group>
                        {tracker.fields.length > 0 && (
                            <Button
                                onClick={() => {
                                    setOpenDialogType(
                                        OpenDialogType.CreateEntry
                                    );
                                }}
                            >
                                Create Entry
                            </Button>
                        )}

                        <Button
                            onClick={() => {
                                setOpenDialogType(OpenDialogType.AddField);
                            }}
                        >
                            Add Field
                        </Button>
                        <Button
                            variant="outline"
                            onClick={() => {
                                setOpenDialogType(OpenDialogType.ViewFields);
                            }}
                        >
                            <Group gap={5}>
                                <CiFolderOn />
                                <Text>{`Field count: ${tracker.fields.length}`}</Text>
                            </Group>
                        </Button>
                    </Group>
                </Group>
                <Stack style={{ width: "100%" }}>
                    {entries.map((x) => (
                        <Group
                            style={{
                                border: "1px solid #ddd",
                                borderRadius: "8px",
                                padding: "2rem",
                                alignItems: "center",
                                gap: "50px",
                            }}
                        >
                            {new Date(x.createdAt).toLocaleString()}
                            <Group style={{ gap: "20px" }}>
                                {x.fieldValues.map((v) => (
                                    <Input.Wrapper label={v.fieldName}>
                                        <Input
                                            readOnly
                                            value={renderValue(v)}
                                        />
                                    </Input.Wrapper>
                                ))}
                            </Group>
                        </Group>
                    ))}
                </Stack>
            </Stack>
            {openDialogType === OpenDialogType.CreateEntry && (
                <CreateEntryDialog
                    trackerId={tracker.id}
                    onClose={() => setOpenDialogType(undefined)}
                    onEntryCreated={async () =>
                        setEntries(await GetEntries(tracker.id))
                    }
                />
            )}
            {openDialogType === OpenDialogType.AddField && (
                <AddFieldDialog
                    trackerId={tracker.id}
                    onClose={() => setOpenDialogType(undefined)}
                    onFieldAdded={async () =>
                        setTracker(await GetTracker(tracker.id))
                    }
                />
            )}
            {openDialogType === OpenDialogType.ViewFields && (
                <ViewFieldsDialog
                    tracker={tracker}
                    onClose={() => setOpenDialogType(undefined)}
                />
            )}
        </>
    );
}
