import { Accordion, Modal } from "@mantine/core";
import { useEffect, useState } from "react";
import AnalyticFormDialog from "../../../trackers/components/AnalyticFormDialog";
import { analyticsController } from "../../api/analyticsController";
import {
    AnalyticConfigDto,
    CodeDto,
    ResultTypeDto,
} from "../../types/AnalyticConfigDto";
import { AnalyticResultTypeItem } from "./AnalyticResultTypeItem";

interface Props {
    onClose: () => void;
}

export default function AnalyticSelectionDialog({ onClose }: Props) {
    const [config, setConfig] = useState<AnalyticConfigDto>();
    const [selectedAnalytic, setSelectedAnalytic] = useState<{
        code: CodeDto;
        resultType: ResultTypeDto;
    }>();
    const [loading, setLoading] = useState(true);

    const fetchConfig = async () => {
        setLoading(true);
        const response = await analyticsController.getAnalyticsConfig();
        setConfig(response.data);
        setLoading(false);
    };

    useEffect(() => {
        fetchConfig();
    }, []);

    return (
        <>
            <Modal
                opened={!loading}
                onClose={onClose}
                title="Select an Analytic"
                centered
            >
                <Accordion variant="contained">
                    {config?.resultTypes.map((resultType) => (
                        <AnalyticResultTypeItem
                            key={resultType.name}
                            resultType={resultType}
                            onSelectAnalytic={(code) =>
                                setSelectedAnalytic({ code, resultType })
                            }
                        />
                    ))}
                </Accordion>
            </Modal>

            {selectedAnalytic && (
                <AnalyticFormDialog
                    onClose={() => setSelectedAnalytic(undefined)}
                    selectedAnalytic={selectedAnalytic}
                />
            )}
        </>
    );
}
