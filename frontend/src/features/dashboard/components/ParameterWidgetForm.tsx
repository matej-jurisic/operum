import { SaveParameterItemDto } from "../types/DashboardDto";
import { ParameterForm } from "./ParameterForm";

interface Props {
    color?: string;
    onBack: () => void;
    onAdd: (dto: SaveParameterItemDto) => Promise<void>;
}

/** Adds a parameter widget: a filter set whose clause values are typed on the board,
    re-filtering the Analytic/Entries widgets wired to it. */
export function ParameterWidgetForm({ color, onBack, onAdd }: Props) {
    return (
        <ParameterForm
            submitLabel="Add"
            color={color}
            onBack={onBack}
            onSubmit={onAdd}
        />
    );
}
