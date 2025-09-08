import { Modal } from "@mantine/core";
import { useEffect, useState } from "react";
import api from "../api/api";
import { TrackerDto } from "../model/TrackerDto";
import { ViewDto } from "../model/ViewDto";

interface Props {
    tracker: TrackerDto;
    onClose: () => void;
}

const GetViewList = async (trackerId: string) => {
    const response = await api.get(`trackers/${trackerId}/views`);
    return response.data.data;
};

export default function ViewsDialog(props: Props) {
    const [views, setViews] = useState<ViewDto[]>([]);

    useEffect(() => {
        const GetViews = async () => {
            setViews(await GetViewList(props.tracker.id));
        };

        GetViews();
    }, []);

    return (
        <Modal opened={true} centered onClose={props.onClose} title="Views">
            {views.map((v: ViewDto) => (
                <div key={v.id}>{v.name}</div>
            ))}
        </Modal>
    );
}
