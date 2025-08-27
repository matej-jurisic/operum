import { Button, FileInput, Modal, Stack } from "@mantine/core";
import { useForm } from "@mantine/form";
import { AxiosRequestConfig } from "axios"; // Import AxiosRequestConfig
import api from "../api/api";

interface ImportEntriesDialogProps {
    onClose: () => void;
    trackerId: string;
    onImport: () => void;
}

const ImportEntries = async (trackerId: string, file: File | null) => {
    if (!file) return;

    const formData = new FormData();
    formData.append("file", file);

    const config: AxiosRequestConfig = {
        headers: {
            "Content-Type": "multipart/form-data",
        },
    };

    await api.post(
        `/trackers/${trackerId}/entries/import-csv`,
        formData,
        config
    );
};

export default function ImportEntriesDialog(props: ImportEntriesDialogProps) {
    const form = useForm<{ file: File | null }>({
        initialValues: {
            file: null,
        },
    });

    return (
        <Modal centered opened onClose={props.onClose} title="Import Entries">
            <form
                onSubmit={form.onSubmit(async (values) => {
                    await ImportEntries(props.trackerId, values.file);
                    props.onClose();
                    props.onImport();
                })}
            >
                <Stack>
                    <FileInput
                        variant="default"
                        accept=".csv"
                        placeholder="Upload file"
                        {...form.getInputProps("file")}
                    />
                    <Button type="submit">Import</Button>
                </Stack>
            </form>
        </Modal>
    );
}
