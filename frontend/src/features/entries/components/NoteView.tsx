import { Alert, Button, Center, Group, Loader, Stack, Textarea } from "@mantine/core";
import { useEffect, useState } from "react";
import { MdWarning } from "react-icons/md";
import { useFields } from "../../fields/context/FieldsContext";
import { useTracker } from "../../trackers/context/TrackerContext";
import { useEntries } from "../context/EntriesContext";
import { entriesController } from "../api/entriesController";

const NOTE_CAP = 1000;
const ID_PREFIX = "# id: ";

interface Props {
    onClose: () => void;
}

export function NoteView({ onClose }: Props) {
    const { tracker, selectedViewIds } = useTracker();
    const { totalCount, markEntriesDirty } = useEntries();
    const { visibleFields } = useFields();

    const [text, setText] = useState("");
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [loadedEntryIds, setLoadedEntryIds] = useState<Set<string>>(new Set());
    const isCapped = totalCount > NOTE_CAP;

    const editableFields = visibleFields.filter((f) => !f.isCalculated);

    useEffect(() => {
        const load = async () => {
            const response = await entriesController.getEntries(
                tracker.id,
                selectedViewIds,
                1,
                NOTE_CAP
            );
            const fetched = response.data.items;
            setLoadedEntryIds(new Set(fetched.map((e) => e.id)));

            if (fetched.length === 0) {
                setText("---");
            } else {
                const blocks = fetched.map((entry) => {
                    const fieldLines = editableFields
                        .map((field) => {
                            const fv = entry.fieldValues.find((fv) => fv.fieldId === field.id);
                            const raw = fv?.value != null ? String(fv.value) : "";
                            return `${field.name}: ${raw.replace(/\n/g, "\\n")}`;
                        })
                        .join("\n");
                    return `${ID_PREFIX}${entry.id}\n${fieldLines}`;
                });
                setText("---\n" + blocks.join("\n---\n") + "\n---");
            }

            setIsLoading(false);
        };
        load();
    }, []);

    const handleSave = async () => {
        const lines = text.split("\n");
        const blocks: string[][] = [];
        let current: string[] = [];

        for (const line of lines) {
            if (line.trim() === "---") {
                if (current.length > 0) { blocks.push(current); current = []; }
            } else {
                current.push(line);
            }
        }
        if (current.length > 0) blocks.push(current);

        const toUpdate: { id: string; fieldValues: Record<string, string> }[] = [];
        const toCreate: Record<string, string>[] = [];
        const unknownIds: string[] = [];

        for (const block of blocks) {
            const idLine = block.find((l) => l.startsWith(ID_PREFIX));
            const fieldValues: Record<string, string> = {};
            for (const line of block) {
                if (line.startsWith("# ")) continue;
                const colonIdx = line.indexOf(": ");
                if (colonIdx === -1) continue;
                fieldValues[line.slice(0, colonIdx)] = line.slice(colonIdx + 2).replace(/\\n/g, "\n");
            }

            if (!idLine) {
                if (Object.keys(fieldValues).length > 0) toCreate.push(fieldValues);
            } else {
                const id = idLine.slice(ID_PREFIX.length).trim();
                if (!loadedEntryIds.has(id)) {
                    unknownIds.push(id);
                } else {
                    toUpdate.push({ id, fieldValues });
                }
            }
        }

        if (unknownIds.length > 0) {
            setError(
                `${unknownIds.length === 1 ? "1 block has an" : `${unknownIds.length} blocks have`} unknown ID${unknownIds.length > 1 ? "s" : ""}: ` +
                unknownIds.join(", ")
            );
            return;
        }

        const presentIds = new Set(toUpdate.map((u) => u.id));
        const toDelete = [...loadedEntryIds].filter((id) => !presentIds.has(id));

        setIsSaving(true);
        setError(null);

        try {
            await entriesController.batchEntries(tracker.id, {
                creates: toCreate,
                updates: toUpdate.map(({ id, fieldValues }) => ({ entryId: id, fieldValues })),
                deletes: toDelete,
            });
            markEntriesDirty();
            onClose();
        } catch {
            setError("Save failed. No changes were applied.");
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <Stack gap="md" flex={1} style={{ minHeight: 0, overflow: "hidden" }}>
            <Group justify="flex-end">
                <Button variant="outline" color={tracker.color} onClick={onClose} disabled={isSaving}>
                    Discard
                </Button>
                <Button color={tracker.color} onClick={handleSave} loading={isSaving}>
                    Save
                </Button>
            </Group>
            {isCapped && (
                <Alert color="orange" icon={<MdWarning size={16} />}>
                    Showing first {NOTE_CAP} of {totalCount} entries. Changes beyond entry {NOTE_CAP} will not be reflected here.
                </Alert>
            )}
            {error && (
                <Alert color="red" icon={<MdWarning size={16} />} onClose={() => setError(null)} withCloseButton>
                    {error}
                </Alert>
            )}
            {isLoading ? (
                <Center flex={1}>
                    <Loader color={tracker.color} />
                </Center>
            ) : (
                <Textarea
                    value={text}
                    onChange={(e) => { setText(e.currentTarget.value); setError(null); }}
                    flex={1}
                    styles={{
                        root: { display: "flex", flexDirection: "column", minHeight: 0 },
                        wrapper: { flex: 1, minHeight: 0 },
                        input: {
                            height: "100%",
                            resize: "none",
                            fontFamily: "var(--mantine-font-family-monospace)",
                        },
                    }}
                />
            )}
        </Stack>
    );
}
